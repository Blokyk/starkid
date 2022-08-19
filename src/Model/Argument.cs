namespace Recline.Generator.Model;

public record Argument(MinimalTypeInfo Type, Desc Desc, string? DefaultValueExpr) {
    public MinimalParameterInfo BackingSymbol { get; set; } = null!;

    public bool IsParams { get; set; }
}