using System.Collections.Concurrent;

namespace Recline.Generator.Model;

internal static class SymbolInfoCache
{
    private static readonly SymbolEqualityComparer symbolComparer = SymbolEqualityComparer.IncludeNullability;
    private static readonly ConcurrentDictionary<ITypeSymbol, string> _typeFullNameMap = new(symbolComparer);
    private static readonly ConcurrentDictionary<ITypeSymbol, string> _typeShortNameMap = new(symbolComparer);
    private static readonly ConcurrentDictionary<ITypeSymbol, MinimalTypeInfo> _typeInfoMap = new(symbolComparer);

    internal static void FullReset() {
        _typeFullNameMap.Clear();
        _typeShortNameMap.Clear();
        _typeInfoMap.Clear();
    }

    internal static string GetFullTypeName(ITypeSymbol type)
        => _typeFullNameMap.GetOrAdd(type, SymbolUtils.GetFullNameBad);

    internal static string GetShortTypeName(ITypeSymbol type)
        => _typeShortNameMap.GetOrAdd(type, SymbolUtils.GetNameWithNull);

    internal static MinimalTypeInfo GetTypeInfo(ITypeSymbol type)
        => _typeInfoMap.GetOrAdd(type, MinimalTypeInfo.FromSymbol);
}