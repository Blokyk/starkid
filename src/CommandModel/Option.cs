using StarKid.Generator.SymbolModel;

namespace StarKid.Generator.CommandModel;

public record Option(MinimalTypeInfo Type, string Name, char Alias, bool IsGlobal, ParserInfo Parser, MinimalSymbolInfo BackingSymbol, string? DefaultValueExpr) {
    public string? Description { get; set; }
    public string? CustomArgName { get; set; }

    public bool IsRepeatableOption()
        => Type is MinimalArrayTypeInfo { ElementType: var itemType }
        && itemType == Parser.TargetType;

    public MinimalLocation GetLocation() => BackingSymbol.Location;

    public ImmutableValueArray<ValidatorInfo> Validators { get; set; }
}