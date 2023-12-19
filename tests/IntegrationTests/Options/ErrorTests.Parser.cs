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
    }
}