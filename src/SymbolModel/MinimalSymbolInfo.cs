using System.Diagnostics;

namespace StarKid.Generator.SymbolModel;

[DebuggerDisplay("{DbgStr(),nq}")]
public abstract record MinimalSymbolInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    MinimalLocation Location
) : IEquatable<MinimalSymbolInfo> {
    internal virtual string DbgStr() => SyntaxUtils.GetSafeName(Name);
    public override string ToString()
        => ContainingType is null
            ? Name
            : ContainingType.ToString() + '.' + Name;

    public override int GetHashCode()
        => MiscUtils.CombineHashCodes(
            Name.GetHashCode(),
            ContainingType?.GetHashCode() ?? 0
        );

    public virtual bool Equals(MinimalSymbolInfo? other)
        => (object)this == other // from roslyn's default equality for records
        || (other is not null && other.GetHashCode() == GetHashCode());
}

public record MinimalTypeInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    string FullName,
    bool IsNullable,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, ContainingType, Location) {
    public override string ToString()
        => SpecialType is not SpecialType.None
        ? FullName
        : '@' + FullName;

    public SpecialType SpecialType { get; init; } = SpecialType.None;
    public ImmutableValueArray<MinimalTypeInfo> TypeArguments { get; init; } = ImmutableValueArray<MinimalTypeInfo>.Empty;

    public bool IsGeneric => TypeArguments.Length != 0;

    public static MinimalTypeInfo FromSymbol(ITypeSymbol type) {
        if (type is IArrayTypeSymbol arrType)
            return MinimalArrayTypeInfo.FromSymbol(arrType);
        if (type is ITypeParameterSymbol paramType)
            return MinimalTypeParameterInfo.FromSymbol(paramType);
        if (type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nullableType)
            return MinimalNullableValueTypeInfo.FromSymbol(nullableType);

        MinimalTypeInfo? containingType = null;

        if (type.ContainingType is not null)
            containingType = SymbolInfoCache.GetTypeInfo(type.ContainingType);

        bool isNullable = SymbolUtils.IsNullable(type);

        var typeArgs
            = type is INamedTypeSymbol namedType
            ? namedType.TypeArguments.Select(SymbolInfoCache.GetTypeInfo).ToImmutableValueArray()
            : ImmutableValueArray<MinimalTypeInfo>.Empty;

        var fullName = SymbolInfoCache.GetFullTypeName(type);
        string shortName;

        if (type.SpecialType is SpecialType.System_Void) {
            fullName = "void";
            shortName = "void";
        } else {
            shortName = SymbolInfoCache.GetShortTypeName(type);
        }

        return new MinimalTypeInfo(
            shortName,
            containingType,
            fullName,
            isNullable,
            type.GetDefaultLocation()
        ) {
            SpecialType = type.SpecialType,
            TypeArguments = typeArgs,
        };
    }

    public override int GetHashCode() =>
        MiscUtils.CombineHashCodes(
            base.GetHashCode(),
            MiscUtils.CombineHashCodes(
                MiscUtils.CombineHashCodes(
                    SpecialType.GetHashCode(),
                    TypeArguments.GetHashCode()
                ),
                IsNullable ? 1 : 0
            )
        );
}

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

    public override int GetHashCode() => MiscUtils.CombineHashCodes(base.GetHashCode(), ElementType.GetHashCode());
}

public sealed record MinimalTypeParameterInfo(
    string Name,
    bool IsNullable,
    MinimalLocation Location
) : MinimalTypeInfo(Name, null, Name, IsNullable, Location) {
    internal override string DbgStr() => "`" + Name;
    public override string ToString() => base.ToString(); // wouldn't wanna break codegen

    public static MinimalTypeParameterInfo FromSymbol(ITypeParameterSymbol type)
        => new(type.Name, SymbolUtils.IsNullable(type), type.GetDefaultLocation());

    public override int GetHashCode() => MiscUtils.CombineHashCodes(Name.GetHashCode(), IsNullable ? 1 : 0);
}

