namespace Recline.Generator;

internal static class Utils
{
    public static readonly Random Random = new();

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

        return fullStr.Substring(lastDotIdx);
    }

    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> pair,
        out TKey key,
        out TValue value
    ) {
        key = pair.Key;
        value = pair.Value;
    }

    public static Location GetLocation(this SyntaxReference syntaxRef)
        => Location.Create(syntaxRef.SyntaxTree, syntaxRef.Span);

    public static Location GetApplicationLocation(AttributeData attr)
        => attr.ApplicationSyntaxReference?.GetLocation() ?? Location.None;

    public static ImmutableArray<string> GetUsings(TypeDeclarationSyntax classDec) {
        SyntaxNode? parent = classDec.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();

        var usingsSyntaxList = new SyntaxList<UsingDirectiveSyntax>();

        if (parent is null) {
            var unit = classDec.FirstAncestorOrSelf<CompilationUnitSyntax>();

            if (unit is null)
                return ImmutableArray<string>.Empty;

            usingsSyntaxList = unit.Usings;
        } else {
            var ns = (BaseNamespaceDeclarationSyntax)parent;
            usingsSyntaxList = ns.Usings;
        }

        return usingsSyntaxList
            .Where(u => u.Name is not null)
            .Select(u => u.Name!.ToString())
            .ToImmutableArray();
    }

    internal static int CombineHashCodes(int h1, int h2) =>  ((h1 << 5) + h1) ^ h2;

    internal static bool IsAsciiLetter(char c) => (uint)((c | 0x20) - 'a') <= 'z' - 'a';
    internal static bool IsAsciiDigit(char c) => (uint)(c - '0') <= '9' - '0';

    internal readonly struct SequenceComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        public static readonly SequenceComparer<T> Instance = new();

        public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
            => x is null
                ? y is null
                : y is not null
                    && x.SequenceEqual(y);

        public int GetHashCode(IEnumerable<T> obj) {
            int acc = 0;

            foreach (var item in obj) {
                acc = CombineHashCodes(
                        acc,
                        item is null
                            ? acc
                            : EqualityComparer<T>.Default.GetHashCode(item)
                    );
            }

            return acc;
        }
    }

    internal static IEqualityComparer<T> CreateComparerFrom<T>(Func<T, T, bool> eq, Func<T, int> hash)
        => new PredicateEqualityComparer<T>(eq, hash);

    private class PredicateEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _predicate;
        private readonly Func<T, int> _hash;

        public PredicateEqualityComparer(Func<T, T, bool> equality, Func<T, int> hash) {
            _predicate = equality;
            _hash = hash;
        }

        public bool Equals(T? a, T? b)
            => a is null
                ? b is null
                : b is not null && _predicate(a, b);

        public int GetHashCode(T a)
            => _hash(a);
    }

    public static IReadOnlyDictionary<TKey, TValue> EmptyDictionary<TKey, TValue>() => default(EmptyMapImpl<TKey, TValue>);

    private struct EmptyMapImpl<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        public readonly TValue this[TKey key] => throw new KeyNotFoundException();

        public readonly IEnumerable<TKey> Keys => Enumerable.Empty<TKey>();

        public readonly IEnumerable<TValue> Values => Enumerable.Empty<TValue>();

        public readonly int Count => 0;

        public readonly bool ContainsKey(TKey key)
            => false;

        public readonly bool TryGetValue(TKey key, out TValue value) {
            value = default!;
            return false;
        }

        public readonly IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => ((IEnumerable<KeyValuePair<TKey, TValue>>)Array.Empty<KeyValuePair<TKey, TValue>>()).GetEnumerator();

        readonly System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => Array.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
    }
}

internal class TupleComparer<T, U> : IEqualityComparer<Tuple<T, U>>, IEqualityComparer<ValueTuple<T, U>>
{
    private readonly IEqualityComparer<T> _tComparer;
    private readonly IEqualityComparer<U> _uComparer;

    public TupleComparer() : this(EqualityComparer<T>.Default, EqualityComparer<U>.Default) {}
    public TupleComparer(IEqualityComparer<T> tComparer, IEqualityComparer<U> uComparer) {
        _tComparer = tComparer;
        _uComparer = uComparer;
    }

    public bool Equals(Tuple<T, U>? x, Tuple<T, U>? y)
        =>  x is null
                ? y is null
                : y is not null
                    && _tComparer.Equals(
                            x.Item1,
                            y.Item1
                        )
                    && _uComparer.Equals(
                            x.Item2,
                            y.Item2
                        );

    public int GetHashCode(Tuple<T, U> obj)
        => GetHashCode(obj.ToValueTuple());

    public bool Equals((T, U) x, (T, U) y)
        => _tComparer.Equals(x.Item1, y.Item1) && _uComparer.Equals(x.Item2, y.Item2);
    public int GetHashCode((T, U) obj)
        => Utils.CombineHashCodes(
            obj.Item1 is null ? 0 : _tComparer.GetHashCode(obj.Item1),
            obj.Item2 is null ? 0 : _uComparer.GetHashCode(obj.Item2)
        );
}