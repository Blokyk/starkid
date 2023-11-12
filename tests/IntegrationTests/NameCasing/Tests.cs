using StarKid.Generated;

namespace StarKid.Tests.NameCasing;

public class Tests {
    // todo: rewrite a few tests to use [Theory]/[InlineData] where possible

    static readonly string[] _mainHelpLines = StarKidProgram.MainHelpText.Split('\n');
    static readonly string[] _mainOptionsSection
        = _mainHelpLines
                .SkipWhile(s => !s.Contains("Options:"))
                .Skip(2) // skip "Options:" line and then -h/--help line
                .TakeWhile(s => !String.IsNullOrWhiteSpace(s))
                .ToArray();

    [Theory]
    [InlineData(0, "SOME-VAL")]
    [InlineData(1, "URL-MAX-LENGTH")]
    [InlineData(2, "S-SOME-NIGHTMARISH-VAR-NAME")]
    public void Opts(int i, string expected) {
        Assert.Contains($"--opt{i+1} <{expected}>", _mainOptionsSection[i]);
    }


    public static readonly string[] _dummyHelpLines = StarKidProgram.DummyHelpText.Split('\n');
    public static readonly string[] _dummyArgsSection
        = _dummyHelpLines
                .SkipWhile(s => !s.Contains("Arguments:"))
                .Skip(1)
                .TakeWhile(s => !String.IsNullOrWhiteSpace(s))
                .ToArray();

    [Theory]
    [InlineData(0, "SOME-VAL")]
    [InlineData(1, "URL-MAX-LENGTH")]
    [InlineData(2, "S-SOME-NIGHTMARISH-VAR-NAME")]
    public void Args(int i, string expected) {
        var usage = _dummyHelpLines[1];
        Assert.Contains($"<{expected}>", usage);

        Assert.Contains(expected, _dummyArgsSection[i]);
    }
}