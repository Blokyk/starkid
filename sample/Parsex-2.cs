#nullable enable

using System;
using Recline;

namespace SomeStuff;

using System.IO;

/// <summary>
/// A parser/typechecker for lotus.
/// </summary>
[CommandGroup("parsex", DefaultCmdName = "silent")]
public static partial class Parsex
{
    [Option("verbose", 'v')]
    public static bool verboseFlag;

    [Option("diagnostics", 'd')]
    public static bool diagnosticsFlag;

    [Option("int", 'u')]
    public static bool @int;

    /// <summary>
    /// Ignore parsing/compilation errors before executing commands
    /// </summary>
    [Option("force", shortName: 'f')]
    public static bool forceOption = false;

    private static FileInfo? _outputFile;
    /// <summary>
    /// The file to output stuff to, instead of stdin
    /// </summary>
    [Option("output", ArgName = "file")]
    public static FileInfo OutputFile {
        get => _outputFile ?? new FileInfo("null");
        set {
            if (!value.Exists && value.Name != "-")
                throw new ArgumentException();

            _outputFile = value;
        }
    }

    public static (int start, int end) rangeInfo;

    [Command("silent", ShortDesc = "Don't print anything to stdout (errors go to stderr)")]
    public static void Silent() {
        if (forceOption)
            Console.WriteLine("Silently forcing ??");
        else
            Console.WriteLine("👁️   _ 👁️");

        Dump();
    }

    /// <param name="files">The list of files to count</param>
    [Command("count")]
    public static int CheckAll(string anotherOne, params string[] files) {
        foreach (var file in files) {
            Console.WriteLine(file);
        }

        return files.Length;
    }

    /// <param name="filename">Some filename idk</param>
    [Command("print")]
    public static int Print(
        FileInfo filename
    ) {
        Console.WriteLine("File " + filename.FullName + " does" + (filename.Exists ? "n't" : "") + " exist");
        return filename.Exists ? 0 : 1;
    }

    /// <summary>
    /// Print the hash of the AST graph
    /// </summary>
    /// <param name="file">fileDesc</param>
    [Command("hash")]
    public static int Hash(
        FileInfo? file = null
    ) {
        if (file is null) {
            Console.WriteLine("How original...");
        } else {
            Console.WriteLine("Daring today, aren't we ?");
        }

        Console.WriteLine("Could you pass me the salt ?");
        return -1;
    }

    public static int Default() {
        if (!forceOption)
            Console.WriteLine("Forcefully Default, av-- wait that's just my teenage years");
        else
            Console.WriteLine("Bravely Default, available now in your terminal!");
        return 0;
    }
}

public static partial class Parsex {
    [CommandGroup("graph", DefaultCmdName = "syntax")]
    public static class Graph {
        /// <summary>
        /// Color expressions based on whether they are definitely constant or not.
        /// </summary>
        [Option("const")]
        public static bool constOption;

        [Command("syntax")]
        public static int GraphSyntax()
            => constOption ? 0xbeef : 0xdead;

        public static (int, int) ParseRange(string s) {
            var parts = s.Split("..", 2);

            if (parts.Length != 2)
                throw new FormatException("Range must be of the format 'num..num'");

            int start = 0;
            int end = 0;

            if (parts[0].Length != 0)
                start = Int32.Parse(parts[0]);

            if (parts[1].Length != 0)
                end = Int32.Parse(parts[1]);

            return (start, end);
        }

        [Command("const")]
        public static int GraphConst(
            [Option("range", 'r')] [ParseWith(nameof(ParseRange))] (int start, int end) range
        ) {
            Console.WriteLine("range = " + range);
            Console.WriteLine("Oh getting fancy tonight");
            return 0xf;
        }
    }

    static void Dump([System.Runtime.CompilerServices.CallerArgumentExpression("others")] string expr = "", params string[] others) {
        Console.WriteLine($@"
            forceOption = {forceOption}
            OutputFile = {OutputFile}
            rangeInfo = {rangeInfo}
            {expr} = {string.Join(", ", others)}
        ");
    }
}