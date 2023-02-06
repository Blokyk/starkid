using System.Collections.ObjectModel;

namespace Recline.Generator.Model;

public sealed record Group(
    string Name,
    string FullClassName,
    string? ParentClassFullName,
    MinimalTypeInfo BackingClass
) : InvokableBase(Name), IEquatable<Group> {
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

    // todo: validate default command
    public void SetDefaultCommand(Command cmd)
        => DefaultCommand = cmd;

    internal static readonly IEqualityComparer<Group> FastIDComparer
        = Utils.CreateComparerFrom<Group>(
            (g1, g2) => g1?.ID.GetHashCode() == g2?.ID.GetHashCode(),
            g => g.ID.GetHashCode()
        );

    public override int GetHashCode()
        => Utils.CombineHashCodes(
            base.GetHashCode(),
            Utils.CombineHashCodes(
                Utils.SequenceComparer<Command>.Instance.GetHashCode(_cmds),
                Utils.CombineHashCodes(
                    Utils.SequenceComparer<Group>.Instance.GetHashCode(_subgroups),
                    Utils.CombineHashCodes(
                        FullClassName.GetHashCode(),
                        Utils.CombineHashCodes(
                            ParentClassFullName?.GetHashCode() ?? 0,
                            Utils.CombineHashCodes(
                                BackingClass.GetHashCode(),
                                DefaultCommand?.GetHashCode() ?? 0
                            )
                        )
                    )
                )
            )
        );

    public bool Equals(Group? group)
        => group is not null && group.GetHashCode() == GetHashCode();

    public override string ToString() => ID;
}