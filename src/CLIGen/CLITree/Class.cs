namespace CLIGen.Generator.Model;

public record Class(string Name, params Member[] members) : Member(Name) {
    public override StringBuilder AppendTo(StringBuilder sb) {
        sb
            .Append(String.Join(" ", Modifiers))
            .Append(" class ")
            .Append(Name)
            .Append('{')
            .AppendLine();

        foreach (var member in members)
            member.AppendTo(sb);

        return sb
                .AppendLine()
                .Append('}');
    }
}