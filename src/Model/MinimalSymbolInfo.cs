namespace Recline.Generator;

public abstract record MinimalSymbolInfo(
    string Name,
    MinimalTypeInfo? ContainingType
) {
    public override string ToString() => ContainingType?.ToString() + (ContainingType is null ? "" : ".") + Name;

    internal static class Cache
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

        private static void ResetIfNeeded<T, U>(Dictionary<T, U> dic) {
            if (dic.Count > 100) {
                dic.Clear();
            }
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
}

public record MinimalTypeInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    string FullName,
    bool IsNullable
) : MinimalSymbolInfo(Name, ContainingType) {
    public override string ToString() => FullName;

    public static MinimalTypeInfo FromSymbol(ITypeSymbol type) {
        MinimalTypeInfo? containingType = null;

        if (type.ContainingType is not null)
            containingType = Cache.GetTypeInfo(type.ContainingType);

        bool isNullable
            = type.IsReferenceType
            ? type.NullableAnnotation == NullableAnnotation.Annotated
            : type.Name == "Nullable";

        return new MinimalTypeInfo(Cache.GetShortTypeName(type), containingType, Cache.GetFullTypeName(type), isNullable);
    }
}

public record MinimalMemberInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo Type
) : MinimalSymbolInfo(Name, ContainingType) {
    public override string ToString() => ContainingType!.ToString() + "." + SymbolUtils.GetSafeName(Name);

    public static MinimalMemberInfo FromSymbol(ISymbol symbol) {
        if (symbol is IPropertySymbol propSymbol)
            return new MinimalMemberInfo(propSymbol.Name, Cache.GetTypeInfo(propSymbol.ContainingType), Cache.GetTypeInfo(propSymbol.Type));
        else if (symbol is IFieldSymbol fieldSymbol)
            return new MinimalMemberInfo(fieldSymbol.Name, Cache.GetTypeInfo(fieldSymbol.ContainingType), Cache.GetTypeInfo(fieldSymbol.Type));
        else if (symbol is IMethodSymbol methodSymbol)
            return MinimalMethodInfo.FromSymbol(methodSymbol);

        throw new ArgumentException("Trying to create a MemberInfo from symbol type '" + symbol.GetType().Name + "'.", nameof(symbol));
    }
}

public record MinimalMethodInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo Type,
    ImmutableArray<MinimalParameterInfo> Parameters
) : MinimalMemberInfo(Name, ContainingType, Type) {
    public override string ToString() => ContainingType!.ToString() + "." + SymbolUtils.GetSafeName(Name);

    public static MinimalMethodInfo FromSymbol(IMethodSymbol symbol)
        => new(
            symbol.Name,
            Cache.GetTypeInfo(symbol.ContainingType),
            Cache.GetTypeInfo(symbol.ReturnType),
            ImmutableArray.CreateRange(symbol.Parameters, p => MinimalParameterInfo.FromSymbol(p))
        );

    public bool ReturnsVoid => Type.Name == "Void";
}

public record MinimalParameterInfo(
    string Name, // need the name cause the help text might change otherwise
    MinimalTypeInfo Type,
    bool IsNullable,
    bool IsParams,
    Optional<object?> DefaultValue
) : MinimalSymbolInfo(Name, null) {
    public static MinimalParameterInfo FromSymbol(IParameterSymbol symbol)
        => new(
            symbol.Name,
            Cache.GetTypeInfo(symbol.Type),
            symbol.NullableAnnotation == NullableAnnotation.Annotated,
            symbol.IsParams,
            symbol.HasExplicitDefaultValue ? new Optional<object?>(symbol.ExplicitDefaultValue) : new Optional<object?>()
        );

    public override string ToString() => SymbolUtils.GetSafeName(Name);

    public bool HasDefaultValue => DefaultValue.HasValue;
}