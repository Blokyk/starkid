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

    private static TimeSpan analysisTime, parserGenerationTime;

    static void Execute(ImmutableArray<CLIData?> classes, SourceProductionContext context) {
        var result = TryExecute(classes, context);

        if (result is not null)
            context.AddSource("CLIGen_err.g.txt", ("failed to generate: " + result));
    }

    static string? TryExecute(ImmutableArray<CLIData?> datas, SourceProductionContext context) {
        if (datas.Length != 1 || datas[0] is null)
            return "Expected only 1 CLI declaration, got " + datas.Length;

        var (appName, fullClassName, usings, cmdAndArgs, opts, appDesc, cmds, helpExitCode) = datas[0]!;

        var sw = new Stopwatch();
        sw.Start();

        var descBuilder = new CmdDescBuilder(
            appName,
            fullClassName,
            usings,
            cmdAndArgs,
            opts,
            appDesc
        ) {
            HelpExitCode = helpExitCode
        };

        foreach (var cmd in cmds)
            descBuilder.AddCmd(cmd, cmd.Options, cmd.Args);

        var descDynamicText = descBuilder.ToString();

        sw.Stop();
        parserGenerationTime = sw.Elapsed;

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