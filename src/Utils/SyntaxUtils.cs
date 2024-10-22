namespace StarKid.Generator.Utils;

internal static class SyntaxUtils
{
    public static Location GetLocation(this SyntaxReference syntaxRef)
        => syntaxRef.SyntaxTree.GetLocation(syntaxRef.Span);

    public static Location GetApplicationLocation(AttributeData attr)
        => attr.ApplicationSyntaxReference?.GetLocation() ?? Location.None;

    // note: this only needs to be used for debug purposes,
    // otherwise it's always safe to add a '@' prefix to an
    // identifier
    public static string GetSafeName(string name) {
        if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(name)))
            return '@' + name;

        return name;
    }

    public static int GetHashCode(SyntaxNode s) {
        var h1 = s.RawKind; // equivalent to Kind().GetHashCode() (yes, down to jit output :p)

        var h2 = s.ChildNodes().Aggregate(0, (acc, node) => MiscUtils.CombineHashCodes(acc, GetHashCode(node)));
        if (h2 == 0) // it'll only be 0 if the node didn't have any child
            h2 = s.ToString().GetHashCode();

        return MiscUtils.CombineHashCodes(h1, h2);
    }

    /// <summary>
    /// Traverse a syntax node's ancestors to figure out the reachable namespaces
    /// from its position, either through using directives or namespace declarations.
    /// </summary>
    internal static ImmutableArray<string> GetReachableNamespaceNames(SyntaxNode classDec) {
        SyntaxNode? parent = classDec.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();

        var usings = new List<UsingDirectiveSyntax>();

        var fullNamespaceNameParts = new List<string>();
        while (parent is BaseNamespaceDeclarationSyntax { Usings: var nsUsings } ns) {
            usings.AddRange(nsUsings);
            fullNamespaceNameParts.Add(ns.Name.ToString());
            parent = ns.Parent;
        }

        var unit = (parent ?? classDec).FirstAncestorOrSelf<CompilationUnitSyntax>();

        if (unit is not null)
            usings.AddRange(unit.Usings);

        var names = usings
            .Where(u => u.StaticKeyword == default && u.Name is not null)
            .Select(u => u.Name!.ToString());

        if (fullNamespaceNameParts.Count != 0)
            names = names.Append(String.Join(".", fullNamespaceNameParts.Reverse<string>())); // Reverse<T>() is IEnumerable, Reverse() is void

        return names.ToImmutableArray();
    }
}