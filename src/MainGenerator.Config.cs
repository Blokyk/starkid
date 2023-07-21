using Microsoft.CodeAnalysis.Diagnostics;

namespace Recline.Generator;

public record ReclineConfig(
    int ColumnLength,
    int HelpExitCode,
    LanguageVersion LanguageVersion
);

public partial class MainGenerator
{
    public const string COLUMN_LENGTH_PROP_NAME = "ReclineHelpColumnLength";
    public const string HELP_EXIT_CODE_PROP_NAME = "ReclineHelpExitCode";

    public const int DEFAULT_COLUMN_LENGTH = 80, DEFAULT_HELP_EXIT_CODE = 1;

    static ReclineConfig ParseConfig(AnalyzerConfigOptions analyzerConfig, LanguageVersion langVersion, SourceProductionContext spc) {
        int columnLength
            = GetProp(
                COLUMN_LENGTH_PROP_NAME,
                DEFAULT_COLUMN_LENGTH,
                Int32.TryParse,
                static columnLength => columnLength is -1 or > 40,
                analyzerConfig,
                spc
            );

        int helpExitCode
            = GetProp(
                HELP_EXIT_CODE_PROP_NAME,
                DEFAULT_HELP_EXIT_CODE,
                Int32.TryParse,
                analyzerConfig,
                spc
            );

        return new(columnLength, helpExitCode, langVersion);
    }

    delegate bool TryParser<T>(string str, out T val);
    private static T GetProp<T>(string key, T defaultVal, TryParser<T> parse, AnalyzerConfigOptions config, SourceProductionContext spc)
        => GetProp(key, defaultVal, parse, (_) => true, config, spc);
    private static T GetProp<T>(string key, T defaultVal, TryParser<T> parse, Func<T, bool> validate, AnalyzerConfigOptions config, SourceProductionContext spc) {
        if (!config.TryGetValue("build_property." + key, out var str))
            return defaultVal;

        if (!parse(str, out var res) || !validate(res)) {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.InvalidValueForProjectProperty,
                    Location.None,
                    key
                )
            );

            return defaultVal;
        }

        return res;
    }
}