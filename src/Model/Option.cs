namespace Recline.Generator.Model;

public record Option(MinimalTypeInfo Type, OptDesc Desc, ParserInfo Parser, MinimalSymbolInfo BackingSymbol, string? DefaultValueExpr) : IParsable {
    private bool? _isFlag;
    public virtual bool IsFlag {
        get => _isFlag ?? (_isFlag = Type == Utils.BOOLMinInfo).Value;
        init => _isFlag = value;
    }
}