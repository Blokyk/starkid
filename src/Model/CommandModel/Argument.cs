namespace StarKid.Generator.Model;

public sealed record Argument(ParserInfo Parser, MinimalParameterInfo BackingSymbol, string? DefaultValueExpr) {
    public string? Description { get; set; }

    public MinimalTypeInfo Type => BackingSymbol.Type;
    public string Name => BackingSymbol.Name;

    public MinimalLocation GetLocation() => BackingSymbol.Location;

    public bool IsParams => BackingSymbol.IsParams;

    public ImmutableValueArray<ValidatorInfo> Validators { get; set; }
}