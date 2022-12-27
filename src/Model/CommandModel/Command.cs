namespace Recline.Generator.Model;

[System.Diagnostics.DebuggerDisplay("<{Name,nq}>")]
public sealed record Command(bool HasExitCode, string Name, string? Description, ImmutableArray<Option> Options, ImmutableArray<Argument> Args) : IEquatable<Command> {
    public WithArgsDesc GetDesc() => new(
        Name,
        Args.Select(a => a.Desc.Name).ToArray(),
        Description
    );

    public bool InheritOptions { get; init; }

    private string? _fullName;
    public string FullName => _fullName ??= ParentCmd?.FullName + "_" + Name;

    public Command? ParentCmd { get; set; }
    public string? ParentCmdMethodName { get; set; }

    [MemberNotNullWhen(false, nameof(ParentCmd))]
    public bool IsTopLevel => ParentCmd is null;

    public bool IsRoot { get; init; }

    public MinimalMethodInfo BackingSymbol { get; set; } = null!;

    public bool HasParams { get; set; }

    public override int GetHashCode() => FullName.GetHashCode();
    public bool Equals(Command? cmd) => cmd?.GetHashCode() == GetHashCode();
}