#nullable enable

using System;
using System.Linq;
using System.IO;

using StarKid;

namespace SomeStuff;

enum Greetings { Hey, Hi }

public record Stuff(int i) {
    public bool IsPositive => i > 0;
}

/// <summary>
/// A toolkit for the lotus language, including a parser,
/// a typechecker, as well as a few visualizing tools.
/// </summary>
/// <remarks>
/// heyo check out those fire verses
/// <para>
/// Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc eget dignissim nunc. Maecenas eu ipsum nibh. Vestibulum vehicula purus ex, ac scelerisque mauris porta et. Cras tempus mi vitae diam hendrerit, vel aliquet velit accumsan. Pellentesque ultricies mauris neque, ac rhoncus velit pharetra nec. Suspendisse potenti. Morbi et magna eget lacus rutrum varius a at nisl. Proin at congue turpis. Praesent vestibulum nibh ut turpis facilisis ornare. Donec blandit est non massa semper volutpat. Donec eget magna tortor.
/// </para>
/// <para>
/// Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Proin ut nibh magna.Nam dolor turpis, egestas sed elit id, tincidunt feugiat felis.Phasellus at mattis dolor, vestibulum hendrerit ipsum.Aliquam et porta nunc. In vitae lorem lectus. Cras sit amet lacinia elit.Praesent quis sodales libero. Morbi maximus arcu at velit rutrum egestas.Praesent sodales erat libero, eu pulvinar dui accumsan in. Cras pretium lacus et nisi laoreet, vel feugiat ex volutpat.
/// </para>
/// wait 'verses' doesn't sound right here
/// </remarks>
[CommandGroup("lotus", DefaultCmdName = "silent", ShortDesc = "A parser/typechecker for lotus")]
public static partial class Lotus
{
    /// <summary>Ignore parsing/compilation errors before executing commands</summary>
    [Option("force", shortName: 'f')]
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

    public static bool FeelLikeIt(Stuff _) => Random.Shared.Next() % 2 == 0;

    [Option("log-level")]
    [ParseWith(nameof(ParseStuff))]
    [ValidateWith(nameof(Stuff.IsPositive))]
    [ValidateWith(nameof(FeelLikeIt), "Welp, looks like the oracle didn't like your value")]
    public static Stuff? logLevel = new(5);

    private static FileInfo? _outputFile;
    /// <summary>The file to output stuff to, instead of stdin</summary>
    [Option("output", ArgName = "file")]
    public static FileInfo OutputFile {
        get => _outputFile ?? new FileInfo("null");
        set {
            if (!value.Exists && value.Name != "-")
                throw new ArgumentException();

            _outputFile = value;
        }
    }

    /// <summary>
    /// Range of possible positron-metastasis levels
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public static (int start, int end) rangeInfo;

    /// <summary>I AM LOOKING RESPECTFULLY <br> here</br>
    /// LOOK AT ME
    ///
    /// multi
    /// line and <para> some paragraph
    /// </para>
    /// on top of that!
    /// </summary>
    /// <param name="arg">heyo that arg stuff is fire</param>
    [Command("silent", ShortDesc = "Don't print anything to stdout (errors go to stderr)")]
    public static void Silent(bool arg) {
        if (forceOption)
            Console.WriteLine("Silently forcing ??");
        else
            Console.WriteLine("👁️  _ 👁️");

        Dump(others: arg);
    }

    /// <param name="files">The list of files to count</param>
    [Command("count")]
    public static int CheckAll(string anotherOne, params string[] files) => files.Length;

    /// <param name="file">fileDesc</param>
    [Command("print")]
    public static int Print(
        FileInfo file
    ) {
        Console.WriteLine("File " + file.FullName + " does" + (file.Exists ? "n't" : "") + " exist");
        return file.Exists ? 0 : 1;
    }

    /// <summary>Print the hash of the AST graph</summary>
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
        if (forceOption)
            Console.WriteLine("Forcefully Default, av-- wait that's just my teenage years");
        else
            Console.WriteLine("Bravely Default, available now in your terminal!");
        return 0;
    }
}

public static partial class Lotus {
    [CommandGroup("graph", DefaultCmdName = "syntax")]
    public static class Graph {
        /// <summary>
        /// Some kind of description.<br/>
        /// Also it has multiple lines
        /// </summary>
        [Option("a-very-long-option-name-that-should-be-shortened", 'c')]
        public static bool constOption;

        [Option("a-normal-opt")]
        public static FileInfo? output;

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

    static void Dump([System.Runtime.CompilerServices.CallerArgumentExpression("others")] string expr = "", params object[] others)
        => Console.WriteLine($@"
            forceOption = {forceOption}
            OutputFile = {OutputFile}
            rangeInfo = {rangeInfo}
            {expr} = {String.Join(", ", others.Select(o => o.ToString()))}
        ");
}