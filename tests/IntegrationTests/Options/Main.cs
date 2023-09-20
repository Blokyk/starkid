using System.Diagnostics.CodeAnalysis;
using System.Net;

using StarKid.Tests;

namespace StarKid.Tests.Options;

[CommandGroup("test")]
public static partial class OptionTest {
    [Command("dummy")] public static void Dummy() { }

    [Option("switch")] public static bool SimpleSwitch;
    [Option("switch-prop")] public static bool SwitchProp { get; set; }

    [Option("true-switch")] public static bool TrueSwitch = true;

    internal static bool BinaryToBool(string? s) => s switch { "0" => false, "1" => true, null => true, _ => throw new Exception() };
    [ParseWith(nameof(BinaryToBool))]
    [Option("parsed-switch")] public static bool ParsedSwitch { get; set; }

    [Option("str-opt")] public static string StringOption { get; set; } = "blank0";

    // special-cased because of SpecialType.System_Int32
    [Option("int-opt")] public static int IntOption { get; set; }

    // auto mscorlib type
    [Option("auto-lib-opt")] public static IPAddress? AutoLibOption { get; set; }

    [Option("enum-opt")] public static FileMode EnumOption { get; set; } = FileMode.Open;
    [Option("auto-user-opt")] public static AsciiString FooOption { get; set; }

    internal static string StringToUpper(string s) => s.ToUpper();
    [ParseWith(nameof(StringToUpper))]
    [Option("parsed-str-opt")] public static string ParsedStringOption { get; set; } = "blank1";

    internal static KeyValuePair<string, string> ParseStringPair(string s) {
        var parts = s.Split(':', 2);
        return KeyValuePair.Create(string.Intern(parts[0]), string.Intern(parts[1]));
    }
    [ParseWith(nameof(ParseStringPair))]
    [Option("manual-mscorlib-opt")] public static KeyValuePair<string, string> ManualLibOption { get; set; }

    [ParseWith(nameof(Utils.ParseUnixFileMode))]
    [Option("manual-enum-opt")] public static UnixFileMode ManualEnumOption { get; set; }

    [ParseWith(nameof(AsciiString.From))]
    [Option("manual-user-opt")] public static AsciiString ManualFooOption { get; set; }

    [Option("nullable-struct-opt")] public static Int128? NullableStructOption { get; set; }

    // todo: ParseWith(TryParse)
    // todo: validators

    [Option("throwing-setter")] public static string ThrowingOption {
        get => null!;
        set => throw new InvalidOperationException("Faulty setter!");
    }
}

public static partial class OptionTest {
    [Option("global-switch", IsGlobal = true)] public static bool GlobalSwitch { get; set; }

    internal static object Dummy2State = new();
    [Command("dummy2")] public static void Dummy2(
        [Option("cmd-opt-with-default")] int defaultOpt = 5
    ) {
        Dummy2State = new { defaultOpt };
    }
}

public readonly struct AsciiString {
    public string InternalString { get; }
    private AsciiString(string s) => InternalString = s;
    public static AsciiString From(string s) {
        if (TryParse(s, out var res))
            return res;

        var firstNonAsciiChar = s.FirstOrDefault(c => !Char.IsAscii(c));
        throw new InvalidOperationException("Char '" + firstNonAsciiChar + "' is not an ASCII character");
    }

    public static bool TryParse(string s, [MaybeNullWhen(false)] out AsciiString ascii) {
        ascii = default;
        if (!s.All(Char.IsAscii))
            return false;
        ascii = new(s);
        return true;
    }
}