using System.Collections.ObjectModel;

using StarKid.Generator.SymbolModel;

namespace StarKid.Generator.CommandModel;

[System.Diagnostics.DebuggerDisplay("<{Name,nq}>")]
public sealed record Command : InvokableBase, IEquatable<Command> {
    public Command(string name, Group parentGroup, MinimalMethodInfo backingMethod) : base(name, backingMethod.Name) {
        if (name == "#") {
            IsHiddenCommand = true;
            Name = parentGroup.Name;
        } else {
            Name = name;
        }

        ParentGroup = parentGroup;
        BackingMethod = backingMethod;
    }

#pragma warning disable CS8765 // nullability mismatch
    public override Group ParentGroup {
        get => base.ParentGroup!;
        set => base.ParentGroup = value;
    }
#pragma warning restore CS8765

    public bool IsHiddenCommand { get; } = false;

    public MinimalMethodInfo BackingMethod { get; init; }

    public override MinimalLocation Location => BackingMethod.Location;

    private readonly List<Argument> _args = new();
    public ReadOnlyCollection<Argument> Arguments => _args.AsReadOnly();

    public void AddArg(Argument arg) {
        if (arg.IsParams)
            ParamsArg = arg;
        else
            _args.Add(arg);
    }

    public Argument? ParamsArg { get; private set; }

    [MemberNotNullWhen(true, nameof(ParamsArg))]
    public bool HasParams => ParamsArg is not null;

    public override int GetHashCode()
        => Polyfills.CombineHashCodes(
            base.GetHashCode(),
            Polyfills.CombineHashCodes(
                BackingMethod.GetHashCode(),
                Polyfills.CombineHashCodes(
                    SequenceComparer<Argument>.Instance.GetHashCode(_args),
                    Polyfills.CombineHashCodes(
                        ParamsArg?.GetHashCode() ?? 0,
                        IsHiddenCommand ? 1 : 0
                    )
                )
            )
        );

    public bool Equals(Command? cmd)
        => (object)this == cmd || (cmd is not null && cmd.GetHashCode() == GetHashCode());
}