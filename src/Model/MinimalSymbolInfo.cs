using System.Diagnostics;

namespace Recline.Generator.Model;

public abstract record MinimalSymbolInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    MinimalLocation Location
) : IEquatable<MinimalSymbolInfo> {
    public override string ToString()
        => ContainingType is null
            ? Name
            : ContainingType.ToString() + '.' + Name;

    public override int GetHashCode()
        => Utils.CombineHashCodes(
            Name.GetHashCode(),
            ContainingType?.GetHashCode() ?? 0
        );

    public virtual bool Equals(MinimalSymbolInfo? other)
        => (object)this == other // from roslyn's default equality for records
        || (other is not null && other.GetHashCode() == GetHashCode());
}

[DebuggerDisplay("{SymbolUtils.GetSafeName(Name),nq}")]
public record MinimalTypeInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    string FullName,
    bool IsNullable,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, ContainingType, Location) {
    public override string ToString() => '@' + FullName;

    public SpecialType SpecialType { get; init; } = SpecialType.None;
    public ImmutableArray<MinimalTypeInfo> TypeArguments { get; init; } = ImmutableArray<MinimalTypeInfo>.Empty;

    public bool IsGeneric => TypeArguments.Length != 0;

    public static MinimalTypeInfo FromSymbol(ITypeSymbol type) {
        if (type is IArrayTypeSymbol arrType)
            return MinimalArrayTypeInfo.FromSymbol(arrType);

        MinimalTypeInfo? containingType = null;

        if (type.ContainingType is not null)
            containingType = SymbolInfoCache.GetTypeInfo(type.ContainingType);

        bool isNullable = SymbolUtils.IsNullable(type);

        var typeArgs
            = type is INamedTypeSymbol namedType
            ? namedType.TypeArguments.Select(getTypeArgSymbol).ToImmutableArray()
            : ImmutableArray<MinimalTypeInfo>.Empty;

        return new MinimalTypeInfo(
            SymbolInfoCache.GetShortTypeName(type),
            containingType,
            SymbolInfoCache.GetFullTypeName(type),
            isNullable,
            type.GetDefaultLocation()
        ) {
            SpecialType = type.SpecialType,
            TypeArguments = typeArgs,
        };

        static MinimalTypeInfo getTypeArgSymbol(ITypeSymbol t) {
            if (t.TypeKind is not TypeKind.TypeParameter)
                return SymbolInfoCache.GetTypeInfo(t);

            return new MinimalTypeInfo(
                t.Name, null, t.Name, false, MinimalLocation.Default) {
                SpecialType = SpecialType.None,
                TypeArguments = ImmutableArray<MinimalTypeInfo>.Empty
            };
        }
    }
}

[DebuggerDisplay("{SymbolUtils.GetSafeName(Name),nq}")]
public sealed record MinimalArrayTypeInfo(
    MinimalTypeInfo ElementType,
    bool IsNullable,
    MinimalLocation Location
) : MinimalTypeInfo(ElementType.Name + "[]", null, ElementType.FullName + "[]", IsNullable, Location) {
    public override string ToString() => base.ToString(); // required to prevent auto-gen'd ToString

    public static MinimalArrayTypeInfo FromSymbol(IArrayTypeSymbol type) {
        var elementType = SymbolInfoCache.GetTypeInfo(type.ElementType);
        bool isNullable = SymbolUtils.IsNullable(type);

        return new MinimalArrayTypeInfo(
            elementType,
            isNullable,
            type.GetDefaultLocation()
        );
    }

}

[DebuggerDisplay("{ContainingType!.ToString(),nq} . {SymbolUtils.GetSafeName(Name),nq}")]
public record MinimalMemberInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo Type,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, ContainingType, Location) {
    public override string ToString() => ContainingType!.ToString() + ".@" + Name;

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
    public override string ToString() => ContainingType!.ToString() + ".@" + Name;

    public static MinimalMethodInfo FromSymbol(IMethodSymbol symbol)
        => new(
            symbol.Name,
            SymbolInfoCache.GetTypeInfo(symbol.ContainingType),
            SymbolInfoCache.GetTypeInfo(symbol.ReturnType),
            symbol.Parameters.Select(MinimalParameterInfo.FromSymbol).ToImmutableArray(),
            symbol.TypeArguments.Select(MinimalTypeInfo.FromSymbol).ToImmutableArray(),
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

[DebuggerDisplay("{SymbolUtils.GetSafeName(Name),nq}")]
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