using System.Diagnostics;

namespace Recline.Generator;

public partial class MainGenerator : IIncrementalGenerator
{
    static void GenerateFromData(ImmutableArray<(CLIData? data, ImmutableArray<Diagnostic> diags)> tuples, int columnLength, SourceProductionContext spc) {
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

        var watch = new Stopwatch();
        watch.Start();

        bool hasError = false;

        foreach (var (data, diags) in tuples) {
            hasError |= data is null;

            foreach (var diag in diags) {
                spc.ReportDiagnostic(diag);

                if (diag.Severity == DiagnosticSeverity.Error)
                    hasError = true;
            }
        }

        if (hasError)
            return;

        if (tuples.Length < 1)
            return;

        if (tuples.Length > 1) {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.TooManyCLIClasses,
                    Location.None,
                    tuples[0].data!.FullClassName, tuples[1].data!.FullClassName
                )
            );

            return;
        }

        if (columnLength <= 0) {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.InvalidColumnLength,
                    Location.None
                )
            );

            columnLength = 80;
        }

        Resources.MAX_LINE_LENGTH = columnLength;
        GenerateFromDataCore(tuples[0].data!, spc);

        watch.Stop();
        codegenMS = watch.Elapsed.TotalMilliseconds;

        spc.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.TimingInfo,
                Location.None,
                "Generation", codegenMS
            )
        );
    }

    static void GenerateFromDataCore(CLIData data, SourceProductionContext context) {
        var (appName, fullClassName, usings, cmdAndArgs, opts, appDesc, cmds, helpExitCode) = data!;

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

        context.AddSource(
            Resources.GenNamespace + "_Program.g.cs",
            SourceText.From(Resources.ProgClassStr, Encoding.UTF8)
        );

        context.AddSource(
            Resources.GenNamespace + "_CmdDescDynamic.g.cs",
            SourceText.From(descDynamicText, Encoding.UTF8)
        );

        SymbolInfoCache.FullReset();
        CommonTypes.Clear();
    }
}