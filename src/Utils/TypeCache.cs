namespace Recline.Generator;

public readonly struct TypeCache<TValue>
{
    private readonly Cache<ITypeSymbol, TValue> _map;

    private readonly Dictionary<SpecialType, TValue> _specialMap;

    public TypeCache(
        Func<ITypeSymbol, TValue> getter,
        Dictionary<SpecialType, TValue> specialMap
    ) {
        _map = new(SymbolEqualityComparer.Default, getter);
        _specialMap = specialMap;
    }

    public TValue GetValue(ITypeSymbol type) {
        if (_specialMap.TryGetValue(type.SpecialType, out var val))
            return val;

        if (type.SpecialType == SpecialType.System_Nullable_T)
            return GetValue(((INamedTypeSymbol)type).TypeArguments[0]);

        return _map.GetValue(type);
    }
}