namespace Recline.Generator;

internal static class Diagnostics {
    public static readonly DiagnosticDescriptor TimingInfo
        = new DiagnosticDescriptor(
            "CLI000",
            "{0} took: {1}ms",
            "{0} took: {1}ms",
            "Debug",
            DiagnosticSeverity.Info,
            true
        );

    public static readonly DiagnosticDescriptor CmdMustBeStatic
        = new DiagnosticDescriptor(
            "CLI010",
            "Method '{0}' must be static to be a command.",
            "Methods marked with [Command] must be static.",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );
}