using System.Collections.Immutable;
using System.Collections.Concurrent;

using Recline;
using Recline.Generator;
using Recline.Generator.Model;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Diagnostics;

namespace Recline.Generator;

public partial class MainGenerator : IIncrementalGenerator
{
    internal static string lastHintName = null!;
    internal static SourceText lastGeneratedText = null!;
    internal static INamedTypeSymbol currClass = null!;
    internal static Compilation currCompilation = null!;

    private static TimeSpan analysisTime, parserGenerationTime;

    static void Execute(ImmutableArray<CLIData?> classes, SourceProductionContext spc) {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        var result = TryExecute(classes, spc);

        watch.Stop();
        codegenMS = watch.ElapsedMilliseconds;

        spc.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CLI000",
                    "PostInit took: " + postInitMS + "ms",
                    "PostInit took: " + postInitMS + "ms",
                    "Debug",
                    (postInitMS > 100 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Info),
                    true
                ),
                Location.None
            )
        );

        spc.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CLI000",
                    "Analysis took: " + analysisMS + "ms",
                    "Analysis took: " + analysisMS + "ms",
                    "Debug",
                    (analysisMS > 10 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Info),
                    true
                ),
                Location.None
            )
        );

        spc.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CLI000",
                    "Generation took: " + codegenMS + "ms",
                    "Generation took: " + codegenMS + "ms",
                    "Debug",
                    (codegenMS > 5 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Info),
                    true
                ),
                Location.None
            )
        );

        if (result is not null)
            spc.AddSource("Recline_err.g.txt", ("failed to generate: " + result));
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