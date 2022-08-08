using System.Collections.Immutable;
using System.Collections.Concurrent;

using CLIGen;
using CLIGen.Generator;
using CLIGen.Generator.Model;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Diagnostics;

namespace CLIGen.Generator;

public partial class MainGenerator : IIncrementalGenerator
{
    internal static Dictionary<string, CmdHelp> _nameToHelp = new();
    internal static Dictionary<CmdHelp, string> _cachedHelpText = new();

    internal static string lastHintName = null!;
    internal static SourceText lastGeneratedText = null!;
    internal static INamedTypeSymbol currClass = null!;
    internal static Compilation currCompilation = null!;

    static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context) {
        var result = TryExecute(compilation, classes, context);

        if (result is not null)
            return; //("failed to generate: " + result);
    }

    static string? TryExecute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context) {
        ClassDeclarationSyntax[] nonNullClassSyntaxes = classes.Where(syntax => syntax is not null).ToArray()!;

        if (nonNullClassSyntaxes.Length == 0)
            return null;

        Utils.UpdatePredefTypes(currCompilation = compilation);

        if (!Validate(compilation, nonNullClassSyntaxes, context, out var model, out var classSymbol))
            return "Failed to validate";

        var sw = new Stopwatch();
        sw.Start();

        var fullClassName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (!Utils.TryGetCLIClassDecNode(classSymbol, out var node))
            return "Couldn't get class with CLI attribute";

        // var cliAttrib

        if (!classSymbol.TryGetAttribute(Ressources.CLIAttribName, out var cliAttrib))
            return "Couldn't get CLI attribute for class " + classSymbol.Name;

        if (!Utils.TryParseCLIAttrib(cliAttrib, out var cliAttr))
            return "Couldn't parse CLI attribute on class " + classSymbol.Name;

        var (appName, entryPointName) = cliAttr;

        // var descAttrib

        string? appDesc = null;

        if (classSymbol.TryGetAttribute(Ressources.DescAttribName, out var descAttrib)) {
            if (!Utils.TryGetDescription(descAttrib, out appDesc))
                return "Couldn't parse description for class " + classSymbol.Name;
        }

        var optList = new List<Option>();
        var cmdList = new List<Command>();

        var members = classSymbol.GetMembers();

        foreach (var member in members.Where(s => s.Kind is SymbolKind.Field or SymbolKind.Property or SymbolKind.Method)) {
            if (!TryGetOptions(member, model, out var opt)) {
                return "Couldn't parse symbol " + member.Name + " into an option";
            }

            if (opt is not null)
                optList.Add(opt);
        }

        var classMethods = members.OfType<IMethodSymbol>();

        foreach (var member in classMethods) {
            if (!TryGetCommand(member, model, out var cmd)) {
                return "Couldn't parse symbol " + member.Name + " into a subcmd";
            }

            if (cmd is not null)
                cmdList.Add(cmd);
        }

        var opts = optList.ToArray();
        var cmds = cmdList.ToArray();

        for (int i = 0; i < cmds.Length; i++) {
            if (cmds[i].ParentCmdName is not null) {
                if (!TryBindParentCmd(cmds[i], cmds, out var newCmd))
                    return "Couldn't bind parent cmd '" + cmds[i].ParentCmdName + "' of cmd '" + cmds[i].Name + "'";

                cmds[i] = newCmd;
            }
        }

        //TODO: check that every option and cmd has a unique name and/or alias

        var optsDescs = opts.Select(o => o.Desc).ToArray();
        var subCmdsDescs = cmds.Select(c => c.WithArgsDesc).ToArray();

        Command? rootCmd = null;
        var posArgs = new Desc[] { };

        if (entryPointName is not null) {
            entryPointName = Utils.GetLastNamePart(entryPointName.AsSpan());

            //TODO: check that, if the entry point name is Main, then the class is partial
            // and the main method is marked partial without implementation

            rootCmd = cmds.FirstOrDefault(
                cmd => cmd.BackingSymbol?.Name == entryPointName
            );

            if (rootCmd is null) {
                var candidates = classMethods.Where(
                    m => m.Name == entryPointName
                ).ToArray();

                if (candidates.Length < 1)
                    return "Could not find any method named " + entryPointName;
                else if (candidates.Length > 1)
                    return classSymbol.Name + " contains multiple methods named " + entryPointName;

                var method = candidates[0];

                if (!TryGetEntryPointCommand(method, model, out rootCmd))
                    return "Couldn't parse method " + method.Name + " as an entry point";
            }

            if (rootCmd is null)
                throw new Exception("wtf??");

            if (rootCmd.Options.Count != 0)
                return "Entry point cannot have parameters marked as options. Please use fields or properties to declare them.";

            rootCmd = rootCmd with {
                Name = appName,
                Description = appDesc ?? rootCmd.Description,
            };

            for (int i = 0; i < cmds.Length; i++) {
                if (cmds[i].ParentCmdName is null) {
                    cmds[i].ParentCmd = rootCmd;
                }
            }

            posArgs = rootCmd.Args.Select(
                arg => new Desc(
                    arg.Name,
                    arg.Description
                )
            ).ToArray();
        }

        sw.Stop();
        var analysisTime = sw.Elapsed;
        sw.Restart();

        var descBuild = new CmdDescBuilder(fullClassName);

        context.AddSource(
            Ressources.GenNamespace + "_CmdDescDynamic.g.cs",
            SourceText.From(CmdDescBuilder.GetBaseDescFile(appName), Encoding.UTF8)
        );

        if (rootCmd is not null) {
            context.AddSource(
                Ressources.GenNamespace + "_" + appName + ".g.cs",
                SourceText.From(descBuild.AddCmd(rootCmd, opts, posArgs), Encoding.UTF8)
            );
        }

        sw.Stop();
        var parserGenerationTime = sw.Elapsed;
        sw.Restart();

        var rootHelp = new CmdHelp(
            null,
            appName,
            appDesc,
            optsDescs,
            posArgs,
            subCmdsDescs,
            entryPointName is not null
        );

        var origFile = node.SyntaxTree.FilePath;

        if (origFile.Length == 0)
            origFile = "__" + node.Identifier.ToString();

        var helpText = rootHelp.AppendTo(new StringBuilder()).ToString();
        sw.Stop();
        var helpGenerationTime = sw.Elapsed;

        /*System.IO.File.WriteAllText(
            "/home/blokyk/csharp/cli-gen/src/CLIGen.Tests/obj/out/CLIGen.Generated_Help.g.txt",
            helpText
                + "\n# Analysis generated in: " + analysisTime
                + "\n# CmdDesc generated in:" + parserGenerationTime
                + "\n# Help generated in: " + helpGenerationTime
        );*/

        return null;
    }
}