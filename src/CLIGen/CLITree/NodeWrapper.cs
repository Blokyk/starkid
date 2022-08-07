namespace CLIGen.Generator.Model;

public record Node<T>(T obj) : ICLINode {
    public StringBuilder AppendTo(StringBuilder sb)
        => sb
            .Append(obj);
}