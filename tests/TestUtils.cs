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

    public static void DisplaySteps(GeneratorRunResult results) {
        var maxLength = 0;

        var steps = results.TrackedSteps.Where(kv => kv.Key.StartsWith("recline_")).ToDictionary(kv => kv.Key, kv => kv.Value);

        if (results.TrackedOutputSteps.Any(s => s.Key == "ImplementationSourceOutput")) {
            steps.Add(
                "recline_output",
                results.TrackedOutputSteps.Single(s => s.Key == "ImplementationSourceOutput").Value
            );
        } else {
            Console.WriteLine("\x1b[2m-- no output --\x1b[0m");
        }

        if (steps.Count == 0) {
            Console.WriteLine("No recline steps were run.");
            return;
        }

        foreach (var (stepName, _) in steps) {
            // - 8 for the "recline_" prefix
            if (maxLength < stepName.Length - 8)
                maxLength = stepName.Length - 8;
        }

        var lineSeparator = new string('-', maxLength + 25);
        var spacing = new string(' ', maxLength + 4);

        foreach (var (stepName, step) in steps) {
            var name = stepName[8..];
            var runtime = step[0].ElapsedTime;

            Console.Write("\x1b[2m");
            Console.WriteLine(lineSeparator);
            Console.Write("\x1b[0m| ");

            Console.Write(name);
            Console.Write(spacing[name.Length..]);
            Console.Write($"\x1b[2m|\x1b[0m    {runtime.TotalMilliseconds,4:#,##0}\x1b[2mms\x1b[0m {runtime.Microseconds,3:##0}\x1b[2mus\x1b[0m ");

            Console.WriteLine("\x1b[2m|\x1b[0m");
        }

        Console.WriteLine(lineSeparator);
    }
}