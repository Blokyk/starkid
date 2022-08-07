namespace CLIGen.Generator.Model;

public record Property(ITypeSymbol Type, string Name, string Expr) : Field(Type, Name, Expr) {
    public override StringBuilder AppendTo(StringBuilder sb)
        => sb
            .Append(String.Join(" ", Modifiers))
            .Append(' ')
            .Append(Type.Name)
            .Append(' ')
            .Append(Name)
            .Append(" => ")
            .Append(Expr)
            .AppendLine(";");
}