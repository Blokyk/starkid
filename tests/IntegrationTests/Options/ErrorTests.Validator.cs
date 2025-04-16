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

        [Fact]
        public void ValidatorWithCustomMessage() {
            Assert.Equal(1,
                StarKidProgram.TestMain([
                    "--validator-with-message-opt", "-5",
                "dummy"], out var stdout, out var stderr)
            );
            Assert.Empty(stdout);
            Assert.StartsWith(
                "Expression '-5' is not a valid value for option '--validator-with-message-opt': " +
                "Number must be positive",
                stderr
            );
        }

        [Fact]
        public void ValidatorProperty() {
            Assert.Equal(1,
                StarKidProgram.TestMain([
                    "--validator-prop-opt", "boat",
                "dummy"], out var stdout, out var stderr)
            );
            Assert.Empty(stdout);
            Assert.StartsWith(
                "Expression 'boat' is not a valid value for option '--validator-prop-opt': " +
                "'validator-prop-opt.HasWheels is true' was false", // fixme: ewwwwwwwwwwwwwwww
                stderr
            );
        }

        [Fact]
        public void FalseValidatorProperty() {
            Assert.Equal(1,
                StarKidProgram.TestMain([
                    "--false-validator-prop-opt", "",
                "dummy"], out var stdout, out var stderr)
            );
            Assert.Empty(stdout);
            Assert.StartsWith(
                "Expression '' is not a valid value for option '--false-validator-prop-opt': " +
                "'false-validator-prop-opt.IsEmpty is false' was false", // fixme: ewwwwwwwwwwwwwwww
                stderr
            );
        }

        [Fact]
        public void ValidatorInheritedProperty() {
            Assert.Equal(1,
                StarKidProgram.TestMain([
                    "--validator-inherited-prop-opt", "sleigh",
                "dummy"], out var stdout, out var stderr)
            );
            Assert.Empty(stdout);
            Assert.StartsWith(
                "Expression 'sleigh' is not a valid value for option '--validator-inherited-prop-opt': " +
                "'validator-inherited-prop-opt.HasWheels is true' was false", // fixme: ewwwwwwwwwwwwwwww
                stderr
            );
        }
    }
}