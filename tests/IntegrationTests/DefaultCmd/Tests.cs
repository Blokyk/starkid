using StarKid.Generated;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace StarKid.Tests.DefaultCmd;

public class Tests
{
    [Fact]
    public void HiddenIsInvokedWithoutName() {
        StarKidProgram.TestMain(["with-hidden", "thing", "stuff"], out var stdout, out var stderr);

        Assert.Empty(stderr);
        Assert.Equal("thing, stuff\n", stdout);
    }

    [Fact]
    /// <summary>
    /// Hidden commands should be completely invisible -- that includes
    /// not treating their 'name' as a command name but simply as an argument
    /// </summary>
    public void HiddenIsNotInvokable() {
        StarKidProgram.TestMain(["with-hidden", "#", "stuff"], out var stdout, out var stderr);

        Assert.Empty(stderr);
        Assert.Equal("#, stuff\n", stdout);
    }

    [Fact]
    public void CanSetGroupOptionWithHidden() {
        StarKidProgram.TestMain(["with-hidden", "hello", "--some", "world"], out var stdout, out var stderr);

        Assert.Empty(stderr);
        Assert.Equal("hello, world\n", stdout);
        Assert.True(App.WithHidden.SomeFlag);
    }

    [Fact]
    public void DefaultIsInvokedWithoutName() {
        StarKidProgram.TestMain(["with-visible", "45"], out var stdout, out var stderr);

        Assert.Empty(stderr);
        Assert.Equal("bar(False): 45\n", stdout);
    }

    [Fact]
    public void DefaultIsInvokedWithName() {
        StarKidProgram.TestMain(["with-visible", "bar", "45"], out var stdout, out var stderr);

        Assert.Empty(stderr);
        Assert.Equal("bar(False): 45\n", stdout);
    }

    [Fact]
    public void NonDefaultIsInvokable() {
        StarKidProgram.TestMain(["with-visible", "foo", "truc"], out var stdout, out var stderr);

        Assert.Empty(stderr);
        Assert.Equal("foo: truc\n", stdout);
    }

    [Fact]
    public void OptionIsAccessibleWhenUsingDefaultCmdExplicitly() {
        StarKidProgram.TestMain(["with-visible", "bar", "--flag", "45"], out var stdout, out var stderr);

        Assert.Empty(stderr);
        Assert.Equal("bar(True): 45\n", stdout);
    }
}