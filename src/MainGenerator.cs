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
        nameof(CLIAttribute),
        nameof(CommandAttribute),
        nameof(DescriptionAttribute),
        nameof(OptionAttribute),
        nameof(SubCommandAttribute)
    };

    public static long postInitMS = -1;
    public static long analysisMS = -1;
    public static long codegenMS = -1;

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(
            static postInitCtx => {
                // TODO: load stuff from consts strings when everything is stable

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                // FIXME: obviously this is temp, inline all this when stable

                foreach (var filename in _staticFilenames) {
                    postInitCtx.AddSource(
                        Ressources.GenNamespace + "_" + filename + ".g.cs",
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
                    Ressources.GenNamespace + "_Attributes.g.cs",
                    SourceText.From(sb.ToString(), Encoding.UTF8)
                );
                watch.Stop();
                postInitMS = watch.ElapsedMilliseconds;
            }
        );

        var cliDataSource = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Recline.CLIAttribute",
                static (node, _) => HasAnyAttributes(node),
                static (ctx, _) => {
                    var watch = new System.Diagnostics.Stopwatch();
                    watch.Start();
                    var res = GetCmdData(ctx);
                    watch.Stop();
                    analysisMS = watch.ElapsedMilliseconds;
                    return res;
                }
            )
            .WithComparer(new CLIDataComparer())
            .Collect();

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(cliDataSource,
            static (spc, cliData) => GenerateFromData(cliData, spc));
    }

    static bool HasAnyAttributes(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0};

    static (CLIData? data, ImmutableArray<Diagnostic> diags) GetCmdData(GeneratorAttributeSyntaxContext ctx) {
        var sw = new Stopwatch();
        sw.Start();

        var model = ctx.SemanticModel;
        Utils.UpdatePredefTypes(model.Compilation);

        var attribParser = new AttributeParser();
        var modelBuilder = new ModelBuilder(attribParser);

        (CLIData? data, ImmutableArray<Diagnostic> diags) bail() => (null, modelBuilder.GetDiagnostics());

        var classSymbol = (ctx.TargetSymbol as INamedTypeSymbol)!;

        var fullClassName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (!classSymbol.TryGetAttribute(Ressources.CLIAttribName, out var cliAttrib))
            throw new Exception("Couldn't get CLI attribute for class " + classSymbol.Name);

        if (!attribParser.TryParseCLIAttrib(cliAttrib, out var cliAttr))
            return (null, ImmutableArray<Diagnostic>.Empty); // no need to add diagnostics, cause the syntax is already invalid

        var (appName, entryPointName, helpExitCode) = cliAttr;

        string? appDesc = null;

        if (classSymbol.TryGetAttribute(Ressources.DescAttribName, out var descAttrib)) {
            if (!Utils.TryGetDescription(descAttrib, out appDesc))
                return (null, ImmutableArray<Diagnostic>.Empty); // no need to add diagnostics, cause the syntax is already invalid
        }

        var optList = new List<Option>();
        var cmdList = new List<Command>();

        var members = classSymbol.GetMembers().Where(m => m.Kind is SymbolKind.Field or SymbolKind.Property or SymbolKind.Method);

        foreach (var member in members) {
            if (!modelBuilder.TryGetOptions(member, out var opt)) {
                return bail();
            }

            if (opt is not null) {
                optList.Add(opt);
            } else if (member is IMethodSymbol method) {
                if (!modelBuilder.TryGetCommand(method, out var cmd)) {
                    return bail();
                }

                if (cmd is not null)
                    cmdList.Add(cmd);
            }
        }

        var opts = optList.ToArray();
        var cmds = cmdList.ToArray();

        //TODO: check that every option and cmd has a unique name and/or alias

        Command? rootCmd = null;
        var posArgs = Array.Empty<Argument>();

        var classMethods = members.OfType<IMethodSymbol>();

        if (entryPointName is not null) {
            entryPointName = Utils.GetLastNamePart(entryPointName.AsSpan());

            modelBuilder.TryGetEntryPoint(
                entryPointName,
                cmds,
                classMethods,
                cliAttrib.ApplicationSyntaxReference!.SyntaxTree.GetLocation(cliAttrib.ApplicationSyntaxReference!.Span),
                out rootCmd
            );

            rootCmd = rootCmd with {
                Name = appName,
                Description = appDesc ?? rootCmd.Description,
                ParentSymbolName = null
            };

            posArgs = rootCmd.Args;
        }

        for (int i = 0; i < cmds.Length; i++) {
            if (cmds[i].ParentSymbolName is not null) {
                if (!modelBuilder.TryBindParentCmd(cmds[i], cmds, out var newCmd))
                    // Are you sure you marked '${cmds[i].ParentCmdName}' with [Command] ?
                    return bail();

                cmds[i] = newCmd;
            } else if (rootCmd is not null) {
                cmds[i].ParentCmd = rootCmd;
            }
        }

        if (!modelBuilder.TryGetAllUniqueUsings(classSymbol, out var usings))
            throw new Exception("Couldn't collect all usings for class " + classSymbol.Name);

        sw.Stop();
        analysisTime = sw.Elapsed;

        return (new CLIData(
            appName,
            fullClassName,
            usings,
            rootCmd is null ? null : (rootCmd, posArgs),
            opts,
            appDesc,
            cmds,
            helpExitCode
        ), modelBuilder.GetDiagnostics());
    }
}

internal class CLIDataComparer : EqualityComparer<(CLIData? data, ImmutableArray<Diagnostic> diags)>
{
    public override bool Equals((CLIData? data, ImmutableArray<Diagnostic> diags) x, (CLIData? data, ImmutableArray<Diagnostic> diags) y)
        => x.diags.Length == 0
        && y.diags.Length == 0
        && EqualityComparer<CLIData?>.Default.Equals(x.data, y.data);

    public override int GetHashCode((CLIData? data, ImmutableArray<Diagnostic> diags) obj)
        => EqualityComparer<CLIData?>.Default.GetHashCode(obj.data);
}