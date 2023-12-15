using System.Globalization;

namespace StarKid.Generator.Utils;

internal static class SymbolUtils
{
    public static bool Equals(ISymbol? s1, ISymbol? s2) => SymbolEqualityComparer.Default.Equals(s1, s2);

    [Obsolete("Wrong one, buddy")]
    public new static bool Equals(object _, object __) => false;

    public static bool IsStringLike(ITypeSymbol type)
        => type.SpecialType is SpecialType.System_String
        || (type is INamedTypeSymbol nt && IsReadOnlySpanCharType(nt));

    public static bool IsReadOnlySpanCharType(INamedTypeSymbol type)
        => type is {
            MetadataName: "ReadOnlySpan`1",
            ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true },
            TypeArguments: [ {SpecialType: SpecialType.System_Char} ],
        };

    // Nullable<Nullable<int>> is not a valid type (Nullable<T> has T : nonnull)
    public static bool IsNullableValue(ITypeSymbol type)
        => type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T };

    public static bool IsNullable(ITypeSymbol type)
        => type.IsReferenceType
        ?  type.NullableAnnotation == NullableAnnotation.Annotated
        :  IsNullableValue(type);

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

    public static Location GetDefaultLocation(this ISymbol symbol)
        => symbol.Locations.FirstOrDefault(l => l.IsInSource, Location.None);

    public static string GetFullNameBad(ISymbol symbol) {
        static string getFullNameRecursive(ISymbol symbol)
            => symbol.ContainingType is null
                    ? GetRawName(symbol)
                    : getFullNameRecursive(symbol.ContainingType)
                        + "."
                        + (symbol is ITypeSymbol typeSymbol ? GetNameWithNull(typeSymbol) : symbol.Name);

        static string getNamespaceRecursive(INamespaceSymbol ns)
            => ns.ContainingNamespace is null or { IsGlobalNamespace: true }
                    ? ns.Name
                    : getNamespaceRecursive(ns.ContainingNamespace) + "." + ns.Name;

        var symbolName = getFullNameRecursive(symbol);

        if (symbol.ContainingNamespace is null or { IsGlobalNamespace: true })
            return symbolName;

        return getNamespaceRecursive(symbol.ContainingNamespace) + "." + symbolName;
    }

    public static string GetErrorName(this ISymbol symbol)
        => symbol switch {
            IParameterSymbol param => param.Name,
            _ => symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
        };

    public static bool IsInterfaceOn(this INamedTypeSymbol baseType, INamedTypeSymbol derived) {
        if (baseType.TypeKind is not TypeKind.Interface)
            return false;

        return derived.AllInterfaces.Contains(baseType, SymbolEqualityComparer.Default);
    }

    public static bool IsBaseOf(this INamedTypeSymbol baseType, INamedTypeSymbol derived) {
        var current = derived;

        while (current is not null && Equals(baseType, current))
            current = current.BaseType;

        // it'll only be null if we exited early
        return current is not null;
    }

    public static bool IsBaseOrInterfaceOf(this ITypeSymbol baseType, ITypeSymbol derived)
        => derived is INamedTypeSymbol derivedType && baseType.TypeKind switch {
            TypeKind.Class => IsBaseOf((INamedTypeSymbol)baseType, derivedType),
            TypeKind.Interface => IsInterfaceOn((INamedTypeSymbol)baseType, derivedType),
            _ => false
        };

    public static string? GetDefaultValue(ISymbol symbol) {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        if (symbol is not IParameterSymbol parameterSymbol)
            return null;

        if (!parameterSymbol.HasExplicitDefaultValue)
            return null;

        var defaultVal = parameterSymbol.ExplicitDefaultValue;

        if (defaultVal is null)
            return "null";

        return defaultVal switch {
            string s => '"' + s + '"',
            char c => "'" + c + "'",
            bool b => b ? "true" : "false",
            float f => f.ToString() + 'f',
            decimal dm => dm.ToString() + 'm',
            _ => defaultVal.ToString()
        };
    }

    public static DocumentationInfo? GetDocInfo(ISymbol symbol) {
        var xml = symbol.GetDocumentationCommentXml(preferredCulture: CultureInfo.InvariantCulture, expandIncludes: true);
        return String.IsNullOrEmpty(xml)
            ? null
            : DocumentationParser.ParseDocumentationInfoFrom(xml!);
    }
}