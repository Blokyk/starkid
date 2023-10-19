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
}