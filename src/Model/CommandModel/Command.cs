using System.Collections.ObjectModel;

namespace Recline.Generator.Model;

[System.Diagnostics.DebuggerDisplay("<{Name,nq}>")]
public sealed record Command : InvokableBase, IEquatable<Command> {
    public Command(string name, Group parentGroup, MinimalMethodInfo backingMethod) : base(name) {
        if (name == "#") {
            IsHiddenCommand = true;
            Name = "";
        } else {
            Name = name;
        }

        ParentGroup = parentGroup;
        BackingMethod = backingMethod;
    }

    public override Group ParentGroup {
        get => base.ParentGroup!;
#pragma warning disable CS8765 // nullability mismatch
        set => base.ParentGroup = value;
#pragma warning restore CS8765
    }

    public bool IsHiddenCommand { get; } = false;

    public MinimalMethodInfo BackingMethod { get; init; }

    public override MinimalLocation Location => BackingMethod.Location;

    private readonly List<Argument> _args = new();
    public ReadOnlyCollection<Argument> Arguments => _args.AsReadOnly();

    public void AddArg(Argument arg) {
        if (arg.IsParams)
            HasParams = true;

        _args.Add(arg);
    }

    public bool HasParams { get; private set; }

    public override int GetHashCode()
        => Utils.CombineHashCodes(
            base.GetHashCode(),
            Utils.CombineHashCodes(
                BackingMethod.GetHashCode(),
                Utils.CombineHashCodes(
                    Utils.SequenceComparer<Argument>.Instance.GetHashCode(_args),
                    Utils.CombineHashCodes(
                        IsHiddenCommand ? 1 : 0,
                        HasParams ? 0 : 1
                    )
                )
            )
        );

    public bool Equals(Command? cmd)
        => cmd is not null && cmd.GetHashCode() == GetHashCode();
}