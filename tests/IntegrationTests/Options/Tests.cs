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

    static void AssertStateChange(object changedProps)
        => Assert.Equal(DefaultState.With(changedProps), Utils.GetHostState());

    [Fact]
    public void NoOption() {
        TestMainDummy();
        Assert.Equal(DefaultState, Utils.GetHostState());
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