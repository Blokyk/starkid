namespace CLIGen.Generator.Model;

public record MethodOption(OptDesc Desc, bool NeedsAutoHandling) : Option(Utils.VOID, Desc, null) {
    private IMethodSymbol _symbol = null!;
    public new IMethodSymbol BackingSymbol {
        get => _symbol;
        set {
            base.BackingSymbol = _symbol = value;
        }
    }
}