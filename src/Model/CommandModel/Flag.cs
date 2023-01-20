namespace Recline.Generator.Model;

public sealed record Flag : Option, IEquatable<Flag> {
    public Flag(string Name, char Alias, ParserInfo parser, MinimalSymbolInfo backingSymbol, string? defaultValueExpr)
        : base(CommonTypes.BOOLMinInfo, Name, Alias, "", parser, backingSymbol, defaultValueExpr) {}
}