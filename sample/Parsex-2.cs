#nullable enable

using System;
using Recline;

namespace SomeStuff;

using System.IO;

[CommandGroup("parsex", DefaultCmdName = "silent")]
[Description("A parser/typechecker for lotus")]
public static partial class Parsex
{
    [Option("verbose", 'v')]
    public static bool verboseFlag;

    [Option("diagnostics", 'd')]
    public static bool diagnosticsFlag;

    [Option("int", 'u')]
    public static bool @int;

    [Option("force", shortName: 'f')]
    [Description("Ignore parsing/compilation errors before executing commands")]
    public static bool forceOption = false;

    private static FileInfo? _outputFile;
    [Option("output", ArgName = "file")]
    [Description("The file to output stuff to, instead of stdin")]
    public static FileInfo OutputFile {
        get => _outputFile ?? new FileInfo("null");
        set {
            if (!value.Exists && value.Name != "-")
                throw new ArgumentException();

            _outputFile = value;
        }
    }

    public static (int start, int end) rangeInfo;

    [Command("silent")]
    [Description("Don't print anything to stdout (errors go to stderr)")]
    public static void Silent() {
        if (forceOption)
            Console.WriteLine("Silently forcing ??");
        else
            Console.WriteLine("👁️   _ 👁️");

        Dump();
    }

    [Command("count")]
    public static int CheckAll(string anotherOne, [Description("The list of files to count")] params string[] files) {
        foreach (var file in files) {
            Console.WriteLine(file);
        }

        return files.Length;
    }

    [Command("print")]
    public static int Print(
        [Description("some filename idk")] FileInfo filename
    ) {
        Console.WriteLine("File " + filename.FullName + " does" + (filename.Exists ? "n't" : "") + " exist");
        return filename.Exists ? 0 : 1;
    }

    [Description("Print the hash of the AST graph")]
    [Command("hash")]
    public static int Hash(
        [Description("fileDesc")] FileInfo? file = null
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
        [Command("syntax")]
        public static int GraphSyntax()
            => constOption ? 0xf : 'g' % 0xf;

        [Command("const")]
        public static int GraphConst(
            [Option("range", 'r')] bool range
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