namespace Recline.Generator.Model;

public record Command(bool HasExitCode, string Name, string? Description, ImmutableArray<Option> Options, ImmutableArray<Argument> Args) {
    public WithArgsDesc WithArgsDesc => new WithArgsDesc(
        Name,
        Args.Select(a => a.Desc.Name).ToArray(),
        Description
    );

    public bool InheritOptions { get; init; }

    public Command? ParentCmd { get; set; }
    public string? ParentSymbolName { get; set; }

    public MinimalMethodInfo BackingSymbol { get; set; } = null!;

    public bool IsRoot => BackingSymbol is null;

    public override string ToString() => "<" + Name + ">";

    public bool HasParams { get; set; }

    public string GetNameWithParent() => ParentCmd is null ? Name : ParentCmd.GetNameWithParent() + " " + Name;
}