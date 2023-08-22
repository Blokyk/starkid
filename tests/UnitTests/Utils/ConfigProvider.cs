using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Recline.Tests;

internal sealed class ConfigProvider : AnalyzerConfigOptionsProvider
{
    public static readonly ConfigProvider Empty = new();

    private readonly Dictionary<string, AnalyzerConfigOptions> _fileSpecificOptions;

    public ConfigProvider(
        Dictionary<string, string>? globalOptions = null,
        Dictionary<string, AnalyzerConfigOptions>? fileSpecificOptions = null
    ) {
        GlobalOptions = globalOptions is null ? EmptyConfigOptions.Instance : new DictionaryConfigOptions(globalOptions);
        _fileSpecificOptions = fileSpecificOptions ?? new();
    }

    public override AnalyzerConfigOptions GlobalOptions { get; }
    public override AnalyzerConfigOptions GetOptions(RS.SyntaxTree tree) => _fileSpecificOptions.TryGetValue(Path.GetFileName(tree.FilePath), out var options) ? options : EmptyConfigOptions.Instance;
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => EmptyConfigOptions.Instance;

    private sealed class DictionaryConfigOptions : AnalyzerConfigOptions {
        private readonly Dictionary<string, string> _options = new();
        public DictionaryConfigOptions(Dictionary<string, string> options) => _options = options;
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _options.TryGetValue(key, out value);
    }

    private sealed class EmptyConfigOptions : AnalyzerConfigOptions {
        public static readonly EmptyConfigOptions Instance = new();
        private EmptyConfigOptions() { }
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) { value = null; return false; }
    }
}