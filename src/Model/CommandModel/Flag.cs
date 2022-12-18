namespace Recline.Generator.Model;

public record Flag : Option {
    public Flag(FlagDesc desc, ParserInfo parser, MinimalSymbolInfo backingSymbol, string? defaultValueExpr)
        : base(CommonTypes.BOOLMinInfo, desc, parser, backingSymbol, defaultValueExpr) {}
}