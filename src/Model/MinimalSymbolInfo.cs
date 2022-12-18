namespace Recline.Generator;

public abstract partial record MinimalSymbolInfo(
    string Name,
    MinimalTypeInfo? ContainingType
) {
    public override string ToString() => ContainingType?.ToString() + (ContainingType is null ? "" : ".") + Name;
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
            containingType = SymbolInfoCache.GetTypeInfo(type.ContainingType);

        bool isNullable
            = type.IsReferenceType
            ? type.NullableAnnotation == NullableAnnotation.Annotated
            : type.Name == "Nullable";

        return new MinimalTypeInfo(SymbolInfoCache.GetShortTypeName(type), containingType, SymbolInfoCache.GetFullTypeName(type), isNullable);
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
            return new MinimalMemberInfo(propSymbol.Name, SymbolInfoCache.GetTypeInfo(propSymbol.ContainingType), SymbolInfoCache.GetTypeInfo(propSymbol.Type));
        else if (symbol is IFieldSymbol fieldSymbol)
            return new MinimalMemberInfo(fieldSymbol.Name, SymbolInfoCache.GetTypeInfo(fieldSymbol.ContainingType), SymbolInfoCache.GetTypeInfo(fieldSymbol.Type));
        else if (symbol is IMethodSymbol methodSymbol)
            return MinimalMethodInfo.FromSymbol(methodSymbol);

        throw new ArgumentException("Trying to create a MemberInfo from symbol type '" + symbol.GetType().Name + "'.", nameof(symbol));
    }
}

public record MinimalMethodInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo ReturnType,
    ImmutableArray<MinimalParameterInfo> Parameters,
    ImmutableArray<MinimalTypeInfo> TypeArguments
) : MinimalMemberInfo(Name, ContainingType, ReturnType) {
    public override string ToString() => ContainingType!.ToString() + "." + SymbolUtils.GetSafeName(Name);

    public static MinimalMethodInfo FromSymbol(IMethodSymbol symbol)
        => new(
            symbol.Name,
            SymbolInfoCache.GetTypeInfo(symbol.ContainingType),
            SymbolInfoCache.GetTypeInfo(symbol.ReturnType),
            ImmutableArray.CreateRange(symbol.Parameters, MinimalParameterInfo.FromSymbol),
            ImmutableArray.CreateRange(symbol.TypeArguments, MinimalTypeInfo.FromSymbol)
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
            SymbolInfoCache.GetTypeInfo(symbol.Type),
            symbol.NullableAnnotation == NullableAnnotation.Annotated,
            symbol.IsParams,
            symbol.HasExplicitDefaultValue ? new Optional<object?>(symbol.ExplicitDefaultValue) : new Optional<object?>()
        );

    public override string ToString() => SymbolUtils.GetSafeName(Name);

    public bool HasDefaultValue => DefaultValue.HasValue;
}