public sealed record MinimalNullableValueTypeInfo(
    MinimalTypeInfo ValueType,
    MinimalLocation Location
) : MinimalTypeInfo(ValueType.Name + "?", null, "System.Nullable<"+ValueType.FullName+">", true, Location) {
    public override string ToString() => base.ToString();

    public static MinimalNullableValueTypeInfo FromSymbol(INamedTypeSymbol type) {
        Debug.Assert(type.IsValueType && type.TypeArguments.Length == 1);

        var innerType = SymbolInfoCache.GetTypeInfo(type.TypeArguments[0]);

        return new(
            innerType,
            type.GetDefaultLocation()
        ) {
            TypeArguments = ImmutableArray.Create(innerType).ToValueArray(),
            // if this is an unconstrained Nullable<T>, this will be System_Nullable_T, otherwise it'll be None
            SpecialType = type.SpecialType
        };
    }

    public override int GetHashCode() => ValueType.GetHashCode() + 1;
}

public record MinimalMemberInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo Type,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, ContainingType, Location) {
    internal override string DbgStr() => ContainingType!.ToString() + '.' + SyntaxUtils.GetSafeName(Name);
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

    public override int GetHashCode() => MiscUtils.CombineHashCodes(base.GetHashCode(), Type.GetHashCode());
}

public sealed record MinimalMethodInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo ReturnType,
    ImmutableValueArray<MinimalParameterInfo> Parameters,
    ImmutableValueArray<MinimalTypeInfo> TypeParameters,
    MinimalLocation Location
) : MinimalMemberInfo(Name, ContainingType, ReturnType, Location), IEquatable<MinimalMethodInfo> {
    internal override string DbgStr() =>
        $"{ReturnType.Name} {Name}<{String.Join(", ", TypeParameters)}>({String.Join(", ", Parameters)})";
    public override string ToString() => ContainingType!.ToString() + ".@" + Name;

    public bool ReturnsVoid => ReturnType.SpecialType is SpecialType.System_Void;
    public bool IsGeneric => TypeParameters.Length > 0;

    public static MinimalMethodInfo FromSymbol(IMethodSymbol symbol)
        => new(
            symbol.Name,
            SymbolInfoCache.GetTypeInfo(symbol.ContainingType),
            SymbolInfoCache.GetTypeInfo(symbol.ReturnType),
            symbol.Parameters.Select(MinimalParameterInfo.FromSymbol).ToImmutableValueArray(),
            symbol.TypeArguments.Select(MinimalTypeInfo.FromSymbol).ToImmutableValueArray(),
            symbol.GetDefaultLocation()
        );

    public override int GetHashCode()
        => MiscUtils.CombineHashCodes(
            base.GetHashCode(),
            MiscUtils.CombineHashCodes(
                Parameters.GetHashCode(),
                TypeParameters.GetHashCode()
            )
        );

    // doesn't use GetHashCode to take advantage of SpanHelpers.SequenceEqual's better perf
    public bool Equals(MinimalMethodInfo? other)
        => base.Equals(other)
        && Parameters.Equals(other.Parameters)
        && TypeParameters.Equals(other.TypeParameters);
}

public sealed record MinimalParameterInfo(
    string Name, // need the name cause the help text might change otherwise
    MinimalTypeInfo Type,
    bool IsParams,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, null, Location) {
    public static MinimalParameterInfo FromSymbol(IParameterSymbol symbol)
        => new(
            symbol.Name,
            SymbolInfoCache.GetTypeInfo(symbol.Type),
            symbol.IsParams,
            symbol.GetDefaultLocation()
        );

    public override string ToString() => '@' + Name;

    public bool IsNullable => Type.IsNullable;

    public override int GetHashCode() =>
        MiscUtils.CombineHashCodes(
            base.GetHashCode(),
            MiscUtils.CombineHashCodes(
                Type.GetHashCode(),
                IsParams ? 0 : 1
            )
        );
}