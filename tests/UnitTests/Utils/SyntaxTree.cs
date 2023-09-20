namespace StarKid.Tests;

public static class SyntaxTree
{
    public static RS.SyntaxTree Of(string source) => Of(source, ParseOptions.Default);
    public static RS.SyntaxTree Of(string source, CSharpParseOptions options) => CSharpSyntaxTree.ParseText(source, options);

    public static RS.SyntaxTree WithMarkedNode(string source, out SyntaxNode node) {
        var startMarkerIdx = source.IndexOf("[|");

        if (startMarkerIdx == -1)
            throw new ArgumentException("Provided source code didn't contain any marker!");

        var beforeMarker = source[..startMarkerIdx];

        var endMarkerIdx = source.IndexOf("|]", startMarkerIdx);

        if (endMarkerIdx == -1)
            throw new ArgumentException("Unterminated marker");

        var markedText = source[(startMarkerIdx+2)..endMarkerIdx];

        var afterMarker = source[(endMarkerIdx+2)..];

        var cleanSource = beforeMarker + markedText + afterMarker;

        var tree = Of(cleanSource);

        node = tree.GetRoot().FindNode(new TextSpan(startMarkerIdx, endMarkerIdx - startMarkerIdx - 2));

        return tree;
    }

    public static bool TryGetNodeContainingText(this RS.SyntaxTree tree, string text, [NotNullWhen(true)] out SyntaxNode? node)
        => (node = tree.GetRoot().DescendantNodes(_ => true).FirstOrDefault(n => n.ToFullString().Contains(text))) != null;
}