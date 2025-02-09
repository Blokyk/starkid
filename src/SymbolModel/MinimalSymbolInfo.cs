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
        => unchecked(
            -1521134295 * Name.GetHashCode()
            -1521134295 * (ContainingType?.GetHashCode() ?? 0)
        );

    public virtual bool Equals(MinimalSymbolInfo? s)
        => (object)this == s || (s is not null && ValueEquals(s));
    protected bool ValueEquals(MinimalSymbolInfo s)
        => Name == s?.Name
        && ContainingType == s?.ContainingType;
}

public record MinimalTypeInfo(
    string Name,
    MinimalTypeInfo? ContainingType,
    string FullName,
    bool IsNullable,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, ContainingType, Location), IEquatable<MinimalTypeInfo> {
    public override string ToString()
        => SpecialType is not SpecialType.None
        ? FullName
        : '@' + FullName;

    public SpecialType SpecialType { get; init; } = SpecialType.None;
    public ImmutableArray<MinimalTypeInfo> TypeArguments { get; init; } = ImmutableArray<MinimalTypeInfo>.Empty;

    public bool IsGeneric => TypeArguments.Length != 0;

    public override int GetHashCode()
        => SpecialType is not SpecialType.None
        ? (int)SpecialType
        : unchecked(
            -1521134295 * base.GetHashCode()
            -1521134295 * FullName.GetHashCode()
            -1521134295 * IsNullable.GetHashCode()
            -1521134295 * TypeArguments.GetHashCode()
        );

    public virtual bool Equals(MinimalTypeInfo? t)
        => (object)this == t || (t is not null && ValueEquals(t));
    protected bool ValueEquals(MinimalTypeInfo t)
        => IsNullable == t.IsNullable
        && (SpecialType is not SpecialType.None
                ? SpecialType == t.SpecialType
                : (base.ValueEquals(t)
                    && FullName == t.FullName
                    && TypeArguments.SequenceEqual(t.TypeArguments)
                )
        );

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
            ? namedType.TypeArguments.Select(SymbolInfoCache.GetTypeInfo).ToImmutableArray()
            : ImmutableArray<MinimalTypeInfo>.Empty;

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
}

