namespace CLIGen.Generator.Model;

public record Argument(ITypeSymbol Type, string Name, string? Description, ExpressionSyntax? DefaultValue);