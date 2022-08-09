namespace CLIGen.Generator.Model;

public record Command(bool HasExitCode, string Name, string? Description, Option[] Options, Argument[] Args) {
    private Lazy<WithArgsDesc> _asArgsDesc = new Lazy<WithArgsDesc>(
        () => new WithArgsDesc(
                Name,
                Args.Select(a => a.Desc.Name).ToArray(),
                Description
            ),
        false
    );

    public WithArgsDesc WithArgsDesc => _asArgsDesc.Value;

    public bool InheritOptions { get; init; }

    public Command? ParentCmd { get; set; }
    public string? ParentCmdName { get; set; }

    public IMethodSymbol BackingSymbol { get; set; } = null!;

    public bool IsRoot => BackingSymbol is null;

    public override string ToString() => "<nah>";
}