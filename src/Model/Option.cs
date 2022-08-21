namespace Recline.Generator.Model;

public record Option(MinimalTypeInfo Type, OptDesc Desc, string? DefaultValueExpr) {
    public MinimalSymbolInfo BackingSymbol { get; set; } = null!;


    private Lazy<bool> _isFlag = new Lazy<bool>(() => Type == Utils.BOOLMinInfo);
    public virtual bool IsFlag { get => _isFlag.Value; init => _isFlag = new Lazy<bool>(value); }
}