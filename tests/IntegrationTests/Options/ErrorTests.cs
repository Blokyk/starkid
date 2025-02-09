using StarKid.Generated;

namespace StarKid.Tests.Options;

public partial class ErrorTests
{
    // todo: factor all those strings into methods with parameters

    [Fact]
    public void RepeatedSwitch() {
        Assert.Equal(1, StarKidProgram.TestMain(["--switch", "--switch", "dummy"], out var stdout, out var stderr));
        Assert.Empty(stdout);
        Assert.StartsWith(
            "Option '--switch' has already been specified",
            stderr
        );
    }

    [Fact]
    public void NonExistantRoot() {
        Assert.Equal(1, StarKidProgram.TestMain(["--cogito-ergo-sum", "dummy"], out var stdout, out var stderr));
        Assert.Empty(stdout);
        Assert.StartsWith( // we don't care about help text
            "Command 'test' doesn't have any option named '--cogito-ergo-sum'",
            stderr
        );
    }

    [Fact]
    public void ThrowingOption() { // todo: make a similar one for subcommands just to check the parser has the right help
        Assert.Equal(1, StarKidProgram.TestMain(["--throwing-setter", "hey", "dummy"], out var stdout, out var stderr));
        Assert.Empty(stdout);
        Assert.StartsWith(
            "Expression 'hey' is not a valid value for option '--throwing-setter': Faulty setter!",
            stderr
        );
    }

    [Fact]
    public void NonBoolArgToFlag() {
        Assert.Equal(1, StarKidProgram.TestMain(["dummy2", "--flag-no-arg=maybe"], out var stdout, out var stderr));
        Assert.Empty(stdout);
        Assert.StartsWith(
            "Expression 'maybe' is not a valid value for option '--flag-no-arg': Couldn't understand 'maybe' as a boolean value",
            stderr
        );

        Assert.Equal(1, StarKidProgram.TestMain(["dummy2", "-f=maybe"], out stdout, out stderr));
        Assert.Empty(stdout);
        Assert.StartsWith(
            "Expression 'maybe' is not a valid value for option '-f': Couldn't understand 'maybe' as a boolean value",
            stderr
        );
    }

    [Fact]
    public void MissingArg() {
        Assert.Equal(1, StarKidProgram.TestMain(["dummy2", "--missing-arg"], out var stdout, out var stderr));
        Assert.Empty(stdout);
        Assert.StartsWith(
            "Option '--missing-arg' needs an argument",
            stderr
        );

        Assert.Equal(1, StarKidProgram.TestMain(["dummy2", "-m"], out stdout, out stderr));
        Assert.Empty(stdout);
        Assert.StartsWith(
            "Option '--missing-arg' needs an argument",
            stderr
        );
    }
}