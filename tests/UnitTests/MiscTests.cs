namespace Recline.Tests;

public class MiscTests
{
    [Fact]
    public void OnlyGeneratesAttributes() {
        var source = @"
using Recline;

class C1 {}
";

        var comp = Compilation.From(source, CompilationOptions.DefaultLibrary);

        var driver = Driver.Create();
        driver = driver.RunGeneratorsAndUpdateCompilation(comp, out var newComp, out _);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Single(runResult.GeneratedSources.Select(src => src.HintName), "Recline.Generated_Attributes.g.cs");

        Assert.True(newComp.ContainsSymbolsWithName("CommandAttribute", SymbolFilter.Type));
        Assert.True(newComp.ContainsSymbolsWithName("CommandGroupAttribute", SymbolFilter.Type));
        Assert.True(newComp.ContainsSymbolsWithName("OptionAttribute", SymbolFilter.Type));
        Assert.True(newComp.ContainsSymbolsWithName("ParseWithAttribute", SymbolFilter.Type));
        Assert.True(newComp.ContainsSymbolsWithName("ValidateWithAttribute", SymbolFilter.Type));
    }
}