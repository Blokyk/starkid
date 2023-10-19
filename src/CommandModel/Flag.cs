using StarKid.Generator.SymbolModel;

namespace StarKid.Generator.CommandModel;

public sealed record Flag : Option, IEquatable<Flag> {
    public Flag(string Name, char Alias, bool IsGlobal, ParserInfo parser, MinimalSymbolInfo backingSymbol, string? defaultValueExpr)
        : base(CommonTypes.BOOL, Name, Alias, "", IsGlobal, parser, backingSymbol, defaultValueExpr) {}
}