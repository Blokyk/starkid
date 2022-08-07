using System.Collections.Immutable;

using CLIGen;
using CLIGen.Generator;
using CLIGen.Generator.Model;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static CLIGen.Generator.Ressources;
using System.Diagnostics;

namespace CLIGen.Generator;

public partial class MainGenerator : IIncrementalGenerator
{
    internal static Dictionary<string, CmdHelp> _nameToHelp = new();
    internal static Dictionary<CmdHelp, string> _cachedHelpText = new();

    internal static Compilation currCompilation = null!;

    static void Execute(Compilation compilation, ImmutableArray<INamedTypeSymbol> classes, SourceProductionContext context) {
        if (classes.IsDefaultOrEmpty)
            return;

        Utils.UpdatePredefTypes(currCompilation = compilation);

        if (!Validate(compilation, classes, context))
            return;

        var classSymbol = classes[0];
        var decNodeRefs = classSymbol.DeclaringSyntaxReferences;

        if (!Utils.TryGetCLIClassDecNode(classSymbol, out var node))
            return;

        var model = compilation.GetSemanticModel(node.SyntaxTree);

        // var cliAttrib

        if (!classSymbol.TryGetAttribute(Ressources.CLIAttribName, out var cliAttrib))
            return;

        if (!Utils.TryParseCLIAttrib(cliAttrib, out var cliAttr))
            return;

        var (appName, entryPoint) = cliAttr;

        // var descAttrib

        string? appDesc = null;
        if (!classSymbol.TryGetAttribute(Ressources.DescAttribName, out var descAttrib)) {
            if (!Utils.TryGetDescription(descAttrib, out appDesc))
                return;
        }

        var opts = new List<Option>();
        var cmds = new List<Command>();

        var members = classSymbol.GetMembers();

        foreach (var member in members.Where(s => s.Kind is SymbolKind.Field or SymbolKind.Property or SymbolKind.Method)) {
            if (!TryGetOptions(member, model, out var opt)) {
                return;
            }

            if (opt is not null)
                opts.Add(opt);
        }

        foreach (var member in members.OfType<IMethodSymbol>()) {
            if (!TryGetCommand(member, model, out var cmd)) {
                return;
            }

            if (cmd is not null)
                cmds.Add(cmd);
        }

        var optsDescs = new OptDesc[] { };
        var posArgsDescs = new Desc[] { };
        var subCmdsDescs = new Desc[] { };

        var rootHelp = new CmdHelp(
            null,
            appName,
            appDesc,
            optsDescs,
            posArgsDescs,
            subCmdsDescs,
            entryPoint is null
        );

        var origFile = node.SyntaxTree.FilePath;

        if (origFile.Length == 0)
            origFile = "__" + node.Identifier.ToString();
    }
}