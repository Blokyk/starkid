using StarKid.Generator;
using StarKid.Generator.SymbolModel;
using StarKid.Generator.CommandModel;

namespace StarKid.Tests;

public static partial class GroupBuilderTests
{
    private static readonly ParserInfo.DirectMethod _intParser
        = new("System.Int32.Parse", CommonTypes.INT32);

    private static (List<Diagnostic> diags, dynamic gb) GetBuilder(CSharpCompilation comp) {
        var diags = new List<Diagnostic>();
        return (diags, new GroupBuilderProxy(ref diags, comp));
    }
}

internal class GroupBuilderProxy : PrivateProxy<GroupBuilder>
{
    private readonly AttributeListBuilder _attrBuilder;
    private static AttributeListBuilder CreateAndOutAttrBuilder(List<Diagnostic> diags, out AttributeListBuilder attrBuilder)
        => attrBuilder = new(diags.Add);
    public GroupBuilderProxy(ref List<Diagnostic> diags, CSharpCompilation comp)
    : base(new(CreateAndOutAttrBuilder(diags, out var builder), comp, diags.Add))
    => _attrBuilder = builder;

    internal bool TryGetArg_(IParameterSymbol param, [NotNullWhen(true)] out Argument? arg) {
        arg = null;
        return _attrBuilder.TryGetAttributeList(param, out var attrList)
            && ((dynamic)this).TryGetArg(param, attrList, out arg);
    }

    internal bool TryCreateOptionFrom_(ISymbol symbol, [NotNullWhen(true)] out Option? opt) {
        opt = null;
        return _attrBuilder.TryGetAttributeList(symbol, out var attrList)
            && ((dynamic)this).TryCreateOptionFrom(symbol, attrList, out opt);
    }
}
