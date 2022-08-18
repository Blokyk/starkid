namespace Recline.Generator.Model;

public record MethodOption(OptDesc Desc, bool NeedsAutoHandling) : Option(Utils.VOID, Desc, null) {
    private MinimalMethodInfo _symbol = null!;
    public new MinimalMethodInfo BackingSymbol {
        get => _symbol;
        set {
            base.BackingSymbol = _symbol = value;
        }
    }

    public override bool IsSwitch { get; init; }
}