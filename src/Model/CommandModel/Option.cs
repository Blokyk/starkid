namespace Recline.Generator.Model;

public record Option(MinimalTypeInfo Type, string Name, char Alias, string ArgName, ParserInfo Parser, MinimalSymbolInfo BackingSymbol, string? DefaultValueExpr) {
    public string? Description { get; set; }

    public MinimalLocation GetLocation() => BackingSymbol.Location;

    public ValidatorInfo? Validator { get; set; }
}