namespace CLIGen.Generator.Model;

public record Argument(ITypeSymbol Type, Desc Desc, ExpressionSyntax? DefaultValue) {
    public IParameterSymbol BackingSymbol { get; set; } = null!;

    public bool IsParams { get; set; }
}