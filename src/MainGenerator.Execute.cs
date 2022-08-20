using System.Diagnostics;

namespace Recline.Generator;

public partial class MainGenerator : IIncrementalGenerator
{
    internal static string lastHintName = null!;
    internal static SourceText lastGeneratedText = null!;
    internal static INamedTypeSymbol currClass = null!;
    internal static Compilation currCompilation = null!;

    private static TimeSpan analysisTime, parserGenerationTime;

    static void GenerateFromData(ImmutableArray<(CLIData? data, ImmutableArray<Diagnostic> diags)> tuples, SourceProductionContext spc) {
        spc.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.TimingInfo,
                Location.None,
                "PostInit", postInitMS
            )
        );

        spc.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.TimingInfo,
                Location.None,
                "Transform", analysisMS
            )
        );

        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        var allDiags = tuples.SelectMany(t => t.diags).Where(d => d is not null).ToArray();

        if (allDiags.Length > 0) {
            foreach (var diag in allDiags) {
                spc.ReportDiagnostic(diag);
            }

            return;
        }

        var allDatas = tuples.Select(t => t.data).Where(d => d is not null)!.ToImmutableArray<CLIData>();

        if (allDatas.Length < 1)
            return; // it's already been reported

        if (allDatas.Length > 1) {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.TooManyCLIClasses,
                    Location.None,
                    allDatas[0].FullClassName, allDatas[1].FullClassName
                )
            );

            return;
        }

        GenerateFromDataCore(allDatas, spc);

        watch.Stop();
        codegenMS = watch.ElapsedMilliseconds;

        spc.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.TimingInfo,
                Location.None,
                "Generation", codegenMS
            )
        );
    }

    static void GenerateFromDataCore(ImmutableArray<CLIData> datas, SourceProductionContext context) {
        var sw = new Stopwatch();
        sw.Start();

        var (appName, fullClassName, usings, cmdAndArgs, opts, appDesc, cmds, helpExitCode) = datas[0]!;

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
            Resources.GenNamespace + "_Program.g.cs",
            SourceText.From(Resources.ProgClassStr, Encoding.UTF8)
        );

        context.AddSource(
            Resources.GenNamespace + "_CmdDescDynamic.g.cs",
            SourceText.From(
                descDynamicText
#if DEBUG
                + "\n// Analysis took " + analysisTime.Milliseconds + "ms\n// Generation took " + parserGenerationTime.Milliseconds + "ms"
#endif
                ,
                Encoding.UTF8
            )
        );

        MinimalSymbolInfo.Cache.FullReset();
    }
}