#nullable enable

using System;
using System.Linq;
using System.IO;

using Recline;

namespace SomeStuff;

public record Stuff(int i);

[CLI("lotus", EntryPoint = nameof(Silent), HelpExitCode = 0)]
[Description("A parser/typechecker for lotus")]
public static partial class Lotus
{
    [Option("force", shortName: 'f')]
    [Description("Ignore parsing/compilation errors before executing commands")]
    public static bool forceOption = false;

    internal static Stuff? ParseStuff(string? str) {
        if (str is null)
            return null;
        else if (Int32.TryParse(str, out var i))
            return new Stuff(i);
        else
            return new Stuff(0);
    }

    internal static bool CheckStuff(Stuff? s) => s?.i >= 0;

    internal static int? ParseInt(string? arg) => arg is null ? 0 : Int32.Parse(arg);

    [Option("log-level")]
    [ParseWith(nameof(ParseStuff))]
    [ValidateWith(nameof(CheckStuff))]
    public static Stuff? logLevel = new(5);

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
    public static void Silent(bool arg) {
        if (forceOption)
            Console.WriteLine("Silently forcing ??");
        else
            Console.WriteLine("👁️  _ 👁️");

        Dump(others: arg);
    }

    [Command("count")]
    public static int CheckAll(string anotherOne, [Description("The list of files to count")] params string[] files) => files.Length;

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

public static partial class Lotus {
    [Command("graph")]
    public static int Graph(
        [Option("const", 'c')] bool constOption
    ) => constOption ? 0xf : 'g' % 0xf;

    [SubCommand("const", nameof(Graph))]
    public static int GraphConst(
        [Option("range", 'r')] bool range
    ) {
        Console.WriteLine("range = " + range);
        Console.WriteLine("Oh getting fancy tonight");
        return 0xf;
    }

    static void Dump([System.Runtime.CompilerServices.CallerArgumentExpression("others")] string expr = "", params object[] others)
        => Console.WriteLine($@"
            forceOption = {forceOption}
            OutputFile = {OutputFile}
            rangeInfo = {rangeInfo}
            {expr} = {String.Join(", ", others.Select(o => o.ToString()))}
        ");
}