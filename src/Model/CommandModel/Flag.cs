namespace Recline.Generator.Model;

public sealed record Flag : Option, IEquatable<Flag> {
    public Flag(FlagDesc desc, ParserInfo parser, MinimalSymbolInfo backingSymbol, string? defaultValueExpr)
        : base(CommonTypes.BOOLMinInfo, desc, parser, backingSymbol, defaultValueExpr) {}
}