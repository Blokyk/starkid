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
            context.AddSource("CLIGen_err.g.txt", ("failed to generate: " + result));
    }

    static string? TryExecute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context) {
        ClassDeclarationSyntax[] nonNullClassSyntaxes = classes.Where(syntax => syntax is not null).ToArray()!;

        if (nonNullClassSyntaxes.Length == 0)
            return null;

        Utils.UpdatePredefTypes(currCompilation = compilation);

        if (!Validate(compilation, nonNullClassSyntaxes, context, out var model, out var classSymbol))
            return "Failed to validate classes";

        var sw = new Stopwatch();
        sw.Start();

        var fullClassName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // var cliAttrib

        if (!classSymbol.TryGetAttribute(Ressources.CLIAttribName, out var cliAttrib))
            return "Couldn't get CLI attribute for class " + classSymbol.Name;

        if (!Utils.TryParseCLIAttrib(cliAttrib, out var cliAttr))
            return "Couldn't parse CLI attribute on class " + classSymbol.Name;

        var (appName, entryPointName) = cliAttr;

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

        //TODO: check that every option and cmd has a unique name and/or alias

        Command? rootCmd = null;
        var posArgs = Array.Empty<Argument>();

        if (entryPointName is not null) {
            entryPointName = Utils.GetLastNamePart(entryPointName.AsSpan());

            // TODO: check that, if the entry point name is Main, then the main method
            // is marked partial without implementation. We also need to create a stub/trampoline
            // from $cliClassName to our Program's Main method

            rootCmd = cmds.FirstOrDefault(
                cmd => cmd.BackingSymbol.Name == entryPointName
            );

            if (rootCmd is null) {
                // technically, we could use classSymbol.GetMembers(entryPointName),
                // but that'd probably be slower

                // we don't actually have to use .ToArray here, we could just try to iterate
                // and error if we can't call MoveNext() exactly once. But that would be
                // an incredibly small optimisation

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

            if (rootCmd.Options.Length != 0)
                return "Entry point cannot have parameters marked as options. Please use fields or properties to declare them.";

            rootCmd = rootCmd with {
                Name = appName,
                Description = appDesc ?? rootCmd.Description,
                ParentSymbolName = null
            };

            posArgs = rootCmd.Args;
        }

        for (int i = 0; i < cmds.Length; i++) {
            if (cmds[i].ParentSymbolName is not null) {
                if (!TryBindParentCmd(cmds[i], cmds, out var newCmd))
                    // Are you sure you marked '${cmds[i].ParentCmdName}' with [Command] ?
                    return "Couldn't bind parent cmd '" + cmds[i].ParentSymbolName + "' of sub-cmd '" + cmds[i].Name + "'";

                cmds[i] = newCmd;
            } else if (rootCmd is not null) {
                cmds[i].ParentCmd = rootCmd;
            }
        }

        sw.Stop();
        var analysisTime = sw.Elapsed;
        sw.Restart();

        if (!TryGetAllUniqueUsings(classSymbol, out var usings))
            return "Couldn't collect all usings for class " + classSymbol.Name;

        var descBuilder = new CmdDescBuilder(
            appName,
            fullClassName,
            usings,
            rootCmd is null ? null : (rootCmd, posArgs),
            opts,
            appDesc
        );

        foreach (var cmd in cmds)
            descBuilder.AddCmd(cmd, cmd.Options, cmd.Args);

        var descDynamicText = descBuilder.ToString();

        sw.Stop();
        var parserGenerationTime = sw.Elapsed;

        context.AddSource(
            Ressources.GenNamespace + "_CmdDescDynamic.g.cs",
            SourceText.From(
                descDynamicText
#if DEBUG
                + "\n// Analysis took " + analysisTime.Milliseconds + "ms\n// Generation took " + parserGenerationTime.Milliseconds + "ms"
#endif
                ,
                Encoding.UTF8
            )
        );

        return null;
    }
}