namespace StarKid.Tests.Options;

public static partial class Main {
    [Option("switch")] public static bool SimpleSwitch;
    [Option("switch-prop")] public static bool SwitchProp { get; set; }

    [Option("true-switch")] public static bool TrueSwitch = true;

    internal static bool BinaryToBool(string? s) => s switch { "f" => false, "t" => true, null => true, _ => throw new Exception() };
    [ParseWith(nameof(BinaryToBool))]
    [Option("parsed-switch")] public static bool ParsedSwitch { get; set; }
}

public partial class Tests
{
    public class Switches {
        [Fact]
        public void SimpleSwitchAlone() {
            TestMainDummy("--switch");
            AssertStateChange(new { SimpleSwitch = true });

            TestMainDummy("--switch=true");
            AssertStateChange(new { SimpleSwitch = true });
            TestMainDummy("--switch=false");
            AssertStateChange(new { SimpleSwitch = false });

            TestMainDummy("--switch=1");
            AssertStateChange(new { SimpleSwitch = true });
            TestMainDummy("--switch=0");
            AssertStateChange(new { SimpleSwitch = false });
        }

        [Fact]
        public void PropBackedSwitch() {
            TestMainDummy("--switch-prop");
            AssertStateChange(new { SwitchProp = true });
        }

        [Fact]
        public void InvertedSwitch() {
            TestMainDummy("--true-switch");
            AssertStateChange(new { TrueSwitch = true });

            TestMainDummy("--true-switch=false");
            AssertStateChange(new { TrueSwitch = false });
        }

        [Fact]
        public void ParsedSwitch() {
            TestMainDummy("--parsed-switch");
            AssertStateChange(new { ParsedSwitch = true });

            TestMainDummy("--parsed-switch=t");
            AssertStateChange(new { ParsedSwitch = true });

            TestMainDummy("--parsed-switch=f");
            AssertStateChange(new { ParsedSwitch = false });
        }
    }
}