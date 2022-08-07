namespace CLIGen.Generator.Model;

public record Field(ITypeSymbol Type, string Name, string Expr) : Member(Name) {
    public override StringBuilder AppendTo(StringBuilder sb)
        => sb
            .Append(String.Join(" ", Modifiers))
            .Append(' ')
            .Append(Type.Name)
            .Append(' ')
            .Append(Name)
            .Append('=')
            .Append(Expr)
            .AppendLine(";");
}