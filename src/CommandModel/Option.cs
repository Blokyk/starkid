using StarKid.Generator.SymbolModel;

namespace StarKid.Generator.CommandModel;

public record Option(MinimalTypeInfo Type, string Name, char Alias, string ArgName, bool IsGlobal, ParserInfo Parser, MinimalSymbolInfo BackingSymbol, string? DefaultValueExpr) {
    public string? Description { get; set; }

    public MinimalLocation GetLocation() => BackingSymbol.Location;

    public ImmutableValueArray<ValidatorInfo> Validators { get; set; }
}