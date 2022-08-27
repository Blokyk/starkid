namespace Recline.Generator.Model;

public record MethodOption : Option {
    public bool NeedsAutoHandling { get; init; }

    //public override bool IsFlag { get; init; }

    private MinimalMethodInfo _symbol;
    public new MinimalMethodInfo BackingSymbol {
        get => _symbol;
        init {
            base.BackingSymbol = _symbol = value;
        }
    }

    public MethodOption(
        OptDesc desc,
        ParserInfo parser,
        MinimalMethodInfo backingSymbol,
        bool needsAutoHandling
    ) : base(Utils.VOIDMinInfo, desc, parser, backingSymbol, null) {
        _symbol = backingSymbol;
        NeedsAutoHandling = needsAutoHandling;
    }
}