namespace Recline.Generator.Model;

public record Option(MinimalTypeInfo Type, OptDesc Desc, ParserInfo Parser, MinimalSymbolInfo BackingSymbol, string? DefaultValueExpr) {
    private bool? _isFlag;
    public virtual bool IsFlag {
        get => _isFlag ?? (_isFlag = Type == CommonTypes.BOOLMinInfo).Value;
        init => _isFlag = value;
    }
}