#nullable enable

using System;
using Recline;
using System.IO;

namespace SomeStuff;

public static class FrenchRiseUp {
    public static bool AsBool(string? rawArg)
        => rawArg switch {
            "vrai" => true,
            "faux" => false,
            _ => throw new Exception()
        };

    public static int? AsInt(string? rawArg)
        => rawArg switch {
            "zéro" => 0,
            "un" => 1,
            "max" => Int32.MaxValue,
            _ => null,
        };

}

[CommandGroup("dotnet", ShortDesc = "Execute a .NET application")]
public static partial class Dotnet {
    // [ParseWith(nameof(FrenchRiseUp.AsBool))]
    [Option("verbose", 'v', IsGlobal = true)]
    public static bool verboseFlag;

    [ParseWith(nameof(FrenchRiseUp.AsBool))]
    [Option("diagnostics", 'd')]
    public static bool diagnosticsFlag;

    [Option("int", 'u')]
    [ParseWith(nameof(FrenchRiseUp.AsBool))]
    public static bool @int;

    public static int Exec(string filename) {
        return File.Exists(filename) ? 0 : 128;
    }

    internal static bool IntIsPositive(int? i)
        => i > 0;

    /// <summary>
    /// Build and run the current project
    /// </summary>
    [Command("run")]
    public static int Run(
        string? project = null
    ) {
        return (project is null ? 1 : -1) + Build.BuildProject(false, "Release", project);
    }
}

public static partial class Dotnet {
    [CommandGroup("build", DefaultCmdName = "project")]
    public static class Build {
        [Option("framework", 'f', IsGlobal = true)]
        [ValidateWith(nameof(IntIsPositive), "Framework version must be positive")]
        [ParseWith(nameof(FrenchRiseUp.AsInt))]
        public static int? framework;

        [Option("directory", 'd', IsGlobal = true)]
        public static FileInfo? outputDir;

        [Command("project")]
        public static int BuildProject(
            [Option("very-verbose", 'V')] bool verbose = false,
            [Option("config", 'c')] string config = "Debug",
            string? project = null
        ) {
            Console.WriteLine("framework = " + framework);
            Console.WriteLine("project = " + project);
            return config == "Debug" && !verbose ? 1 : 0;
        }

        [Command("solution")]
        public static void BuildSolution(params string[] projectNames) {
            Console.WriteLine("Cleaning up " + new Random().Next(0, 10) + " projects...");
        }
    }
}