namespace Recline.Generator.Model;

internal static class SymbolInfoCache
{
    private static readonly SymbolEqualityComparer symbolComparer = SymbolEqualityComparer.IncludeNullability;
    private static readonly Dictionary<ITypeSymbol, string> _typeFullNameMap = new(symbolComparer);
    private static readonly Dictionary<ITypeSymbol, string> _typeShortNameMap = new(symbolComparer);
    private static readonly Dictionary<ITypeSymbol, MinimalTypeInfo> _typeInfoMap = new(symbolComparer);

    internal static void FullReset() {
        _typeFullNameMap.Clear();
        _typeShortNameMap.Clear();
        _typeInfoMap.Clear();
    }

    internal static string GetFullTypeName(ITypeSymbol type) {
        if (!_typeFullNameMap.TryGetValue(type, out var name)) {
            name = SymbolUtils.GetFullNameBad(type);
            _typeFullNameMap.Add(type, name);
        }

        return name;
    }

    internal static string GetShortTypeName(ITypeSymbol type) {
        if (!_typeShortNameMap.TryGetValue(type, out var name)) {
            name = type.GetNameWithNull();
            _typeShortNameMap.Add(type, name);
        }

        return name;
    }

    internal static MinimalTypeInfo GetTypeInfo(ITypeSymbol type) {
        if (!_typeInfoMap.TryGetValue(type, out var info)) {
            info = MinimalTypeInfo.FromSymbol(type);
            _typeInfoMap.Add(type, info);
        }

        return info;
    }
}