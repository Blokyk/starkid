using System.Numerics;

namespace StarKid.Tests.Options;

public static partial class Main {
    #region Parse
    internal static string StringToUpper(string s) => s.ToUpper();
    [ParseWith(nameof(StringToUpper))]
    [Option("parsed-str-opt")] public static string ParsedStringOption { get; set; } = "blank1";

    internal static KeyValuePair<string, string> ParseStringPair(string s) {
        var parts = s.Split(':', 2);
        return KeyValuePair.Create(String.Intern(parts[0]), String.Intern(parts[1]));
    }
    [ParseWith(nameof(ParseStringPair))]
    [Option("manual-mscorlib-opt")] public static KeyValuePair<string, string> ManualLibOption { get; set; }

    [ParseWith(nameof(Utils.ParseUnixFileMode))]
    [Option("manual-enum-opt")] public static UnixFileMode ManualEnumOption { get; set; }

    [ParseWith(nameof(AsciiString.From))]
    [Option("manual-user-opt")] public static AsciiString ManualFooOption { get; set; }

    public static int? ParseNullableInt(string s) => Int32.TryParse(s, out var res) ? res : default;
    [Option("manual-parsed-nullable-struct-opt")]
    [ParseWith(nameof(ParseNullableInt))] public static int? ManualParsedNullableStructOption { get; set; }

    // ensure parser nullability variance
    [Option("direct-parsed-nullable-struct-opt")]
    [ParseWith(nameof(Int32.Parse))] public static int? DirectParsedNullableStructOption { get; set; }

#pragma warning disable CS8618 // see #41
    [ParseWith(nameof(StringToUpper))]
    [Option("repeat-manual-item-opt")] public static string[] RepeatManualItemOption { get; set; }
#pragma warning restore

    public static char[] AsUpperCharArray(string s) => s.ToUpper().ToCharArray();
    [ParseWith(nameof(AsUpperCharArray))]
    [Option("manual-array-opt")] public static char[]? ManualArrayOption { get; set; }

    public static T ParseNumber<T>(string? s) where T : INumber<T> => T.Parse(s, null);
    [ParseWith(nameof(ParseNumber))]
    [Option("generic-parser-opt")] public static int GenericParserOption { get; set; }

#pragma warning disable CS8618 // see #41
    [ParseWith(nameof(ParseNumber))]
    [Option("repeatable-generic-parser-opt")] public static int[] RepeatableGenericParserOption { get; set; }
#pragma warning restore

    #endregion

    #region TryParse
    // todo: ParseWith(TryParse)
    #endregion
}

public partial class Tests {
    public class ParseWith {
        [Fact]
        public void ParsedStrOption() {
            TestMainDummy("--parsed-str-opt", "hello");
            AssertStateChange(new { ParsedStringOption = "HELLO" });

            TestMainDummy("--parsed-str-opt=!DlroW");
            AssertStateChange(new { ParsedStringOption = "!DLROW" });
        }

        [Fact]
        public void ManualLibOption() {
            TestMainDummy("--manual-mscorlib-opt", "foo:bar");
            AssertStateChange(new { ManualLibOption = KeyValuePair.Create("foo", "bar") });
            TestMainDummy("--manual-mscorlib-opt=foo:bar");
            AssertStateChange(new { ManualLibOption = KeyValuePair.Create("foo", "bar") });

            // check that ':' doesn't fuck with things
            TestMainDummy("--manual-mscorlib-opt", ":bar");
            AssertStateChange(new { ManualLibOption = KeyValuePair.Create("", "bar") });
            TestMainDummy("--manual-mscorlib-opt=:bar");
            AssertStateChange(new { ManualLibOption = KeyValuePair.Create("", "bar") });
        }

        [Fact]
        public void ManualEnumOption() {
            TestMainDummy("--manual-enum-opt", "rwxrw-r--");
            AssertStateChange(new { ManualEnumOption = Utils.ParseUnixFileMode("rwxrw-r--") });
        }

        [Fact]
        public void ManualFooOption() {
            TestMainDummy("--manual-user-opt", "no-utf8");
            AssertStateChange(new { ManualFooOption = AsciiString.From("no-utf8") });
        }

        [Fact]
        public void ManualParsedNullableStructOption() {
            TestMainDummy("--manual-parsed-nullable-struct-opt", "47");
            AssertStateChange(new { ManualParsedNullableStructOption = 47 });
        }

        [Fact]
        public void DirectParsedNullableStructOption() {
            TestMainDummy("--direct-parsed-nullable-struct-opt", "47");
            AssertStateChange(new { DirectParsedNullableStructOption = 47 });
        }

        [Fact]
        public void RepeatManualItemOption() {
            TestMainDummy("--repeat-manual-item-opt", "hey", "--repeat-manual-item-opt", "hi");
            AssertStateChange(new { RepeatManualItemOption = (string[])[ "HEY", "HI" ] });
        }

        [Fact]
        public void ManualArrayOption() {
            TestMainDummy("--manual-array-opt", "hey");
            AssertStateChange(new { ManualArrayOption = (char[])[ 'H', 'E', 'Y' ] });
        }

        [Fact]
        public void GenericParserOption() {
            TestMainDummy("--generic-parser-opt", "56");
            AssertStateChange(new { GenericParserOption = 56 });
        }

        [Fact]
        public void RepeatableGenericParserOption() {
            TestMainDummy("--repeatable-generic-parser-opt", "56", "--repeatable-generic-parser-opt", "-87");
            AssertStateChange(new { RepeatableGenericParserOption = (int[])[56, -87] });
        }
    }
}