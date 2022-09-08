namespace Recline.Generator;

internal static class SymbolUtils
{
    public static bool Equals(ISymbol? s1, ISymbol? s2) => SymbolEqualityComparer.Default.Equals(s1, s2);

    [Obsolete("Wrong one, buddy")]
    public new static bool Equals(object _, object __) => false;

    static string GetRawName(ISymbol symbol) {
        if (symbol is IArrayTypeSymbol arrayTypeSymbol) {
            return GetNameWithNull(arrayTypeSymbol.ElementType) + "[]";
        }

        if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType) {
            return symbol.Name + "<" + String.Join(",", namedTypeSymbol.TypeArguments.Select(a => GetNameWithNull(a))) + ">";
        }

        return symbol.Name;
    }

    public static string GetNameWithNull(this ITypeSymbol symbol) {
        if (symbol.Name == "Nullable")
            return GetRawName(symbol);

        return GetRawName(symbol) + (symbol.NullableAnnotation != NullableAnnotation.Annotated ? "" : "?");
    }

    public static string GetSafeName(string name) {
        if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(name)))
            return '@' + name;

        return name;
    }

    public static bool CanBeImplicitlyCastTo(this ITypeSymbol source, ITypeSymbol target, SemanticModel model)
        => model.Compilation.HasImplicitConversion(source, target); // model.Compilation.ClassifyCommonConversion(source, target).IsImplicit;

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
                    : getFullNameRecursive(symbol.ContainingType) + "." + GetRawName(symbol);

        static string getNamespaceRecursive(INamespaceSymbol ns)
            => ns.ContainingNamespace is null || ns.ContainingNamespace.IsGlobalNamespace
                    ? ns.Name
                    : getNamespaceRecursive(ns.ContainingNamespace) + "." + ns.Name;

        var symbolName = getFullNameRecursive(symbol);

        if (symbol.ContainingNamespace is null || symbol.ContainingNamespace.IsGlobalNamespace)
            return symbolName;

        return getNamespaceRecursive(symbol.ContainingNamespace) + "." + symbolName;
    }

    public static string GetErrorName(this ISymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
}