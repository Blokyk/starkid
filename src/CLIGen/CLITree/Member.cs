namespace CLIGen.Generator.Model;

public abstract record Member(string Name, params string[] Modifiers) : ICLINode {
    public abstract StringBuilder AppendTo(StringBuilder sb);
}