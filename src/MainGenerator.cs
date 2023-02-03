using System.IO;
using System.Diagnostics;

using Recline.Generator.Model;

namespace Recline.Generator;

[Generator(LanguageNames.CSharp)]
public partial class MainGenerator : IIncrementalGenerator
{
    private const string staticFolderPath = "/home/blokyk/csharp/recline/src/Static/";
    private static readonly string[] _staticFilenames = new string[] {};

    private static readonly string[] _attributeNames = new[] {
        nameof(CommandAttribute),
        nameof(DescriptionAttribute),
        nameof(OptionAttribute),
        nameof(CommandGroupAttribute),
        nameof(ParseWithAttribute),
        nameof(ValidateWithAttribute)
    };

    public static double postInitMS = -1;
    public static double analysisMS = -1;
    public static double codegenMS = -1;

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(
            static postInitCtx => {
                // fixme: load stuff from const strings when everything is stable

                var watch = new Stopwatch();
                watch.Start();

                foreach (var filename in _staticFilenames) {
                    postInitCtx.AddSource(
                        Resources.GenNamespace + "_" + filename + ".g.cs",
                        SourceText.From(File.ReadAllText(staticFolderPath + filename + ".cs"), Encoding.UTF8)
                    );
                }

                var sb = new StringBuilder();

                sb.AppendLine(@"
#define GEN
#nullable enable

using System;

namespace Recline;

");

                foreach (var filename in _attributeNames) {
                    sb.AppendLine(File.ReadAllText(staticFolderPath + "Attributes/" + filename + ".cs"));
                }

                postInitCtx.AddSource(
                    Resources.GenNamespace + "_Attributes.g.cs",
                    SourceText.From(sb.ToString(), Encoding.UTF8)
                );
                watch.Stop();
                postInitMS = watch.Elapsed.TotalMilliseconds;
            }
        );

        var langVersionSource
            = context
                .CompilationProvider
                .Select(
                    (comp, _) => (comp as CSharpCompilation) ?.LanguageVersion ?? LanguageVersion.Default
                );

        var usingsSource
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    typeof(CommandGroupAttribute).FullName!,
                    (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    (ctx, _) => Utils.GetUsings((ctx.TargetNode as ClassDeclarationSyntax)!)
                )
                .SelectMany((arr, _) => arr)
                .Collect()
                .WithTrackingName("recline_collect_usings");

        var groupsSource
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    typeof(CommandGroupAttribute).FullName!,
                    (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    (ctx, _) => {
                        var wrapper = new GeneratorDataWrapper<Group?>();
                        wrapper.Data = CreateGroup(ctx, wrapper.AddDiagnostic);
                        return wrapper;
                    }
                )
                .WithTrackingName("recline_group_building");

        var groupTreeSource
            = groupsSource
                .Select((w, _) => w.Data)
                .Collect()
                .Select(
                    (groups, _) => {
                        var wrapper = new GeneratorDataWrapper<Group?>();
                        wrapper.Data = BindGroups(groups, wrapper.AddDiagnostic);
                        return wrapper;
                    }
                )
                .WithTrackingName("recline_binding");

        var reclineConfig
            = context
                .AnalyzerConfigOptionsProvider
                .WithTrackingName("recline_config")
                .Select((opts, _) => ReclineConfig.Parse(opts.GlobalOptions));

        var groupTreeOnlySource
            = groupTreeSource.Select((w, _) => w.Data);

        // todo: add custom comparers for each
        var combinedValueProvider
            = groupTreeOnlySource
                .Combine(usingsSource)
                .Combine(reclineConfig.Combine(langVersionSource));

        // Generate the source using the compilation and enums
        context.RegisterImplementationSourceOutput(
            combinedValueProvider,
            static (spc, groupTreeAndConfig)
                => GenerateFromData(
                        groupTreeAndConfig.Left.Left,   // root group
                        groupTreeAndConfig.Left.Right,  // usings
                        groupTreeAndConfig.Right.Left,  // config
                        groupTreeAndConfig.Right.Right, // langVersion
                        spc
                )
        );

        var reclineDiagnosticSource
            = groupsSource
                .Select((w, _)
                    => w.GetDiagnostics()
                )
                .Combine(groupTreeSource.Select((w, _) => w.GetDiagnostics()))
                .WithTrackingName("recline_collect_diagnostics")
                ;

        context.RegisterSourceOutput(
            reclineDiagnosticSource,
            static (spc, diagsTuple) => {
                foreach (var diag in diagsTuple.Left)
                    spc.ReportDiagnostic(diag);
                foreach (var diag in diagsTuple.Right)
                    spc.ReportDiagnostic(diag);
            }
        );
    }

