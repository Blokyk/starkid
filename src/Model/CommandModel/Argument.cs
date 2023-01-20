namespace Recline.Generator.Model;

public sealed record Argument(MinimalTypeInfo Type, string Name, ParserInfo Parser, MinimalParameterInfo BackingSymbol, string? DefaultValueExpr) {
    public string? Description { get; set; }

    public MinimalLocation GetLocation() => BackingSymbol.Location;

    public bool IsParams => BackingSymbol.IsParams;

    public ValidatorInfo? Validator { get; set; }
}