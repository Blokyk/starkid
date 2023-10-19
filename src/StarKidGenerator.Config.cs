using Microsoft.CodeAnalysis.Diagnostics;

namespace StarKid.Generator;

public record StarKidConfig(
    int ColumnLength,
    int HelpExitCode,
    bool AllowRepeatingOptions,
    LanguageVersion LanguageVersion
);

public partial class StarKidGenerator
{
    public const string COLUMN_LENGTH_PROP_NAME = "StarKid_Help_MaxCharsPerLine";
    public const string HELP_EXIT_CODE_PROP_NAME = "StarKid_Help_ExitCode";
    public const string REPEATED_OPT_PROP_NAME = "StarKid_AllowRepeatingOptions";

    public const int DEFAULT_COLUMN_LENGTH = 80, DEFAULT_HELP_EXIT_CODE = 1;
    public const bool DEFAULT_REPEATED_OPT = false;

    static StarKidConfig ParseConfig(AnalyzerConfigOptions analyzerConfig, LanguageVersion langVersion, SourceProductionContext spc) {
        int columnLength
            = GetProp(
                COLUMN_LENGTH_PROP_NAME,
                DEFAULT_COLUMN_LENGTH,
                Int32.TryParse,
                static columnLength => columnLength is -1 or > 40,
                analyzerConfig,
                spc
            );

        // special case: -1 disables the limit entirely
        if (columnLength == -1) columnLength = Int32.MaxValue;

        int helpExitCode
            = GetProp(
                HELP_EXIT_CODE_PROP_NAME,
                DEFAULT_HELP_EXIT_CODE,
                Int32.TryParse,
                analyzerConfig,
                spc
            );

        bool allowRepeatingOptions
            = GetProp(
                REPEATED_OPT_PROP_NAME,
                DEFAULT_REPEATED_OPT,
                Boolean.TryParse,
                analyzerConfig,
                spc
            );

        return new(columnLength, helpExitCode, allowRepeatingOptions, langVersion);
    }

    delegate bool TryParser<T>(string str, out T val);
    private static T GetProp<T>(string key, T defaultVal, TryParser<T> parse, AnalyzerConfigOptions config, SourceProductionContext spc)
        => GetProp(key, defaultVal, parse, (_) => true, config, spc);
    private static T GetProp<T>(string key, T defaultVal, TryParser<T> parse, Func<T, bool> validate, AnalyzerConfigOptions config, SourceProductionContext spc) {
        if (!config.TryGetValue("build_property." + key, out var str)) {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.ConfigPropNotVisible,
                    Location.None,
                    key
                )
            );

            return defaultVal;
        }

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