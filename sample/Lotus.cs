using System;
using System.Linq;
using System.IO;

using Recline;

[CommandGroup("upject")]
public static class Lotus
{
    [Option("verbose", 'V', IsGlobal = true)]
    public static bool canBeVerbose = false;

    public static void PrintIsVerbose() => Console.WriteLine(canBeVerbose);

    public static bool NotEmpty(string s) => !String.IsNullOrWhiteSpace(s);
    public static bool IsValidGlob(string s)
        => s.All(c => Char.IsAsciiLetterOrDigit(c) || c == '*');

    [Command("clean")]
    public static int Clean(
        [ValidateWith(nameof(NotEmpty))]
        [ValidateWith(nameof(IsValidGlob), "Cleaning globs can only contain ASCII letters/digits and '*'")]
        string globFilter
    ) {
        Console.WriteLine("Cleaning everything that matches '" + globFilter + "'");
        PrintIsVerbose();
        return 0;
    }

    public static bool FileExists(FileInfo f) => f.Exists;

    [Command("new")]
    public static int SetupNewProject(
        string projectName,
        [ValidateWith(nameof(FileInfo.Exists))] FileInfo? outputDir = null,
        [Option("framework")] string framework = "latest"
    ) {
        Console.WriteLine("Verbose: " + canBeVerbose);
        Console.WriteLine("projectName: " + projectName);
        Console.WriteLine("outputDir: " + outputDir);
        Console.WriteLine("framework: " + framework);
        return 0;
    }
}
