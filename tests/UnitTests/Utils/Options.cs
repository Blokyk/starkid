namespace StarKid.Tests;

internal static class CompilationOptions
{
    public static readonly CSharpCompilationOptions DefaultConsole =
        new(
            OutputKind.ConsoleApplication,
            specificDiagnosticOptions: new[] { KeyValuePair.Create("CLI006", ReportDiagnostic.Suppress) }
        );

    public static readonly CSharpCompilationOptions DefaultLibrary =
        new(
            OutputKind.DynamicallyLinkedLibrary,
            specificDiagnosticOptions: new[] { KeyValuePair.Create("CLI006", ReportDiagnostic.Suppress) }
        );
}

internal static class ParseOptions
{
    public static readonly CSharpParseOptions Default =
        new(
            LanguageVersion.Default,
            DocumentationMode.Parse
        );
}

internal static class DriverOptions
{
    public static readonly GeneratorDriverOptions Default =
        new(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true
        );
}