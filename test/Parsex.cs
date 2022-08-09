#nullable enable

using System;
using CLIGen;

namespace SomeStuff;

using System.IO;

[CLI("parsex", EntryPoint = nameof(Silent))]
[Description("A parser/typechecker for lotus")]
public static partial class Parsex
{
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

    [Option("range", shortName: 'r', ArgName = "range")]
    public static Exception? ParseRange(string? rawStr) {

        // This method can return 'void' for manual error handling,
        // or it could use one of :
        //      - bool
        //      - int
        //      - string
        //      - Exception

        var parts = rawStr?.Split('-') ?? Array.Empty<string>();

        if (parts.Length != 2)
            return new FormatException("--range needs two numbers separated by a dash, and no space");

        rangeInfo = (Int32.Parse(parts[0]), Int32.Parse(parts[1]));
        return null;
    }

    [Command("silent")]
    [Description("Don't print anything to stdout (errors go to stderr)")]
    public static void Silent() {
        if (forceOption)
            Console.WriteLine("Silently forcing ??");
        else
            Console.WriteLine("👁️  _ 👁️");

        Dump();
    }

    [Command("print")]
    public static int Print(
        [Description("fileDesc")] FileInfo file
    ) {
        Console.WriteLine("File " + file.FullName + " does" + (file.Exists ? "n't" : "") + " exist");
        return file.Exists ? 0 : 1;
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
        if (forceOption)
            Console.WriteLine("Forcefully Default, av-- wait that's just my teenage years");
        else
            Console.WriteLine("Bravely Default, available now in your terminal!");
        return 0;
    }
}

public static partial class Parsex {
    [Command("graph")]
    public static int Graph(
        [Option("const", 'c')] bool constOption
    ) => constOption ? 0xf : 'g' % 0xf;

    [SubCommand("const", nameof(Graph), InheritOptions = false)]
    public static int GraphConst() {
        Console.WriteLine("Oh getting fancy tonight");
        return 0xf;
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