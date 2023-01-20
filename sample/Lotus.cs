using System;
using System.IO;

using Recline;

[CommandGroup("upject")]
public static class Lotus
{
    [Option("verbose", 'V')]
    public static bool canBeVerbose = false;

    public static void PrintIsVerbose() => Console.WriteLine(canBeVerbose);

    [Command("clean")]
    public static int Clean(
        string globFilter
    ) {
        PrintIsVerbose();
        return 0;
    }

    public static bool FileExists(FileInfo f) => f.Exists;

    [Command("new")]
    public static int SetupNewProject(
        string projectName,
        [ValidateWith(nameof(FileInfo.Exists))] FileInfo? outputDir = null, // argument optionnel ('?' -> la variable peut être nulle)
        [Option("framework")] string framework = "latest"
    ) { return 0; }
}
