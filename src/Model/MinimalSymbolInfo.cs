namespace Recline.Generator.Model;

public abstract record MinimalSymbolInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    MinimalLocation Location
) : IEquatable<MinimalSymbolInfo> {
    public override string ToString() => ContainingType?.ToString() + (ContainingType is null ? "" : ".") + Name;

    public override int GetHashCode()
        => Utils.CombineHashCodes(
            Name.GetHashCode(),
            ContainingType?.GetHashCode() ?? 0
        );

    public virtual bool Equals(MinimalSymbolInfo? other)
        => (object)this == other // from roslyn's default equality for records
        || (other is not null && other.GetHashCode() == GetHashCode());
}

public sealed record MinimalTypeInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    string FullName,
    bool IsNullable,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, ContainingType, Location) {
    public override string ToString() => FullName;

    public static MinimalTypeInfo FromSymbol(ITypeSymbol type) {
        MinimalTypeInfo? containingType = null;

        if (type.ContainingType is not null)
            containingType = SymbolInfoCache.GetTypeInfo(type.ContainingType);

        bool isNullable
            = type.IsReferenceType
            ? type.NullableAnnotation == NullableAnnotation.Annotated
            : (type as INamedTypeSymbol)?.SpecialType == SpecialType.System_Nullable_T;

        return new MinimalTypeInfo(SymbolInfoCache.GetShortTypeName(type), containingType, SymbolInfoCache.GetFullTypeName(type), isNullable, type.GetDefaultLocation());
    }
}

public record MinimalMemberInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo Type,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, ContainingType, Location) {
    public override string ToString() => ContainingType!.ToString() + "." + SymbolUtils.GetSafeName(Name);

    public static MinimalMemberInfo FromSymbol(ISymbol symbol) {
        if (symbol is IPropertySymbol propSymbol)
            return new MinimalMemberInfo(propSymbol.Name, SymbolInfoCache.GetTypeInfo(propSymbol.ContainingType), SymbolInfoCache.GetTypeInfo(propSymbol.Type), symbol.GetDefaultLocation());
        else if (symbol is IFieldSymbol fieldSymbol)
            return new MinimalMemberInfo(fieldSymbol.Name, SymbolInfoCache.GetTypeInfo(fieldSymbol.ContainingType), SymbolInfoCache.GetTypeInfo(fieldSymbol.Type), symbol.GetDefaultLocation());
        else if (symbol is IMethodSymbol methodSymbol)
            return MinimalMethodInfo.FromSymbol(methodSymbol);

        throw new ArgumentException("Trying to create a MemberInfo from symbol type '" + symbol.GetType().Name + "'.", nameof(symbol));
    }
}

public sealed record MinimalMethodInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo ReturnType,
    ImmutableArray<MinimalParameterInfo> Parameters,
    ImmutableArray<MinimalTypeInfo> TypeArguments,
    MinimalLocation Location
) : MinimalMemberInfo(Name, ContainingType, ReturnType, Location), IEquatable<MinimalMethodInfo> {
    public override string ToString() => ContainingType!.ToString() + "." + SymbolUtils.GetSafeName(Name);

    public static MinimalMethodInfo FromSymbol(IMethodSymbol symbol)
        => new(
            symbol.Name,
            SymbolInfoCache.GetTypeInfo(symbol.ContainingType),
            SymbolInfoCache.GetTypeInfo(symbol.ReturnType),
            ImmutableArray.CreateRange(symbol.Parameters, MinimalParameterInfo.FromSymbol),
            ImmutableArray.CreateRange(symbol.TypeArguments, MinimalTypeInfo.FromSymbol),
            symbol.GetDefaultLocation()
        );

    public override int GetHashCode()
        => Utils.CombineHashCodes(
            base.GetHashCode(),
            Utils.CombineHashCodes(
                Utils.SequenceComparer<MinimalParameterInfo>.Instance.GetHashCode(Parameters),
                Utils.SequenceComparer<MinimalTypeInfo>.Instance.GetHashCode(TypeArguments)
            )
        );

    // doesn't use GetHashCode to take advantage of SpanHelpers.SequenceEqual's better perf
    public bool Equals(MinimalMethodInfo? other)
        => base.Equals(other)
        && Parameters.AsSpan().SequenceEqual(other.Parameters.AsSpan())
        && TypeArguments.AsSpan().SequenceEqual(other.TypeArguments.AsSpan());

    public bool ReturnsVoid => ReturnType.Name == "Void";
    public bool IsGeneric => TypeArguments.Length > 0;
}

public sealed record MinimalParameterInfo(
    string Name, // need the name cause the help text might change otherwise
    MinimalTypeInfo Type,
    bool IsNullable,
    bool IsParams,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, null, Location) {
    public static MinimalParameterInfo FromSymbol(IParameterSymbol symbol)
        => new(
            symbol.Name,
            SymbolInfoCache.GetTypeInfo(symbol.Type),
            symbol.NullableAnnotation == NullableAnnotation.Annotated,
            symbol.IsParams,
            symbol.GetDefaultLocation()
        );

    public override string ToString() => SymbolUtils.GetSafeName(Name);
}