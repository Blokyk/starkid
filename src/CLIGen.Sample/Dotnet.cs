#nullable enable

using System;
using CLIGen;

namespace SomeStuff;

using System.IO;

[CLI("dotnet", HelpExitCode = 1)]
[Description("Execute a .NET application")]
public static partial class Dotnet {
    [Option("verbose", 'v')] public static bool verboseFlag;
    [Option("diagnostics", 'd')] public static bool diagnosticsFlag;

    [Option("version")]
    public static void PrintVersion() {
        Console.WriteLine("v1.0.58");
        Environment.Exit(0);
    }

    public static int Exec(string filename) {
        return File.Exists(filename) ? 0 : 128;
    }

    [Command("build")]
    public static int Build(
        [Option("framework", 'f')] string framework,
        [Option("verbose", 'v')] bool verbose = false,
        [Option("config", 'c')] string config = "Debug",
        string? project = null
    ) {
        return config == "Debug" && !verbose ? 1 : 0;
    }

    [Command("run")]
    [Description("Build and run the current project")]
    public static int Run(
        string? project = null
    ) {
        return (project is null ? 1 : -1) + Build("net6", false, "Release", project);
    }
}

public static partial class Dotnet {
    [SubCommand("clean", nameof(Build))]
    public static void BuildClean() {
        Console.WriteLine("Cleaning up " + new Random().Next(0, 10) + " projects...");
    }
}