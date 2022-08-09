namespace CLIGen.Generator.Model;

public record Option(ITypeSymbol Type, OptDesc Desc, ExpressionSyntax? DefaultValue) {
    public ISymbol BackingSymbol { get; set; } = null!;

    public bool IsSwitch => Utils.Equals(Type, Utils.BOOL);
}