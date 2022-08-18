namespace Recline.Generator.Model;

public record Argument(ITypeSymbol Type, Desc Desc, ExpressionSyntax? DefaultValue) {
    public MinimalParameterInfo BackingSymbol { get; set; } = null!;

    public bool IsParams { get; set; }
}