using StarKid.Generator;

namespace StarKid.Tests;

internal static class Driver
{
    public static GeneratorDriver Create() => Create(ParseOptions.Default);
    public static GeneratorDriver Create(CSharpParseOptions parseOptions) => Create(parseOptions, ConfigProvider.Empty);

    public static GeneratorDriver Create(CSharpParseOptions parseOptions, AnalyzerConfigOptionsProvider configProvider)
        => CSharpGeneratorDriver.Create(
            new[] { new MainGenerator().AsSourceGenerator() },
            parseOptions: parseOptions,
            optionsProvider: configProvider,
            driverOptions: DriverOptions.Default
        );
}