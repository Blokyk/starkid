using System.Collections.ObjectModel;

using StarKid.Generator.SymbolModel;

namespace StarKid.Generator.CommandModel;

public sealed record Group(
    string Name,
    string FullClassName,
    string? ParentClassFullName,
    MinimalTypeInfo BackingClass
) : InvokableBase(Name, BackingClass.Name), IEquatable<Group> {
    public override MinimalLocation Location => BackingClass.Location;

    private readonly List<Command> _cmds = new();
    public ReadOnlyCollection<Command> Commands => _cmds.AsReadOnly();

    private readonly List<Group> _subgroups = new();
    public ReadOnlyCollection<Group> SubGroups => _subgroups.AsReadOnly();

    public void AddCommand(Command cmd)
        => _cmds.Add(cmd);

    public void AddSubgroup(Group group) {
        group.ParentGroup = this;
        _subgroups.Add(group);
    }

    public Command? DefaultCommand { get; private set; }

    public void SetDefaultCommand(Command cmd)
        => DefaultCommand = cmd;

    internal static readonly IEqualityComparer<Group> FastIDComparer
        = MiscUtils.CreateComparerFrom<Group>(
            (g1, g2) => g1?.ID.GetHashCode() == g2?.ID.GetHashCode(),
            g => g.ID.GetHashCode()
        );

    public override int GetHashCode()
        => MiscUtils.CombineHashCodes(
            base.GetHashCode(),
            MiscUtils.CombineHashCodes(
                SequenceComparer<Command>.Instance.GetHashCode(_cmds),
                MiscUtils.CombineHashCodes(
                    SequenceComparer<Group>.Instance.GetHashCode(_subgroups),
                    MiscUtils.CombineHashCodes(
                        FullClassName.GetHashCode(),
                        MiscUtils.CombineHashCodes(
                            ParentClassFullName?.GetHashCode() ?? 0,
                            MiscUtils.CombineHashCodes(
                                BackingClass.GetHashCode(),
                                DefaultCommand?.GetHashCode() ?? 0
                            )
                        )
                    )
                )
            )
        );

    public bool Equals(Group? group)
        => (object)this == group || (group is not null && group.GetHashCode() == GetHashCode());

    public override string ToString() => ID;
}