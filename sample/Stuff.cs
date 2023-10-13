using StarKid;

using System;
using System.IO;

[CommandGroup("doer")]
public static class MyApp {
    [Option("log-mode", IsGlobal = true)]
    public static FileMode logMode = FileMode.Append;

    public static int ParseNegInt(string s) => -Int32.Parse(s);

    public static int? ParserNullableInt(string s) => Int32.Parse(s);

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
        foreach (var i in nums) {
    		Console.WriteLine(i);
        }
    }
}
