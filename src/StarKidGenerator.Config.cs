using Microsoft.CodeAnalysis.Diagnostics;

namespace StarKid.Generator;

public record StarKidConfig(
    int ColumnLength,
    int HelpExitCode,
    bool AllowRepeatingOptions,
    NameCasing ArgNameCasing,
    LanguageVersion LanguageVersion
);

public partial class StarKidGenerator
{
    public const string COLUMN_LENGTH_PROP_NAME = "StarKid_Help_MaxCharsPerLine";
    public const string HELP_EXIT_CODE_PROP_NAME = "StarKid_Help_ExitCode";
    public const string REPEATED_OPT_PROP_NAME = "StarKid_AllowRepeatingOptions";
    public const string NAMING_CONV_PROP_NAME = "StarKid_Help_ArgNameCasing";

    public const int DEFAULT_COLUMN_LENGTH = 80, DEFAULT_HELP_EXIT_CODE = 1;
    public const bool DEFAULT_REPEATED_OPT = false;
    public const NameCasing DEFAULT_NAMING_CONV = NameCasing.KebabCase;

    static StarKidConfig ParseConfig(AnalyzerConfigOptions analyzerConfig, LanguageVersion langVersion, Action<Diagnostic> addDiagnostic) {
        int columnLength
            = GetProp(
                COLUMN_LENGTH_PROP_NAME,
                DEFAULT_COLUMN_LENGTH,
                Int32.TryParse,
                static columnLength => columnLength is -1 or > 40,
                analyzerConfig,
                addDiagnostic
            );

        // special case: -1 disables the limit entirely
        if (columnLength == -1) columnLength = Int32.MaxValue;

        int helpExitCode
            = GetProp(
                HELP_EXIT_CODE_PROP_NAME,
                DEFAULT_HELP_EXIT_CODE,
                Int32.TryParse,
                analyzerConfig,
                addDiagnostic
            );

        bool allowRepeatingOptions
            = GetProp(
                REPEATED_OPT_PROP_NAME,
                DEFAULT_REPEATED_OPT,
                Boolean.TryParse,
                analyzerConfig,
                addDiagnostic
            );

        var namingConv
            = GetProp(
                NAMING_CONV_PROP_NAME,
                DEFAULT_NAMING_CONV,
                NameCasingUtils.TryParse,
                analyzerConfig,
                addDiagnostic
            );

        return new(columnLength, helpExitCode, allowRepeatingOptions, namingConv, langVersion);
    }

    delegate bool TryParser<T>(string str, out T val);
    private static T GetProp<T>(string key, T defaultVal, TryParser<T> parse, AnalyzerConfigOptions config, Action<Diagnostic> addDiagnostic)
        => GetProp(key, defaultVal, parse, (_) => true, config, addDiagnostic);
    private static T GetProp<T>(string key, T defaultVal, TryParser<T> parse, Func<T, bool> validate, AnalyzerConfigOptions config, Action<Diagnostic> addDiagnostic) {
        if (!config.TryGetValue("build_property." + key, out var str)) {
            addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.ConfigPropNotVisible,
                    Location.None,
                    key
                )
            );

            return defaultVal;
        }

        if (!parse(str, out var res) || !validate(res)) {
            addDiagnostic(
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