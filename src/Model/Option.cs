namespace Recline.Generator.Model;

public record Option(MinimalTypeInfo Type, OptDesc Desc, string? DefaultValueExpr) {
    public MinimalSymbolInfo BackingSymbol { get; set; } = null!;


    private Lazy<bool> _isSwitch = new Lazy<bool>(() => Type == Utils.BOOLMinInfo);
    public virtual bool IsSwitch { get => _isSwitch.Value; init => _isSwitch = new Lazy<bool>(value); }
}