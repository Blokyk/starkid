using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace StarKid.Generator.Utils;

// Adapted from Roslyn's CachedSemanticModelProvider

internal static class SemanticModelCache
{
    private static readonly ConditionalWeakTable<Compilation, PerCompilationProvider>.CreateValueCallback _providerFactory
        = new(compilation => new(compilation));

    private static readonly ConditionalWeakTable<Compilation, PerCompilationProvider> _providerCache = new();

    public static SemanticModel GetSemanticModel(SyntaxTree tree, Compilation compilation, bool ignoreAccessibility = false)
        => _providerCache.GetValue(compilation, _providerFactory).GetSemanticModel(tree, ignoreAccessibility);

    internal static void ClearCache(Compilation compilation)
        => _providerCache.Remove(compilation);

    private sealed class PerCompilationProvider(Compilation compilation)
    {
        private readonly Compilation _compilation = compilation;
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelsMap = new();

        private readonly Func<SyntaxTree, SemanticModel> _createSemanticModel =
            tree => compilation.GetSemanticModel(tree, ignoreAccessibility: false);

        public SemanticModel GetSemanticModel(SyntaxTree tree, bool ignoreAccessibility)
            => ignoreAccessibility
                    ? _compilation.GetSemanticModel(tree, ignoreAccessibility: true)
                    : _semanticModelsMap.GetOrAdd(tree, _createSemanticModel);
    }
}