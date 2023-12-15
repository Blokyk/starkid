namespace StarKid.Tests.Options;

public static partial class Main {
    [Option("str-opt")] public static string StringOption { get; set; } = "blank0";

    // special-cased because of SpecialType.System_Int32
    [Option("int-opt")] public static int IntOption { get; set; }

    // auto mscorlib type
    [Option("auto-lib-opt")] public static Guid AutoLibOption { get; set; }

    [Option("enum-opt")] public static FileMode EnumOption { get; set; } = FileMode.Open;
    [Option("auto-user-opt")] public static AsciiString AutoUserOption { get; set; }

    [Option("auto-parsed-nullable-struct-opt")] public static Int128? AutoParsedNullableStructOption { get; set; }

#pragma warning disable CS8618 // see #41
    [Option("repeatable-str-opt")] public static string[] RepeatableStringOption { get; set; }

    [Option("repeatable-auto-opt")] public static int[] RepeatableAutoOption { get; set; }
#pragma warning restore
}

public partial class Tests {
    public class AutoParser {

        [Fact]
        public void StringOption() {
            TestMainDummy("--str-opt", "hello");
            AssertStateChange(new { StringOption = "hello" });

            TestMainDummy("--str-opt=hello");
            AssertStateChange(new { StringOption = "hello" });
        }

        [Fact]
        public void StringOption_Empty() {
            TestMainDummy("--str-opt", "");
            AssertStateChange(new { StringOption = "" });

            TestMainDummy("--str-opt=");
            AssertStateChange(new { StringOption = "" });
        }

        [Fact]
        public void StringOption_WithSpace() {
            TestMainDummy("--str-opt", "  with spaces");
            AssertStateChange(new { StringOption = "  with spaces" });

            TestMainDummy("--str-opt=  with spaces");
            AssertStateChange(new { StringOption = "  with spaces" });
        }

        [Fact]
        public void StringOption_OptionLikeString() {
            TestMainDummy("--str-opt", "--switch");
            AssertStateChange(new { StringOption = "--switch" });

            TestMainDummy("--str-opt", "/switch");
            AssertStateChange(new { StringOption = "/switch" });

            TestMainDummy("--str-opt=--switch");
            AssertStateChange(new { StringOption = "--switch" });
        }

        [Fact]
        public void IntOption() {
            TestMainDummy("--int-opt", "9");
            AssertStateChange(new { IntOption = 9 });

            TestMainDummy("--int-opt", "-12");
            AssertStateChange(new { IntOption = -12 });
        }

        [Fact]
        public void AutoLibOption() {
            TestMainDummy("--auto-lib-opt", "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
            AssertStateChange(new { AutoLibOption = Guid.Parse("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC") });
        }

        [Fact]
        public void EnumOption() {
            TestMainDummy("--enum-opt", "CreateNew");
            AssertStateChange(new { EnumOption = FileMode.CreateNew });
        }

        [Fact]
        public void AutoUserOption() {
            TestMainDummy("--auto-user-opt", "ascii");
            AssertStateChange(new { AutoUserOption = AsciiString.From("ascii") });
        }

        [Fact]
        public void AutoParsedNullableStructOption() {
            TestMainDummy("--auto-parsed-nullable-struct-opt", "123456789");
            AssertStateChange(new { AutoParsedNullableStructOption = (Int128?)123456789 });
        }

        [Fact]
        public void RepeatableStringOption() {
            TestMainDummy("--repeatable-str-opt", "hello");
            AssertStateChange(new { RepeatableStringOption = (string[])[ "hello" ] });

            TestMainDummy("--repeatable-str-opt", "hello", "--repeatable-str-opt", "world");
            AssertStateChange(new { RepeatableStringOption = (string[])[ "hello", "world" ] });

            TestMainDummy();
            AssertStateChange(new { RepeatableStringOption = Array.Empty<string>() }); // check empty
        }

        [Fact]
        public void RepeatableAutoOption() {
            TestMainDummy("--repeatable-auto-opt", "56");
            AssertStateChange(new { RepeatableAutoOption = (int[])[ 56 ] });

            TestMainDummy("--repeatable-auto-opt", "56", "--repeatable-auto-opt", "-1");
            AssertStateChange(new { RepeatableAutoOption = (int[])[ 56, -1 ] });

            TestMainDummy();
            AssertStateChange(new { RepeatableAutoOption = Array.Empty<int>() }); // check empty
        }
    }
}