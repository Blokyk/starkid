using StarKid;

namespace StarKid.Tests.NameCasing;

[CommandGroup("test")]
public static class Main {
    [Option("opt1")]
    public static string? SomeVal;

    [Option("opt2")]
    public static int URL_max_length;

    [Option("opt3")]
    public static string? s_someNightmarish_VARName;

    [Command("dummy")]
    public static void Dummy(
        string SomeVal,
        int URL_max_length,
        string s_someNightmarish_VARName
    ) { }
}