using StarKid.Generated;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace StarKid.Tests.Options;

public partial class Tests
{
    // force call to Utils' cctor by making sure Tests isn't marked .beforefieldinit and lazy-init'd
    static Tests() {}

    static readonly object DefaultState = Utils.DefaultHostState;

    static void TestMainDummy(params string[] args)
        => StarKidProgram.TestMain([..args, "dummy"]);

    static void AssertNoStateChange()
        => Assert.Equivalent(DefaultState, Utils.GetHostState());

    // we have to use equivalent and not equal because arrays are ref-equal, not value-equal
    static void AssertStateChange(object changedProps)
        => Assert.Equivalent(DefaultState.With(changedProps), Utils.GetHostState());

    [Fact]
    public void NoOption() {
        TestMainDummy();
        Assert.Equivalent(DefaultState, Utils.GetHostState());
    }

    [Fact]
    public void GlobalSwitch() {
        TestMainDummy("--global-switch");
        AssertStateChange(new { GlobalSwitch = true });

        Assert.Equal(0, StarKidProgram.TestMain("dummy", "--global-switch"));
        AssertStateChange(new { GlobalSwitch = true });

        Assert.Equal(0, StarKidProgram.TestMain("dummy2", "--cmd-opt-with-default", "32", "--global-switch"));
        Assert.True(Main.GlobalSwitch);
    }
}