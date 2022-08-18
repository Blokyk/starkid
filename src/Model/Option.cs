namespace Recline.Generator.Model;

public record Option(ITypeSymbol Type, OptDesc Desc, ExpressionSyntax? DefaultValue) {
    public MinimalSymbolInfo BackingSymbol { get; set; } = null!;


    private Lazy<bool> _isSwitch = new Lazy<bool>(() => Utils.Equals(Type, Utils.BOOL));
    public virtual bool IsSwitch { get => _isSwitch.Value; init => _isSwitch = new Lazy<bool>(value); }
}