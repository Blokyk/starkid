using System.Collections.ObjectModel;

namespace StarKid.Generator.Model;

[System.Diagnostics.DebuggerDisplay("{ID,nq}")]
public abstract record InvokableBase : IEquatable<InvokableBase> {
    public string Name { get; init; }
    public DescriptionInfo? Description { get; set; }
    public abstract MinimalLocation Location { get; }

    protected InvokableBase(string name, string symbolName) {
        Name = name;
        _symbolName = symbolName;
    }

    private readonly string _symbolName;
    protected string? _id;
    public string ID
        => _id
            ??= ParentGroup is null
              ? _symbolName
              : ParentGroup.ID + "_" + _symbolName;

    private Group? _parent;
    public virtual Group? ParentGroup {
        get => _parent;
        set {
            _parent = value;
            _id = null;
        }
    }

    private readonly List<Option> _options = new();
    public ReadOnlyCollection<Option> Options => _options.AsReadOnly();

    private readonly List<Flag> _flags = new();
    public ReadOnlyCollection<Flag> Flags => _flags.AsReadOnly();

    public IEnumerable<Option> OptionsAndFlags => _options.Concat(_flags);

    public void AddOption(Option opt) {
        if (opt is Flag flag)
            AddFlag(flag);
        else
            _options.Add(opt);
    }

    public void AddFlag(Flag flag)
        => _flags.Add(flag);

    public override int GetHashCode()
        // we don't compare parents here because otherwise it'd just be recursion
        => Utils.CombineHashCodes(
            Description?.GetHashCode() ?? 0,
            Utils.CombineHashCodes(
                Utils.SequenceComparer<Option>.Instance.GetHashCode(_options),
                Utils.SequenceComparer<Flag>.Instance.GetHashCode(_flags)
            )
        );

    public virtual bool Equals(InvokableBase? other)
        => (object)this == other || (other is not null && other.GetHashCode() == GetHashCode());

    public override string ToString() => ID;
}