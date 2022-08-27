namespace Recline.Generator.Model;

public record Flag : Option {
    public Flag(FlagDesc desc, ParserInfo parser, MinimalSymbolInfo backingSymbol, string? defaultValueExpr)
        : base(Utils.BOOLMinInfo, desc, parser, backingSymbol, defaultValueExpr) {}
}