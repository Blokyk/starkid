namespace StarKid.Generator.Utils;

internal static class CompilationExtensions
{
    public static SemanticModel GetCachedSemanticModel(this Compilation comp, SyntaxTree tree)
        => SemanticModelCache.GetSemanticModel(tree, comp);
    public static SemanticModel GetCachedSemanticModel(this Compilation comp, SyntaxNode node)
        => comp.GetCachedSemanticModel(node.SyntaxTree);

    /// <summary>
    /// Uses <see cref="SemanticModelCache"/> to call <see cref="SemanticModel.GetMemberGroup"/>.
    /// </summary>
    public static ImmutableArray<ISymbol> GetMemberGroup(this Compilation comp, SyntaxNode node)
        => comp.GetCachedSemanticModel(node).GetMemberGroup(node);

    /// <summary>
    /// Uses <see cref="SemanticModelCache"/> to call <see cref="SemanticModel.GetSymbolInfo"/>.
    /// </summary>
    public static SymbolInfo GetSymbolInfo(this Compilation comp, SyntaxNode node)
        => comp.GetCachedSemanticModel(node).GetSymbolInfo(node);
}