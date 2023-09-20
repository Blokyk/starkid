namespace StarKid.Tests;

public class MiscTests
{
    [Fact]
    public void OnlyGeneratesAttributes() {
        var source = @"
using StarKid;

class C1 {}
";

        var comp = Compilation.From(source, CompilationOptions.DefaultLibrary);

        var driver = Driver.Create();
        driver = driver.RunGeneratorsAndUpdateCompilation(comp, out var newComp, out _);
        var runResult = driver.GetRunResult().Results[0];

        Assert.True(newComp.ContainsSymbolsWithName("CommandAttribute", SymbolFilter.Type));
        Assert.True(newComp.ContainsSymbolsWithName("CommandGroupAttribute", SymbolFilter.Type));
        Assert.True(newComp.ContainsSymbolsWithName("OptionAttribute", SymbolFilter.Type));
        Assert.True(newComp.ContainsSymbolsWithName("ParseWithAttribute", SymbolFilter.Type));
        Assert.True(newComp.ContainsSymbolsWithName("ValidateWithAttribute", SymbolFilter.Type));
    }
}