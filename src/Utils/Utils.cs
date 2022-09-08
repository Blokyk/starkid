namespace Recline.Generator;

internal static partial class Utils
{
    internal static INamedTypeSymbol BOOL = null!;
    internal static MinimalTypeInfo BOOLMinInfo = null!;
    internal static INamedTypeSymbol INT32 = null!;
    internal static MinimalTypeInfo INT32MinInfo = null!;
    internal static INamedTypeSymbol CHAR = null!;
    internal static MinimalTypeInfo CHARMinInfo = null!;
    internal static INamedTypeSymbol STR = null!;
    internal static MinimalTypeInfo STRMinInfo = null!;
    internal static INamedTypeSymbol VOID = null!;
    internal static MinimalTypeInfo VOIDMinInfo = null!;
    internal static INamedTypeSymbol EXCEPT = null!;
    internal static MinimalTypeInfo EXCEPTMinInfo = null!;
    internal static INamedTypeSymbol NULLABLE = null!;
    internal static MinimalTypeInfo NULLABLEMinInfo = null!;
    internal static INamedTypeSymbol TYPE = null!;
    internal static MinimalTypeInfo TYPEMinInfo = null!;

    internal static SymbolDisplayFormat memberMinimalDisplayFormat = new(
        parameterOptions: SymbolDisplayParameterOptions.IncludeName,
        //typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        //genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        //memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
    );

    internal static void UpdatePredefTypes(Compilation compilation) {
        Utils.BOOL ??= compilation.GetSpecialType(SpecialType.System_Boolean);
        Utils.INT32 ??= compilation.GetSpecialType(SpecialType.System_Int32);
        Utils.CHAR ??= compilation.GetSpecialType(SpecialType.System_Char);
        Utils.STR ??= compilation.GetSpecialType(SpecialType.System_String);
        Utils.VOID ??= compilation.GetSpecialType(SpecialType.System_Void);
        Utils.NULLABLE ??= compilation.GetSpecialType(SpecialType.System_Nullable_T);

        Utils.BOOLMinInfo ??= MinimalTypeInfo.FromSymbol(BOOL);
        Utils.INT32MinInfo ??= MinimalTypeInfo.FromSymbol(INT32);
        Utils.CHARMinInfo ??= MinimalTypeInfo.FromSymbol(CHAR);
        Utils.STRMinInfo ??= MinimalTypeInfo.FromSymbol(STR);
        Utils.VOIDMinInfo ??= MinimalTypeInfo.FromSymbol(VOID);
        Utils.NULLABLEMinInfo ??= MinimalTypeInfo.FromSymbol(NULLABLE);

        Utils.EXCEPT = compilation.GetTypeByMetadataName("System.Exception")!;
        Utils.EXCEPTMinInfo = MinimalTypeInfo.FromSymbol(EXCEPT);
    }

    public static bool TryGetAttribute(this INamedTypeSymbol type, string name, out AttributeData attr) {
        attr = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == name)!;

        return attr is not null;
    }

    public static bool TryGetConstantValue<T>(this SemanticModel model, SyntaxNode node, out T value) {
        var opt = model.GetConstantValue(node);

        if (!opt.HasValue || opt.Value is not T tVal) {
            value = default(T)!;
            return false;
        }

        value = tVal;
        return true;
    }

    public static string GetLastNamePart(string fullStr) {
        int lastDotIdx = 0;

        for (int i = 0; i < fullStr.Length; i++) {
            if (fullStr[i] == '.' && i + 1 < fullStr.Length)
                lastDotIdx = i + 1;
        }

        return fullStr.Substring(lastDotIdx).ToString();
    }

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

    /*public static string GetFullNameWithNull(this MinimalTypeInfo symbol) {
        if (symbol.Name == "Nullable")

        return GetRawName(symbol) + (symbol.NullableAnnotation != NullableAnnotation.Annotated ? "" : "?");
    }*/

    public static string GetSafeName(string name) {
        if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(name)))
            return '@' + name;

        return name;
    }

    public static bool Equals(this ISymbol? s1, ISymbol? s2) => SymbolEqualityComparer.Default.Equals(s1, s2);

    public static bool CanBeImplicitlyCastTo(this ITypeSymbol source, ITypeSymbol target, SemanticModel model)
        => model.Compilation.ClassifyCommonConversion(source, target).IsImplicit;

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

    public static Location GetApplicationLocation(AttributeData attr)
            => Location.Create(attr.ApplicationSyntaxReference!.SyntaxTree, attr.ApplicationSyntaxReference!.Span);

    //CURSED
    /*public static ITypeSymbol? GetMetadataName(this Type type, SemanticModel model) {
        if (!type.IsConstructedGenericType)
            return model.Compilation.GetTypeByMetadataName(type.FullName);

        var baseSymbol = model.Compilation.GetTypeByMetadataName(type.GetGenericTypeDefinition().FullName);

        if (baseSymbol is null)
            return null;

        var typeArgs = type.GenericTypeArguments;
        var typeArgSymbols = ImmutableArray.CreateBuilder<ITypeSymbol>();

        foreach (var arg in typeArgs) {
            var argSymbol = arg.GetMetadataName(model);

            if (argSymbol is null)
                return null;
            typeArgSymbols.Add(argSymbol);
        }

        return baseSymbol.Construct(typeArgSymbols.ToArray());
    }*/
}