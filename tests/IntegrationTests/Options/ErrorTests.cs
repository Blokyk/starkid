using Recline.Generated;
using System.Diagnostics;
using Xunit.Sdk;

namespace Recline.Tests.Options;


public class ErrorTests
{
    // todo: factor all those strings into methods with parameters

    [Fact]
    public void RepeatedSwitch() {
        Assert.Equal(1, ReclineProgram.TestMain(new[] { "--switch", "--switch", "dummy" }, out var stdout, out var stderr));
        Assert.Empty(stdout);
        Assert.StartsWith(
            "Option '--switch' has already been specified\n",
            stderr
        );
    }

    [Fact]
    public void NonExistantRoot() {
        Assert.Equal(1, ReclineProgram.TestMain(new[] { "--cogito-ergo-sum", "dummy" }, out var stdout, out var stderr));
        Assert.Empty(stdout);
        Assert.StartsWith( // we don't care about help text
            "Command 'test' doesn't have any option named '--cogito-ergo-sum'\n",
            stderr
        );
    }

    [Fact]
    public void DirectParserFail() {
        Assert.Equal(1, ReclineProgram.TestMain(new[] { "--parsed-switch=foo", "dummy" }, out var stdout, out var stderr));
        Assert.Empty(stdout);
        Assert.Equal(
            "Expression 'foo' is not a valid value for option '--parsed-switch': Couldn't parse 'foo' as an argument of type 'bool'\n",
            stderr
        );
    }

    // todo: check that enum opts don't accept numbers

    [Fact]
    public void ThrowingOption() { // todo: make a similar one for subcommands just to check the parser has the right help
        Assert.Equal(1, ReclineProgram.TestMain(new[] { "--throwing-setter", "hey", "dummy" }, out var stdout, out var stderr));
        Assert.Empty(stdout);
        Assert.StartsWith(
            "Expression 'hey' is not a valid value for option '--throwing-setter': Faulty setter!\n",
            stderr
        );
    }
}