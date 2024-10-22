using System.Diagnostics;

using StarKid.Generator.CommandModel;
using StarKid.Generator.SymbolModel;

namespace StarKid.Generator;

[Generator(LanguageNames.CSharp)]
public partial class StarKidGenerator : IIncrementalGenerator
{
    private static readonly string _cmdGroupAttributeName = "StarKid.CommandGroupAttribute";

    internal static readonly string _starkidProgramCode;

    static StarKidGenerator() {
        _starkidProgramCode = MiscUtils.GetStaticResource("StarKidProgram.cs");
        _attributeCode = MiscUtils.GetStaticResource("StarKidAttributes.cs");
    }

    internal static readonly string _attributeCode;

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        if (Debugger.IsAttached)
            Debugger.Break();

        context.RegisterPostInitializationOutput(
            static postInitCtx =>
                postInitCtx.AddSource(
                    "StarKid_Attributes.g.cs",
                    SourceText.From(_attributeCode, Encoding.UTF8)
                )
        );

        var langVersionSource
            = context
                .CompilationProvider
                .Select(
                    (comp, _) => (comp as CSharpCompilation)?.LanguageVersion ?? LanguageVersion.Default
                );

    #if DEBUG
        var fawmnWarmup
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _cmdGroupAttributeName,
                    (node, _) => node is ClassDeclarationSyntax,
                    (_, _) => 0
                )
                .Collect()
                .WithTrackingName("fawmn_warmup");

        context.RegisterSourceOutput(fawmnWarmup, (_, _) => { });
    #endif

        var usingsSource
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _cmdGroupAttributeName,
                    (node, _) => node is ClassDeclarationSyntax,
                    (ctx, _) => SyntaxUtils.GetReachableNamespaceNames(ctx.TargetNode)
                )
                .SelectMany((arr, _) => arr)
                .Collect()
                .WithTrackingName("starkid_collect_usings");

        // todo: separate this into three pipelines:
        //      - ParseWith resolution with FAWMN (returns a map between symbol name and parser)
        //      - same with ValidateWith
        //      - Group building, like before
        // We can then bind parsers back to their original symbols by going through the whole
        // command tree (maybe have an variable for like "needs a parser/validator" so that
        // we don't lookup *every single symbol name*)
        //
        // do we also want to separate auto-parsers? the problem is that we need to do the group
        // building first, but we could the ParseWith
        // part after group_building, and have group_building output a list of stuff that needs
        // an auto-parser
        //
        // todo: we could also make another pipeline to get the xml docs and use a similar strategy to merge back
        // we'd have to do that for every symbol with either [Command], [CommandGroup], or [Option]
        var groupsSource
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _cmdGroupAttributeName,
                    (node, _) => node is ClassDeclarationSyntax,
                    (ctx, _) => {
                        var wrapper = new DataAndDiagnostics<Group?>();
                        wrapper.Data = CreateGroup((INamedTypeSymbol)ctx.TargetSymbol, ctx.SemanticModel, wrapper.AddDiagnostic);
                        return wrapper;
                    }
                )
                .WithTrackingName("starkid_group_building");

        var groupTreeSource
            = groupsSource
                .Data()
                .Collect()
                .Select(
                    (groups, _) => {
                        var wrapper = new DataAndDiagnostics<Group?>();
                        wrapper.Data = BindGroups(groups, wrapper.AddDiagnostic);
                        return wrapper;
                    }
                )
                .WithTrackingName("starkid_binding");

        var globalConfigOptionsSource = context.AnalyzerConfigOptionsProvider.Select((opts, _) => opts.GlobalOptions);

        var starkidConfigSource
            = globalConfigOptionsSource.Combine(langVersionSource)
                .Select(
                    static (combined, _) => {
                        var wrapper = new DataAndDiagnostics<StarKidConfig>();
                        wrapper.Data = ParseConfig(
                            combined.Left, // analyzer config
                            combined.Right, // lang version
                            wrapper.AddDiagnostic
                        );
                        return wrapper;
                    }
                );

        // generates the parser + command infos
        context.RegisterImplementationSourceOutput(
            groupTreeSource.Data()
                .Combine(usingsSource)
                .Combine(starkidConfigSource.Data()),
            static (spc, groupTreeAndUsingsAndConfig) => {
                var rootGroup = groupTreeAndUsingsAndConfig.Left.Left;
                var usings = groupTreeAndUsingsAndConfig.Left.Right;
                var config = groupTreeAndUsingsAndConfig.Right;

                if (rootGroup is null || config is null)
                    return;

                GenerateParserAndHandlers(rootGroup, usings, config, spc);
            }
        );


        context.RegisterDiagnosticSource(groupsSource);
        context.RegisterDiagnosticSource(groupTreeSource);
        context.RegisterDiagnosticSource(starkidConfigSource);
    }

    internal static Group? CreateGroup(INamedTypeSymbol classSymbol, SemanticModel model, Action<Diagnostic> addDiagnostic) {
        var attrListBuilder = new AttributeModel.AttributeListBuilder(addDiagnostic);

        static Group? bail() {
            SymbolInfoCache.FullReset();
            return null;
        }

        if (!GroupBuilder.TryCreateGroupFrom(classSymbol, attrListBuilder, model.Compilation, addDiagnostic, out var group))
            return bail();

        return group;
    }

    internal static Group? BindGroups(IEnumerable<Group?> groups, Action<Diagnostic> addDiagnostic) {
        var classNames = new Dictionary<string, Group>(8); // ¯\_(ツ)_/¯

        // collect the names of the each group's class
        foreach (var group in groups) {
            if (group is null)
                return null;

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

        // if there wasn't any actual groups
        if (classNames.Count == 0)
            return null;

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

        Debug.Assert(rootGroup is not null);

        if (!ValidateOptionTree(rootGroup!, addDiagnostic))
            return null;

        return rootGroup;
    }

    // todo: make those local functions static
    internal static bool ValidateOptionTree(Group rootGroup, Action<Diagnostic> addDiagnostic) {
        return validate(rootGroup, new(), new());

        bool validate(Group group, HashSet<string> globalNames, HashSet<char> globalAliases) {
            var localGlobalNames = new HashSet<string>(globalNames);
            var localGlobalAliases = new HashSet<char>(globalAliases);

            if (!validateCore(group.OptionsAndFlags, localGlobalNames, localGlobalAliases))
                return false;

            foreach (var sub in group.SubGroups) {
                // yes, we have to create new ones each time,
                // cause otherwise the .Add()s would carry
                // over between subs
                if (!validate(sub, localGlobalNames, localGlobalAliases))
                    return false;
            }

            foreach (var cmd in group.Commands) {
                // commands shouldn't have any global options, so no need
                // to allocate new registries
                if (!validateCore(cmd.OptionsAndFlags, localGlobalNames, localGlobalAliases))
                    return false;
            }

            return true;
        }

        bool validateCore(IEnumerable<Option> opts, HashSet<string> globalNames, HashSet<char> globalAliases) {
            var localNames = new HashSet<string>();
            var localAliases = new HashSet<char>();

            foreach (var option in opts) {
                // register it locally and check if it's an existing global option
                // if it's new global option, register it in globalNames
                var longNameExists
                    =  !localNames.Add(option.Name)
                    || (option.IsGlobal ? !globalNames.Add(option.Name) : globalNames.Contains(option.Name));

                if (longNameExists) {
                    addDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.OptNameAlreadyExists,
                            option.GetLocation(),
                            option.Name
                        )
                    );

                    return false;
                }

                if (option.Alias != default(char)) {
                    var aliasExists
                        =  !localAliases.Add(option.Alias)
                        || (option.IsGlobal ? !globalAliases.Add(option.Alias) : globalAliases.Contains(option.Alias));

                    if (aliasExists) {
                        addDiagnostic(
                            Diagnostic.Create(
                                Diagnostics.OptAliasAlreadyExists,
                                option.GetLocation(),
                                option.Alias
                            )
                        );

                        return false;
                    }
                }
            }

            return true;
        }
    }
}
