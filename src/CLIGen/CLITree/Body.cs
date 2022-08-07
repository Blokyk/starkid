namespace CLIGen.Generator.Model;

public record Body(params string[] Statements) : ICLINode {
    public StringBuilder AppendTo(StringBuilder sb) {
        sb.AppendLine("{");

        foreach (var stmt in Statements)
            sb.Append(stmt).Append(';');

        return sb.AppendLine("}");
    }
}