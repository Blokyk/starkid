namespace CLIGen.Generator.Model;

public record Method(ITypeSymbol Type, string[] Params, string Name, ICLINode Body) : Member(Name) {
    public override StringBuilder AppendTo(StringBuilder sb)
        => sb
            .Append(String.Join(" ", Modifiers))
            .Append(' ')
            .Append(Type.Name)
            .Append(' ')
            .Append(Name)
            .Append('(')
            .Append(String.Join(",", Params))
            .Append(')')
            .AppendLine(Body)
            ;
}