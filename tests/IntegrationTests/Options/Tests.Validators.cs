using System.Numerics;

namespace StarKid.Tests.Options;

public static partial class Main {
    // todo: validators (w/ nullability variance, like Int32.IsPositive with a int?)

#pragma warning disable CS8618 // see #41
    public static bool NoSpaceInString(string s) => !s.Contains(' ');
    [ParseWith(nameof(StringToUpper))]
    [ValidateWith(nameof(NoSpaceInString))]
    [Option("repeat-manual-item-validator-opt")] public static string[] RepeatManualItemValidatorOption { get; set; }

    public static bool NoDuplicates(IEnumerable<int> ints) => ints.Count() == ints.Distinct().Count();
    [ValidateWith(nameof(NoDuplicates))]
    [Option("repeat-item-array-validator-opt")] public static int[] RepeatItemArrayValidatorOption { get; set; }
#pragma warning restore

    [ValidateWith(nameof(Int32.IsPositive), "Number must be positive")]
    [Option("validator-with-message-opt")] public static int ValidatorWithMessageOption { get; set; }

    public static bool NotZero<T>(T t) where T : INumber<T> => !T.IsZero(t);
    [ValidateWith(nameof(NotZero))]
    [Option("generic-validator-opt")] public static int GenericValidatorOption { get; set; }

    [ParseWith(nameof(ParseNumber))]
    [ValidateWith(nameof(NotZero))]
    [Option("generic-validator-with-generic-parser-opt")] public static int GenericValidatorWithGenericParserOption { get; set; }
}

public partial class Tests {
    public class Validator {
        [Fact]
        public void RepeatManualItemValidatorOption() {
            TestMainDummy("--repeat-manual-item-validator-opt", "hu-man", "--repeat-manual-item-validator-opt", "a-i");
            AssertStateChange(new { RepeatManualItemValidatorOption = (string[])["HU-MAN", "A-I"] });
        }

        [Fact]
        public void RepeatItemArrayValidatorOption() {
            TestMainDummy("--repeat-item-array-validator-opt", "16", "--repeat-item-array-validator-opt", "10");
            AssertStateChange(new { RepeatItemArrayValidatorOption = (int[])[16, 10] });
        }

        [Fact]
        public void GenericValidatorOption() {
            TestMainDummy("--generic-validator-opt", "24");
            AssertStateChange(new { GenericValidatorOption = 24 });
        }

        [Fact]
        public void GenericValidatorWithGenericParserOption() {
            TestMainDummy("--generic-validator-with-generic-parser-opt", "24");
            AssertStateChange(new { GenericValidatorWithGenericParserOption = 24 });
        }
    }
}