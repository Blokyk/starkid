using StarKid.Generated;

namespace StarKid.Tests.NameCasing;

public class Tests {
    [Fact]
    public void Opts() {
        // note: we use Last in case the opt name is in the usage
        string optLine;
        var helpLines = StarKidProgram.MainHelpText.Split('\n');

        var optionsSections
            = helpLines
                .SkipWhile(s => !s.Contains("Options:"))
                .Skip(2) // skip "Options:" line and then -h/--help line
                .TakeWhile(s => !String.IsNullOrWhiteSpace(s))
                .ToArray();

        Assert.Contains("--opt1 <SOME-VAL>", optionsSections[0]);

        optLine = optionsSections.First(s => s.Contains("--opt2"));
        Assert.Contains("--opt2 <URL-MAX-LENGTH>", optionsSections[1]);

        optLine = optionsSections.First(s => s.Contains("--opt3"));
        Assert.Contains("--opt3 <S-SOME-NIGHTMARISH-VAR-NAME>", optionsSections[2]);
    }

    [Fact]
    public void Args() {
        var helpLines = StarKidProgram.DummyHelpText.Split('\n');

        var usage = helpLines[1];
        Assert.Contains("<SOME-VAL>", usage);
        Assert.Contains("<URL-MAX-LENGTH>", usage);
        Assert.Contains("<S-SOME-NIGHTMARISH-VAR-NAME>", usage);

        var argumentsSection
            = helpLines
                .SkipWhile(s => !s.Contains("Arguments:"))
                .Skip(1)
                .TakeWhile(s => !String.IsNullOrWhiteSpace(s))
                .ToArray();

        Assert.Contains("SOME-VAL", argumentsSection[0]);
        Assert.Contains("URL-MAX-LENGTH", argumentsSection[1]);
        Assert.Contains("S-SOME-NIGHTMARISH-VAR-NAME", argumentsSection[2]);
    }
}