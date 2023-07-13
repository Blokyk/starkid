using Recline.Generator.Model;

namespace Recline.Generator;

public partial class MainGenerator : IIncrementalGenerator
{
    static void GenerateFromData(Group? rootGroup, ImmutableArray<string> usings, ReclineConfig config, LanguageVersion langVersion, SourceProductionContext spc) {
        if (rootGroup is null)
            return;

        SanitizeConfig(ref config, spc);

        Resources.MAX_LINE_LENGTH = config.ColumnLength!.Value;

        CodeGenerator.UseLanguageVersion(langVersion);

        spc.AddSource(
            Resources.GenNamespace + "_CmdDescDynamic.g.cs",
            SourceText.From(GenerateUsingsHeaderCode(usings) + CodeGenerator.ToSourceCode(rootGroup), Encoding.UTF8)
        );

        SymbolInfoCache.FullReset();
        CommonTypes.Reset();
    }

    static void SanitizeConfig(ref ReclineConfig config, SourceProductionContext spc) {
        if (config.ColumnLength is null or <= 0) {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.InvalidValueForProjectProperty,
                    Location.None,
                    ReclineConfig.COLUMN_LENGTH_PROP_NAME
                )
            );

            config = config with { ColumnLength = ReclineConfig.DEFAULT_COLUMN_LENGTH };
        }

        if (config.HelpExitCode is null) {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.InvalidValueForProjectProperty,
                    Location.None,
                    ReclineConfig.HELP_EXIT_CODE_PROP_NAME
                )
            );

            config = config with { HelpExitCode = ReclineConfig.DEFAULT_HELP_EXIT_CODE };
        }
    }

    static string GenerateUsingsHeaderCode(ImmutableArray<string> usings)
        => usings.IsDefaultOrEmpty
         ? ""
         : "using " + String.Join(";\nusing ", usings) + ";\n\n";
}