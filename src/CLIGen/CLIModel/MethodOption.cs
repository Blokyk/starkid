namespace CLIGen.Generator.Model;

public record MethodOption(OptDesc Desc, bool NeedsAutoHandling) : Option(Utils.VOID, Desc, null) {
    public new IMethodSymbol BackingSymbol { get; set; } = null!;
}