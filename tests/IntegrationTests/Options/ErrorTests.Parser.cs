using StarKid.Generated;

namespace StarKid.Tests.Options;

public partial class ErrorTests
{
    public class Parser
    {
        [Fact]
        public void DirectParserFail() {
            Assert.Equal(1, StarKidProgram.TestMain(["--parsed-switch=foo", "dummy"], out var stdout, out var stderr));
            Assert.Empty(stdout);
            Assert.StartsWith(
                "Expression 'foo' is not a valid value for option '--parsed-switch': " +
                "Couldn't parse 'foo' as an argument of type 'bool'",
                stderr
            );
        }

        [Fact]
        public void DontRepeatManualArrayOpt() {
            Assert.Equal(1, StarKidProgram.TestMain(["--manual-array-opt", "hey", "--manual-array-opt", "hi", "dummy"], out var stdout, out var stderr));
            Assert.Empty(stdout);
            Assert.StartsWith(
                "Option '--manual-array-opt' has already been specified",
                stderr
            );
        }

        // todo: check that enum opts don't accept numbers
    }
}