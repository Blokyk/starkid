internal static class Utils
{
    public static string FormatSeverity(this Diagnostic diag)
        => "\x1b["
         + (diag.Severity switch {
            DiagnosticSeverity.Error => 31,
            DiagnosticSeverity.Warning => 33,
            DiagnosticSeverity.Info => 34,
            DiagnosticSeverity.Hidden => 32,
            _ => 31
           })
         + $"m{diag.Severity.ToString()[0]}\x1b[0m: ";
}