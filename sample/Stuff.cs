using StarKid;

using System;
using System.IO;

[CommandGroup("doer")]
public static class MyApp {
    [Option("log-mode", IsGlobal = true)]
    public static FileMode logMode = FileMode.Append;

    public static int ParseNegInt(string s) => -Int32.Parse(s);

    public static int? ParserNullableInt(string s) => Int32.Parse(s);

    [Option("resolution")]
    public static int[] resolutions = Array.Empty<int>();

    public static string whatever(string s) => null!;
    [Option("ext", IsGlobal = true)]
    // [ParseWith(nameof(whatever))]
    public static string[] ext = Array.Empty<string>();

    [Option("something")]
    // [ParseWith(nameof(ParserNullableInt))]
    [ValidateWith(nameof(Int32.IsPositive))]
    public static int? SomeOpt = null;

    /// <summary>
    /// announce some name and then maybe numbers idk
    /// </summary>
    /// <param name="name">this is a name</param>
    /// <param name="isQuiet">should it be quiet</param>
    /// <param name="nums">so numbers</param>
    [Command("announce")]
    public static void Announce(string name, [Option("quiet")] bool isQuiet, [ParseWith(nameof(ParseNegInt))] params int[] nums) {
        var displayName = isQuiet ? name : name.ToUpperInvariant();
        Console.WriteLine($"Here comes... {displayName}!");

        Console.WriteLine("ext = ");
        foreach (var s in ext) Console.WriteLine($"- '{s}'");

        foreach (var i in nums)
    		Console.WriteLine(i);

        Console.WriteLine("Resolutions:");
        foreach (var res in resolutions)
            Console.WriteLine(res);
    }
}
