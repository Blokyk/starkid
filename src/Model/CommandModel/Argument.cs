namespace Recline.Generator.Model;

public sealed record Argument(MinimalTypeInfo Type, Desc Desc, ParserInfo Parser, string? DefaultValueExpr) {
    public MinimalParameterInfo BackingSymbol { get; set; } = null!;

    public bool IsParams { get; set; }

    public ValidatorInfo? Validator { get; set; }
}