using System.Collections.Immutable;

namespace Recline.Generator;

internal static class MinimalSymbolInfoCache {
    private static Dictionary<ITypeSymbol, string> _typeNameMap = new();
    private static Dictionary<ITypeSymbol, MinimalTypeInfo> _typeInfoMap = new();

    internal static void Reset() {
        _typeNameMap.Clear();
        _typeInfoMap.Clear();
    }

    static void ResetIfNeeded() {
        if (_typeNameMap.Count > 1000)
            _typeNameMap.Clear();
        if (_typeInfoMap.Count > 100)
            _typeInfoMap.Clear();

    }

    internal static string GetTypeName(ITypeSymbol type) {
        ResetIfNeeded();

        if (!_typeNameMap.TryGetValue(type, out var name)) {
            name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            _typeNameMap.Add(type, name);
        }

        return name;
    }

    internal static MinimalTypeInfo GetTypeInfo(ITypeSymbol type) {
        ResetIfNeeded();

        if (!_typeInfoMap.TryGetValue(type, out var info)) {
            info = MinimalTypeInfo.FromSymbol(type);
            _typeInfoMap.Add(type, info);
        }

        return info;
    }
}

public abstract record MinimalSymbolInfo(
    string Name,
    MinimalTypeInfo? ContainingType
) {
    public override string ToString() => ContainingType?.ToString() + (ContainingType is null ? "" : ".") + Name;
}

public record MinimalTypeInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    string FullName
) : MinimalSymbolInfo(Name, ContainingType) {
    public static MinimalTypeInfo FromSymbol(ITypeSymbol type) {
        MinimalTypeInfo? containingType = null;

        if (type.ContainingType is not null)
            containingType = MinimalSymbolInfoCache.GetTypeInfo(type.ContainingType);

        return new MinimalTypeInfo(type.GetNameWithNull(), containingType, MinimalSymbolInfoCache.GetTypeName(type));
    }
}

public record MinimalMemberInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo Type
) : MinimalSymbolInfo(Name, ContainingType) {
    public static MinimalMemberInfo FromSymbol(ISymbol symbol) {
        if (symbol is IPropertySymbol propSymbol)
            return new MinimalMemberInfo(propSymbol.Name, MinimalSymbolInfoCache.GetTypeInfo(propSymbol.ContainingType), MinimalSymbolInfoCache.GetTypeInfo(propSymbol.Type));
        else if (symbol is IFieldSymbol fieldSymbol)
            return new MinimalMemberInfo(fieldSymbol.Name, MinimalSymbolInfoCache.GetTypeInfo(fieldSymbol.ContainingType), MinimalSymbolInfoCache.GetTypeInfo(fieldSymbol.Type));
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
    public static MinimalMethodInfo FromSymbol(IMethodSymbol symbol)
        => new(
            symbol.Name,
            MinimalSymbolInfoCache.GetTypeInfo(symbol.ContainingType),
            MinimalSymbolInfoCache.GetTypeInfo(symbol.ReturnType),
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
            MinimalSymbolInfoCache.GetTypeInfo(symbol.Type),
            symbol.NullableAnnotation == NullableAnnotation.Annotated,
            symbol.IsParams,
            symbol.HasExplicitDefaultValue ? new Optional<object?>(symbol.ExplicitDefaultValue) : new Optional<object?>()
        );

    public override string ToString() => Name;

    public bool HasDefaultValue => DefaultValue.HasValue;
}