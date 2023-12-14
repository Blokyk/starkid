using StarKid.Generated;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace StarKid.Tests.OperandParser;

public class Tests
{

    // force call to Utils' cctor by making sure Tests isn't marked .beforefieldinit and lazy-init'd
    static Tests() { }

    static readonly object DefaultState = Utils.DefaultHostState;

    static void AssertStateChange(object changedProps)
        => Assert.Equal(DefaultState.With(changedProps), Utils.GetHostState());

    [Fact]
    public void BasicSum() {
        StarKidProgram.TestMain(new[] { "sum", "1", "1" }, out var stdout, out var stderr);

        Assert.Empty(stderr);
        Assert.Equal("2\n", stdout);
    }

    [Fact]
    public void SumNotEnoughArgs() {
        StarKidProgram.TestMain(new[] { "sum" }, out var stdout, out var stderr);

        Assert.Empty(stdout);
        Assert.Equal("Expected at least 2 arguments, but only got 0\n", stderr);
    }
}