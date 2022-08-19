namespace Recline.Generator;

public abstract record MinimalSymbolInfo(
    string Name,
    MinimalTypeInfo? ContainingType
) {
    public override string ToString() => ContainingType?.ToString() + (ContainingType is null ? "" : ".") + Name;

    internal static class Cache
    {
        private static WeakReference<Dictionary<ITypeSymbol, string>> _typeNameMapRef = new(new());
        private static WeakReference<Dictionary<ITypeSymbol, MinimalTypeInfo>> _typeInfoMapRef = new(new());

        internal static string GetTypeName(ITypeSymbol type) {
            if (!_typeNameMapRef.TryGetTarget(out var typeNameMap)) {
                typeNameMap = new();
                _typeNameMapRef.SetTarget(typeNameMap);
            }

            if (!typeNameMap.TryGetValue(type, out var name)) {
                name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                typeNameMap.Add(type, name);
            }

            return name;
        }

        internal static MinimalTypeInfo GetTypeInfo(ITypeSymbol type) {
            if (!_typeInfoMapRef.TryGetTarget(out var typeInfoMap)) {
                typeInfoMap = new();
                _typeInfoMapRef.SetTarget(typeInfoMap);
            }

            if (!typeInfoMap.TryGetValue(type, out var info)) {
                info = MinimalTypeInfo.FromSymbol(type);
                typeInfoMap.Add(type, info);
            }

            return info;
        }
    }
}

public record MinimalTypeInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    string FullName
) : MinimalSymbolInfo(Name, ContainingType) {
    public override string ToString() => FullName;

    public static MinimalTypeInfo FromSymbol(ITypeSymbol type) {
        MinimalTypeInfo? containingType = null;

        if (type.ContainingType is not null)
            containingType = Cache.GetTypeInfo(type.ContainingType);

        return new MinimalTypeInfo(type.GetNameWithNull(), containingType, Cache.GetTypeName(type));
    }
}

public record MinimalMemberInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo Type
) : MinimalSymbolInfo(Name, ContainingType) {
    public override string ToString() => ContainingType!.ToString() + "." + Name;

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
    public override string ToString() => ContainingType!.ToString() + "." + Name;

    public static MinimalMethodInfo FromSymbol(IMethodSymbol symbol)
        => new(
            symbol.Name,
            Cache.GetTypeInfo(symbol.ContainingType),
            Cache.GetTypeInfo(symbol.ReturnType),
            symbol.Parameters.Select(p => MinimalParameterInfo.FromSymbol(p)).ToImmutableArray()
        );

    public bool ReturnVoid => Name == "void";
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

    public override string ToString() => Name;

    public bool HasDefaultValue => DefaultValue.HasValue;
}