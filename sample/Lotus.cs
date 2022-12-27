using System.IO;

using Recline;

[CLI("upject")]
public static class Lotus
{
    [Command("clean")]
    public static int Clean(
        string globFilter,                         // argument obligatoire
        [Option("verbose", 'V')] bool canBeVerbose // flag --verbose/-V
    ) { return 0; }

    [Command("new")]
    public static int SetupNewProject(
    string projectName,
    FileInfo? outputDir = null, // argument optionel ('?' -> la variable peut être nulle)
    [Option("framework")] string framework = "latest"
    ) { return 0; }
}