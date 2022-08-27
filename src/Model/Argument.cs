namespace Recline.Generator.Model;

public record Argument(MinimalTypeInfo Type, Desc Desc, ParserInfo Parser, string? DefaultValueExpr) : IParsable {
    public MinimalParameterInfo BackingSymbol { get; set; } = null!;

    public bool IsParams { get; set; }
}