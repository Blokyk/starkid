using StarKid.Generated;

namespace StarKid.Tests.Options;

public partial class ErrorTests
{
    public class Validator
    {
        [Fact]
        public void ManualItemValidatorWorks() {
            Assert.Equal(1,
                StarKidProgram.TestMain([
                    "--repeat-manual-item-validator-opt", "hu-man",
                    "--repeat-manual-item-validator-opt", "a i",
                    "dummy"
                ], out var stdout, out var stderr)
            );
            Assert.Empty(stdout);
            Assert.StartsWith(
                "Expression 'a i' is not a valid value for option '--repeat-manual-item-validator-opt': " +
                "'NoSpaceInString(repeat-manual-item-validator-opt)' was false",
                stderr
            );
        }

        [Fact]
        public void ArrayValidatorWorks() {
            Assert.Equal(1,
                StarKidProgram.TestMain([
                    "--repeat-item-array-validator-opt", "7",
                    "--repeat-item-array-validator-opt", "0",
                    "--repeat-item-array-validator-opt", "7",
                "dummy"], out var stdout, out var stderr)
            );
            Assert.Empty(stdout);
            Assert.StartsWith(
                "Invalid values for option '--repeat-item-array-validator-opt': " +
                "'NoDuplicates(repeat-item-array-validator-opt)' was false",
                stderr
            );
        }
    }
}