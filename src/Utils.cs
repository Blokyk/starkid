namespace Recline.Generator;

internal static partial class Utils
{
    internal static INamedTypeSymbol BOOL = null!;
    internal static INamedTypeSymbol INT32 = null!;
    internal static INamedTypeSymbol CHAR = null!;
    internal static INamedTypeSymbol STR = null!;
    internal static INamedTypeSymbol VOID = null!;
    internal static INamedTypeSymbol EXCEPT = null!;
    internal static INamedTypeSymbol NULLABLE = null!;

    internal static SymbolDisplayFormat memberMinimalDisplayFormat = new(
        parameterOptions: SymbolDisplayParameterOptions.IncludeName,
        //typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        //genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        //memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
    );

    internal static void UpdatePredefTypes(Compilation compilation) {
        Utils.BOOL = compilation.GetSpecialType(SpecialType.System_Boolean);
        Utils.INT32 = compilation.GetSpecialType(SpecialType.System_Int32);
        Utils.CHAR = compilation.GetSpecialType(SpecialType.System_Char);
        Utils.STR = compilation.GetSpecialType(SpecialType.System_String);
        Utils.VOID = compilation.GetSpecialType(SpecialType.System_Void);
        Utils.NULLABLE = compilation.GetSpecialType(SpecialType.System_Nullable_T);
        Utils.EXCEPT = compilation.GetTypeByMetadataName("System.Exception")!;
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

    public static string GetFullName(this IMethodSymbol method) {
        return method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + method.Name;
    }

    // determine the namespace the class/enum/struct is declared in, if any
    public static string? GetNamespace(BaseTypeDeclarationSyntax syntax) {
        // If we don't have a namespace at all we'll return an empty string
        // This accounts for the "default namespace" case
        string? nameSpace = null;

        // Get the containing syntax node for the type declaration
        // (could be a nested type, for example)
        SyntaxNode? potentialNamespaceParent = syntax.Parent;

        // Keep moving "out" of nested classes etc until we get to a namespace
        // or until we run out of parents
        while (potentialNamespaceParent != null &&
                potentialNamespaceParent is not NamespaceDeclarationSyntax
                && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax) {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        // Build up the final namespace by looping until we no longer have a namespace declaration
        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent) {
            // We have a namespace. Use that as the type
            nameSpace = namespaceParent.Name.ToString();

            // Keep moving "out" of the namespace declarations until we 
            // run out of nested namespace declarations
            while (true) {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent) {
                    break;
                }

                // Add the outer namespace as a prefix to the final namespace
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                namespaceParent = parent;
            }
        }

        // return the final namespace
        return nameSpace;
    }

    public static bool TryGetCLIClassDecNode(INamedTypeSymbol symbol, out ClassDeclarationSyntax node) {
        var decNodeRefs = symbol.DeclaringSyntaxReferences;

        node = null!;

        if (decNodeRefs.Length == 0) {
            return false;
        } else if (decNodeRefs.Length == 1) {
            var syntax = decNodeRefs[0].GetSyntax();

            if (syntax is not ClassDeclarationSyntax classDecNode)
                return false;

            node = classDecNode;
        } else {
            foreach (var synRef in decNodeRefs) {
                var syn = synRef.GetSyntax();

                if (syn is not ClassDeclarationSyntax classDecNode)
                    continue;

                foreach (var attrib in classDecNode.AttributeLists.SelectMany(l => l.Attributes)) {
                    if (attrib.Name.ToString() is "CLI" or "CLIAttribute") {
                        node = classDecNode;
                        break;
                    }
                }

                if (node is not null)
                    break;
            }
        }

        return node is not null;
    }

    public static bool TryGetDescription(AttributeData descAttrib, out string? desc)
        => TryGetCtorArg<string?>(descAttrib, 0, STR, out desc);

    public static bool TryGetCtorArg<T>(AttributeData attrib, int ctorIdx, INamedTypeSymbol type, out T val) {
        val = default!;

        var ctorArgs = attrib.ConstructorArguments;

        if (ctorArgs.Length < ctorIdx + 1) {
            return false;
        }

        if (!Utils.Equals(ctorArgs[ctorIdx].Type, type) || ctorArgs[ctorIdx].Value is not T)
            return false;

        val = (T)ctorArgs[ctorIdx].Value!;

        return true;
    }

    public static bool TryGetProp<T>(AttributeData attrib, string propName, INamedTypeSymbol type, T defaultVal, out T val) {
        val = defaultVal;

        var namedArgs = attrib.NamedArguments;

        if (namedArgs.IsDefaultOrEmpty)
            return true;

        var arg = namedArgs.FirstOrDefault(
            kv => kv.Key == propName
        ).Value;

        if (arg.Equals(default))
            return true;

        if (!Utils.Equals(arg.Type, type) || arg.Value is not T)
            return false;

        val = (T)arg.Value!;

        return true;
    }

    public static string GetLastNamePart(ReadOnlySpan<char> fullStr) {
        int lastDotIdx = 0;

        for (int i = 0; i < fullStr.Length; i++) {
            if (fullStr[i] == '.' && i + 1 < fullStr.Length)
                lastDotIdx = i + 1;
        }

        return fullStr.Slice(lastDotIdx).ToString();
    }

    public static string GetNameWithNull(this ITypeSymbol symbol) {
        string GetRawName(ITypeSymbol symbol) {
            if (symbol is IArrayTypeSymbol arrayTypeSymbol) {
                return GetNameWithNull(arrayTypeSymbol.ElementType) + "[]";
            }

            return symbol.Name;
        }

        return GetRawName(symbol) + (symbol.NullableAnnotation != NullableAnnotation.Annotated ? "" : "?");
    }

    public static bool Equals(this ISymbol? s1, ISymbol? s2) => SymbolEqualityComparer.Default.Equals(s1, s2);
}

public sealed class NotNullWhenAttribute : Attribute {
    /// <summary>Initializes the attribute with the specified return value condition.</summary>
    /// <param name="returnValue">
    /// The return value condition. If the method returns this value, the associated parameter will not be null.
    /// </param>
    public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

    /// <summary>Gets the return value condition.</summary>
    public bool ReturnValue { get; }
}