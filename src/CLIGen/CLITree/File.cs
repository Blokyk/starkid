namespace CLIGen.Generator.Model;

public record File(string?[] Usings, string? Namespace, Class MainClass) : ICLINode {
    public StringBuilder AppendTo(StringBuilder sb) {
        foreach (var @using in Usings.Where(u => u is not null)) {
            sb.AppendLine("using " + @using + ";");
        }

        if (Namespace is not null) {
            sb
                .Append("namespace ")
                .Append(Namespace)
                .AppendLine(";");
        }

        return MainClass.AppendTo(sb);
    }
}