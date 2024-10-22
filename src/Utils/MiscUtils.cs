using System.IO;
using System.Reflection;

namespace StarKid.Generator.Utils;

internal static class MiscUtils
{
    private static readonly Assembly _starkidAssembly = typeof(MiscUtils).Assembly;
    public static string GetStaticResource(string filePath) {
        using var stream = _starkidAssembly.GetManifestResourceStream("Blokyk.StarKid.Static." + filePath) ?? throw new InvalidOperationException("The requested resource 'Blokyk.StarKid.Static." + filePath + "' was not found");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static bool IsAsciiLetter(char c) => (uint)((c | 0x20) - 'a') <= 'z' - 'a';
    public static bool IsAsciiDigit(char c) => (uint)(c - '0') <= '9' - '0';
    public static bool IsAsciiLetterLower(char c) => (uint)(c - 'a') <= 'z' - 'a';
    public static bool IsAsciiLetterUpper(char c) => (uint)(c - 'A') <= 'Z' - 'A';

    public static string CapitalizeString(string s) {
        if (String.IsNullOrWhiteSpace(s))
            return s;
        var chars = s.ToCharArray();
        chars[0] = Char.ToUpperInvariant(chars[0]);
        return new string(chars);
    }

    public static IEqualityComparer<T> CreateComparerFrom<T>(Func<T, T, bool> eq, Func<T, int> hash)
        => new PredicateEqualityComparer<T>(eq, hash);

    private class PredicateEqualityComparer<T>(
        Func<T, T, bool> equality,
        Func<T, int> hash
    ) : IEqualityComparer<T>
    {
        public bool Equals(T? a, T? b)
            => a is null
                ? b is null
                : b is not null && equality(a, b);

        public int GetHashCode(T a)
            => hash(a);
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