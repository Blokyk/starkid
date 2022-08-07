using CLIGen;

[CLI("parsex")]
[Description("A parser/typechecker for lotus")]
public static partial class Parsex
{
    [Option("force", shortName: 'f')]
    [Description("Ignore compilation errors before executing commands")]
    public static bool forceOption = false;

#nullable disable
    private static FileInfo _outputFile;
    [Option("output", ArgName = "file")]
    [Description("The file to output stuff to, instead of stdin")]
    public static FileInfo OutputFile {
        get => _outputFile;
        set {
            if (!value.Exists && value.Name != "-")
                throw new ArgumentException();

            _outputFile = value;
        }
    }
#nullable restore

    public static (int start, int end) rangeInfo;

    [Option("range", shortName: 'r', ArgName = "range")]
    public static void ParseRange(string rawStr) {

        // 'void' here could also be replaced with bool, for auto/generated
        // error handling

        var parts = rawStr.Split('-');

        if (parts.Length != 2)
            throw new FormatException("--range needs two numbers separated by a dash, and no space");

        rangeInfo = (Int32.Parse(parts[0]), Int32.Parse(parts[1]));
    }

    [Command("silent")]
    [Description("Don't print anything to stdout (errors go to stderr)")]
    public static void Silent() {
        if (forceOption)
            Console.WriteLine("Silently forcing ??");
        else
            Console.WriteLine("👁️  _ 👁️");
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

    [Command(nameof(Graph))]
    public static int Graph(
        [Option("const", shortName: 'c')] bool constOption
    ) => constOption ? 0xf : 'g' % 0xf;

    [SubCommand("const", nameof(Graph), InheritOptions = false)]
    public static int GraphConst() {
        Console.WriteLine("Oh getting fancy tonight");
        return 0xf;
    }
}

public static partial class Parsex {
    public const string DO_YOU_SEE_IT_YEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEET = ".";
}