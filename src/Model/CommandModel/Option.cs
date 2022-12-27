namespace Recline.Generator.Model;

public record Option(MinimalTypeInfo Type, OptDesc Desc, ParserInfo Parser, MinimalSymbolInfo BackingSymbol, string? DefaultValueExpr) : IEquatable<Option> {
    private bool? _isFlag;
    public virtual bool IsFlag {
        get => _isFlag ?? (_isFlag = Type == CommonTypes.BOOLMinInfo).Value;
        init => _isFlag = value;
    }

    public ValidatorInfo? Validator { get; set; }

    public override int GetHashCode() => BackingSymbol.GetHashCode();
    public virtual bool Equals(Option? opt) => opt?.GetHashCode() == GetHashCode();
}