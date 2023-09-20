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

    private static void DisplayTimes(Dictionary<string, IEnumerable<TimeSpan>> namedTimes) {
        var maxLength = namedTimes.Max(kv => kv.Key.Length);

        var lineSeparator = new string('-', maxLength + 25);
        var spacing = new string(' ', maxLength + 4);

        foreach (var (stepName, stepTimes) in namedTimes) {
            var name = stepName;
            var runtime = stepTimes.Aggregate((t1, t2) => t1 + t2);

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

    public static void DisplayAllSteps(GeneratorRunResult results) =>
        DisplayTimes(
            results.TrackedSteps
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Select(s => s.ElapsedTime)
                )
        );

    public static void DisplayStarKidSteps(GeneratorRunResult results) {
        var steps = results.TrackedSteps.Where(kv => kv.Key.StartsWith("starkid_")).ToDictionary(kv => kv.Key, kv => kv.Value);

        if (results.TrackedOutputSteps.TryGetValue("ImplementationSourceOutput", out var outputStep)) {
            steps.Add(
                "starkid_output",
                outputStep
            );
        } else {
            Console.WriteLine("\x1b[2m-- no output --\x1b[0m");
        }

        if (steps.Count == 0) {
            Console.WriteLine("No starkid steps were run.");
            return;
        }

        // [8..] to remove the "starkid_" prefix
        DisplayTimes(steps.ToDictionary(kv => kv.Key[8..], kv => kv.Value.Select(s => s.ElapsedTime)));
    }
}