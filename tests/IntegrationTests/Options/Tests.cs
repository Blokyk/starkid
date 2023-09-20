using StarKid.Generated;
using System.Net;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace StarKid.Tests.Options;

public class Tests
{
    // force call to Utils' cctor by making sure Tests isn't marked .beforefieldinit and lazy-init'd
    static Tests() {}

    static readonly object DefaultState = Utils.DefaultHostState;

    static void TestMainDummy(params string[] args)
        => StarKidProgram.TestMain(args.Append("dummy").ToArray());

    static void AssertStateChange(object changedProps)
        => Assert.Equal(DefaultState.With(changedProps), Utils.GetHostState());

    [Fact]
    public void NoOption() {
        TestMainDummy();
        Assert.Equal(DefaultState, Utils.GetHostState());
    }

    [Fact]
    public void SimpleSwitchAlone() {
        TestMainDummy("--switch");
        AssertStateChange(new { SimpleSwitch = true });

        TestMainDummy("--switch=false");
        AssertStateChange(new { SimpleSwitch = false }); // could be just DefaultState but it's clearer that way
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

        TestMainDummy(new[] { "--true-switch=false" });
        AssertStateChange(new { TrueSwitch = false });
    }

    [Fact]
    public void ParsedSwitch() {
        TestMainDummy("--parsed-switch");
        AssertStateChange(new { ParsedSwitch = true });

        TestMainDummy("--parsed-switch=1");
        AssertStateChange(new { ParsedSwitch = true });

        TestMainDummy("--parsed-switch=0");
        AssertStateChange(new { ParsedSwitch = false }); // technically could be empty but it's clearer that way
    }

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
        TestMainDummy("--auto-lib-opt", "1.1.1.1");
        AssertStateChange(new { AutoLibOption = IPAddress.Parse("1.1.1.1") });

        // check that ':' doesn't interfere in the opt/arg separation
        TestMainDummy("--auto-lib-opt=2001:0db8:85a3:0000:0000:8a2e:0370:7334");
        AssertStateChange(new { AutoLibOption = IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334") });
    }

    [Fact]
    public void EnumOption() {
        TestMainDummy("--enum-opt", "CreateNew");
        AssertStateChange(new { EnumOption = FileMode.CreateNew });
    }

    [Fact]
    public void FooOption() {
        TestMainDummy("--auto-user-opt", "ascii");
        AssertStateChange(new { FooOption = AsciiString.From("ascii") });
    }

    [Fact]
    public void ManualLibOption() {
        TestMainDummy("--manual-mscorlib-opt", "foo:bar");
        AssertStateChange(new { ManualLibOption = KeyValuePair.Create("foo", "bar") });

        TestMainDummy("--manual-mscorlib-opt=:bar"); // check that ':' doesn't fuck with things
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
    public void NullableStructOption() {
        TestMainDummy("--nullable-struct-opt", "123456789");
        AssertStateChange(new { NullableStructOption = (Int128?)123456789 });
    }

    [Fact]
    public void GlobalSwitch() {
        TestMainDummy("--global-switch");
        AssertStateChange(new { GlobalSwitch = true });

        Assert.Equal(0, StarKidProgram.TestMain("dummy", "--global-switch"));
        AssertStateChange(new { GlobalSwitch = true });

        Assert.Equal(0, StarKidProgram.TestMain("dummy2", "--cmd-opt-with-default", "32", "--global-switch"));
        Assert.True(OptionTest.GlobalSwitch);
    }

    [Fact]
    public void WithTest() {
        var a = new { Foo = "hello", Bar = 6 };
        var b = a.With(new{ Foo = "hi" });
        Assert.Equal(new { Foo = "hi", Bar = 6 }, b);
        Assert.Equal(new { Foo = "hello", Bar = 6 }, a);
    }
}