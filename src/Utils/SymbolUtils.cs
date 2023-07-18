namespace Recline.Generator;

internal static class SymbolUtils
{
    public static bool Equals(ISymbol? s1, ISymbol? s2) => SymbolEqualityComparer.Default.Equals(s1, s2);

    [Obsolete("Wrong one, buddy")]
    public new static bool Equals(object _, object __) => false;

    public static bool IsNullable(ITypeSymbol type)
        => type.IsReferenceType
        ?  type.NullableAnnotation == NullableAnnotation.Annotated
        :  type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T };

    public static string GetRawName(ISymbol symbol) {
        if (symbol is IArrayTypeSymbol arrayTypeSymbol) {
            return GetNameWithNull(arrayTypeSymbol.ElementType) + "[]";
        }

        if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType) {
            return symbol.Name + "<" + String.Join(",", namedTypeSymbol.TypeArguments.Select(a => GetNameWithNull(a))) + ">";
        }

        return symbol.Name;
    }

    public static string GetNameWithNull(this ITypeSymbol symbol) {
        if (IsNullable(symbol))
            return GetRawName(symbol) + "?";
        else
            return GetRawName(symbol);
    }

    public static string GetSafeName(string name) {
        if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(name)))
            return '@' + name;

        return name;
    }

    public static Location GetDefaultLocation(this ISymbol symbol) {
        if (symbol.Locations.IsDefaultOrEmpty)
            return Location.None;
        else
            return symbol.Locations[0];
    }

    public static string GetFullNameBad(ISymbol symbol) {
        static string getFullNameRecursive(ISymbol symbol)
            => symbol.ContainingType is null
                    ? GetRawName(symbol)
                    : getFullNameRecursive(symbol.ContainingType)
                        + "."
                        + (symbol is ITypeSymbol typeSymbol ? GetNameWithNull(typeSymbol) : symbol.Name);

        static string getNamespaceRecursive(INamespaceSymbol ns)
            => ns.ContainingNamespace?.IsGlobalNamespace != false
                    ? ns.Name
                    : getNamespaceRecursive(ns.ContainingNamespace) + "." + ns.Name;

        var symbolName = getFullNameRecursive(symbol);

        if (symbol.ContainingNamespace?.IsGlobalNamespace != false)
            return symbolName;

        return getNamespaceRecursive(symbol.ContainingNamespace) + "." + symbolName;
    }

    public static string GetErrorName(this ISymbol symbol)
        => symbol switch {
            IParameterSymbol param => param.Name,
            _ => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
        };

    public static bool IsBaseOf(this ITypeSymbol baseType, ITypeSymbol derived) {
        var current = derived;

        while (current is not null && Equals(baseType, current))
            current = current.BaseType;

        // if we exited early, then it will be null
        return current is not null;
    }
}