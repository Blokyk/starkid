using Microsoft.CodeAnalysis.Diagnostics;

namespace Recline.Generator;

public record struct ReclineConfig(
    int? ColumnLength,
    int? HelpExitCode
) {
    public const string COLUMN_LENGTH_PROP_NAME = "ReclineColumnLength";
    public const string HELP_EXIT_CODE_PROP_NAME = "ReclineHelpExitCode";
    public const int DEFAULT_COLUMN_LENGTH = 80, DEFAULT_HELP_EXIT_CODE = 1;

    public static ReclineConfig Parse(AnalyzerConfigOptions options) {
        var columnLength = TryParseIntProp(COLUMN_LENGTH_PROP_NAME, DEFAULT_COLUMN_LENGTH, options);
        var helpExitCode = TryParseIntProp(HELP_EXIT_CODE_PROP_NAME, DEFAULT_HELP_EXIT_CODE, options);

        return new(columnLength, helpExitCode);
    }

    private static int? TryParseIntProp(string key, int defaultVal, AnalyzerConfigOptions options) {
        if (!options.TryGetValue("build_property." + key, out var str))
            return defaultVal;

        if (!Int32.TryParse(str, out var num))
            return null;

        return num;
    }
}