public sealed record MinimalArrayTypeInfo(
    MinimalTypeInfo ElementType,
    bool IsNullable,
    MinimalLocation Location
) : MinimalTypeInfo(ElementType.Name + "[]", null, ElementType.FullName + "[]", IsNullable, Location), IEquatable<MinimalArrayTypeInfo> {
    public override string ToString() => base.ToString(); // required to prevent auto-gen'd ToString

    public override int GetHashCode()
        => unchecked(
            -1521134295 * ElementType.GetHashCode()
            -1521134295 * IsNullable.GetHashCode()
        );

    public bool Equals(MinimalArrayTypeInfo? t)
        => (object)this == t || (t is not null && ValueEquals(t));
    private bool ValueEquals(MinimalArrayTypeInfo t)
        => ElementType == t.ElementType
        && IsNullable == t.IsNullable;

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

public sealed record MinimalTypeParameterInfo(
    string Name,
    bool IsNullable,
    MinimalLocation Location
) : MinimalTypeInfo(Name, null, Name, IsNullable, Location), IEquatable<MinimalTypeParameterInfo> {
    internal override string DbgStr() => "`" + Name;
    public override string ToString() => base.ToString(); // wouldn't wanna break codegen

    public override int GetHashCode()
        => unchecked(
            -1521134295 * Name.GetHashCode()
            -1521134295 * IsNullable.GetHashCode()
        );

    public bool Equals(MinimalTypeParameterInfo? t)
        => (object)this == t || (t is not null && ValueEquals(t));
    private bool ValueEquals(MinimalTypeParameterInfo t)
        => Name == t.Name
        && IsNullable == t.IsNullable;

    public static MinimalTypeParameterInfo FromSymbol(ITypeParameterSymbol type)
        => new(type.Name, SymbolUtils.IsNullable(type), type.GetDefaultLocation());
}

public sealed record MinimalNullableValueTypeInfo : MinimalTypeInfo, IEquatable<MinimalNullableValueTypeInfo> {
    public MinimalTypeInfo ValueType { get; init; }

    public MinimalNullableValueTypeInfo(MinimalTypeInfo valueType, MinimalLocation loc)
        : base(valueType.Name + "?", null, "System.Nullable<" + valueType.FullName + ">", true, loc)
    {
        ValueType = valueType;
        TypeArguments = ImmutableArray.Create(valueType);
    }

    public override string ToString() => base.ToString();

    public override int GetHashCode()
        => ValueType.GetHashCode();

    public bool Equals(MinimalNullableValueTypeInfo? t)
        => (object)this == t || (t is not null && ValueEquals(t));
    private bool ValueEquals(MinimalNullableValueTypeInfo t)
        => SpecialType is not SpecialType.None
            ? SpecialType == t.SpecialType
            : ValueType == t.ValueType;

    public static MinimalNullableValueTypeInfo FromSymbol(INamedTypeSymbol type) {
        Debug.Assert(type.IsValueType && type.TypeArguments.Length == 1);

        var innerType = SymbolInfoCache.GetTypeInfo(type.TypeArguments[0]);

        var specialType
            = type.IsUnboundGenericType
            ? SpecialType.System_Nullable_T
            : innerType.SpecialType;

        return new(
            innerType,
            type.GetDefaultLocation()
        ) {
            TypeArguments = ImmutableArray.Create(innerType),
            SpecialType = specialType
        };
    }
}

public record MinimalMemberInfo(
    string Name,
    MinimalTypeInfo ContainingType,
    MinimalTypeInfo Type,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, ContainingType, Location), IEquatable<MinimalMemberInfo> {
    internal override string DbgStr() => ContainingType!.ToString() + '.' + SyntaxUtils.GetSafeName(Name);
    public override string ToString() => ContainingType!.ToString() + ".@" + Name;

    public override int GetHashCode()
        => unchecked(
            -1521134295 * base.GetHashCode()
            -1521134295 * Type.GetHashCode()
        );

    public virtual bool Equals(MinimalMemberInfo? t)
        => (object)this == t || (t is not null && ValueEquals(t));
    protected bool ValueEquals(MinimalMemberInfo t)
        => base.ValueEquals(t)
        && Type == t.Type;

    public static MinimalMemberInfo FromSymbol(ISymbol symbol) {
        if (symbol is IPropertySymbol propSymbol)
            return new MinimalMemberInfo(
                propSymbol.Name,
                SymbolInfoCache.GetTypeInfo(propSymbol.ContainingType),
                SymbolInfoCache.GetTypeInfo(propSymbol.Type),
                symbol.GetDefaultLocation()
            );
        else if (symbol is IFieldSymbol fieldSymbol)
            return new MinimalMemberInfo(
                fieldSymbol.Name,
                SymbolInfoCache.GetTypeInfo(fieldSymbol.ContainingType),
                SymbolInfoCache.GetTypeInfo(fieldSymbol.Type),
                symbol.GetDefaultLocation()
            );
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
    ImmutableArray<MinimalTypeInfo> TypeParameters,
    MinimalLocation Location
) : MinimalMemberInfo(Name, ContainingType, ReturnType, Location), IEquatable<MinimalMethodInfo> {
    internal override string DbgStr() =>
        $"{ReturnType.Name} {Name}<{String.Join(", ", TypeParameters)}>({String.Join(", ", Parameters)})";
    public override string ToString()
        => !IsGeneric
            ? $"{ContainingType!}.@{Name}"
            : $"{ContainingType!}.@{Name}<{String.Join(", ", TypeParameters)}>";

    public bool ReturnsVoid => ReturnType.SpecialType is SpecialType.System_Void;
    public bool IsGeneric => TypeParameters.Length > 0;

    public override int GetHashCode()
        => unchecked(
            -1521134295 * base.GetHashCode()
            -1521134295 * Parameters.GetHashCode()
            -1521134295 * TypeParameters.GetHashCode()
        );

    public bool Equals(MinimalMethodInfo? t)
        => (object)this == t || (t is not null && ValueEquals(t));
    private bool ValueEquals(MinimalMethodInfo t)
        => base.ValueEquals(t)
        && Parameters.SequenceEqual(t.Parameters)
        && TypeParameters.SequenceEqual(t.TypeParameters);

    public static MinimalMethodInfo FromSymbol(IMethodSymbol symbol)
        => new(
            symbol.Name,
            SymbolInfoCache.GetTypeInfo(symbol.ContainingType),
            SymbolInfoCache.GetTypeInfo(symbol.ReturnType),
            symbol.Parameters.Select(MinimalParameterInfo.FromSymbol).ToImmutableArray(),
            symbol.TypeArguments.Select(MinimalTypeInfo.FromSymbol).ToImmutableArray(),
            symbol.GetDefaultLocation()
        );
}

public sealed record MinimalParameterInfo(
    string Name, // need the name cause the help text might change otherwise
    MinimalTypeInfo Type,
    bool IsParams,
    MinimalLocation Location
) : MinimalSymbolInfo(Name, null, Location), IEquatable<MinimalParameterInfo> {
    public override string ToString() => '@' + Name;

    public bool IsNullable => Type.IsNullable;

    public override int GetHashCode()
        => unchecked(
            -1521134295 * Name.GetHashCode()
            -1521134295 * Type.GetHashCode()
            -1521134295 * IsParams.GetHashCode()
        );

    public bool Equals(MinimalParameterInfo? p)
        => (object)this == p || (p is not null && ValueEquals(p));
    private bool ValueEquals(MinimalParameterInfo p)
        => Name == p.Name
        && Type == p.Type
        && IsParams == p.IsParams;

    public static MinimalParameterInfo FromSymbol(IParameterSymbol symbol)
        => new(
            symbol.Name,
            SymbolInfoCache.GetTypeInfo(symbol.Type),
            symbol.IsParams,
            symbol.GetDefaultLocation()
        );
}