    static Group? CreateGroup(GeneratorAttributeSyntaxContext ctx, Action<Diagnostic> addDiagnostic) {
        var model = ctx.SemanticModel;
        CommonTypes.Refresh(model.Compilation, force: false);

        var attribParser = new AttributeParser(addDiagnostic);

        static Group? bail() {
            CommonTypes.Reset();
            SymbolInfoCache.FullReset();
            return null;
        }

        if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
            return bail();

        if (!GroupBuilder.TryCreateGroupFrom(classSymbol, attribParser, model, addDiagnostic, out var group))
            return bail();

        return group;
    }

    static Group? BindGroups(ImmutableArray<Group?> groups, Action<Diagnostic> addDiagnostic) {
        if (groups.IsDefaultOrEmpty)
            return null;

        var classNames = new Dictionary<string, Group>(groups.Length);

        // collect the names of the each group's class
        foreach (var group in groups) {
            if (group is null)
                continue;

            // if we have duplicate class names, it means an attribute
            // was repeated multiple times on the same class. That is pretty
            // likely to happen in user code, since we should assume it is
            // invalid most of the time; therefore might as well take the small
            //perf hit of an additional lookup (since we don't have Dictionary<T,U>.TryAdd
            // on ns2.0), and just ignore it, since we'd still like to bind as many
            // group as possible in any case
            if (classNames.ContainsKey(group.FullClassName))
                continue;

            classNames.Add(group.FullClassName, group);
        }

        Group? rootGroup = null;

        // for each group, find the group in which it was
        // contained and then add it as a subgroup to that parent

        foreach (var group in classNames.Values) {
            // a group is the root group if:
            //      (1) It is not a nested class
            //              => group.ParentClassFullName is null
            //   OR
            //      (2) Its parent class isn't marked with group
            //              => group.ParentClassFullName is not in classNames

            if (group.ParentClassFullName is null || !classNames.ContainsKey(group.ParentClassFullName)) {
                if (rootGroup is null) {
                    rootGroup = group;
                    continue;
                }

                // if there already was a root group, then
                addDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.TooManyRootGroups,
                        Location.None,
                        rootGroup.FullClassName, group.FullClassName
                    )
                );

                return null;
            }

            classNames[group.ParentClassFullName].AddSubgroup(group);
        }

        if (rootGroup is not null)
            ValidateOptionTree(rootGroup, addDiagnostic);

        return rootGroup;
    }

    static void ValidateOptionTree(Group rootGroup, Action<Diagnostic> addDiagnostic) {
        void validate(Group group, HashSet<string> names, HashSet<char> aliases) {
            var groupOptions = group.OptionsAndFlags;
            var commandOptions = group.Commands.SelectMany(cmd => cmd.OptionsAndFlags);

            foreach (var option in groupOptions.Concat(commandOptions)) {
                if (!names.Add(option.Name)) {
                    addDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.OptNameAlreadyExists,
                            option.GetLocation(),
                            option.Name
                        )
                    );
                }

                if (option.Alias != default(char) && !aliases.Add(option.Alias)) {
                    addDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.OptAliasAlreadyExists,
                            option.GetLocation(),
                            option.Alias
                        )
                    );
                }
            }

            foreach (var sub in group.SubGroups) {
                // yes, we have to create new ones each time,
                // cause otherwise the .Add()s would carry
                // over between subs
                validate(sub, new(names), new(aliases));
            }
        }

        validate(rootGroup, new(), new());
    }
}