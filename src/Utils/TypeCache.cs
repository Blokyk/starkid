namespace StarKid.Generator.Utils;

public readonly struct TypeCache<TValue>(
    Func<ITypeSymbol, TValue> getter,
    Dictionary<SpecialType, TValue> specialMap
) {
    private readonly Cache<ITypeSymbol, TValue> _map = new(SymbolEqualityComparer.Default, getter);

    public TValue GetValue(ITypeSymbol type) {
        if (specialMap.TryGetValue(type.SpecialType, out var val))
            return val;

        if (type.SpecialType == SpecialType.System_Nullable_T)
            return GetValue(((INamedTypeSymbol)type).TypeArguments[0]);

        return _map.GetValue(type);
    }
}