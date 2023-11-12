using StarKid.Generator;

namespace StarKid.Tests;

public class OptionDiags
{
    [Fact]
    public void CLI299_IsGlobalOnNonGroupOpt() {
        var source = """
        using StarKid;

        [CommandGroup("test")]
        public static class Main {
            [Command("dummy")]
            public static void Dummy(
                [Option("opt", IsGlobal = true)] [|string _|]
            ) {}
        }
        """;

        var tree = SyntaxTree.WithMarkedNode(source, out var paramNode);

        var comp = Compilation.From(tree);
        var genResult = comp.RunStarKid();

        Assert.Single(
            genResult.Diagnostics,
            d => d.Descriptor == Diagnostics.IsGlobalOnNonGroupOpt
              && paramNode.GetLocation().Contains(d.Location)
        );
    }
}