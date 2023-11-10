using System.IO;
using System.Reflection;

namespace StarKid.Generator.Utils;

internal static class MiscUtils
{
    // polyfill for netstandard2.0
#if NETSTANDARD2_0
    public static bool StartsWith(this string s, char c) => s.Length >= 1 && s[0] == c;

    public static T FirstOrDefault<T>(this IEnumerable<T> coll, Func<T, bool> condition, T defaultVal) {
        foreach (var item in coll) {
            if (condition(item))
                return item;
        }

        return defaultVal;
    }

    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> pair,
        out TKey key,
        out TValue value
    ) {
        key = pair.Key;
        value = pair.Value;
    }

#pragma warning disable RCS1197 // Optimize StringBuilder call
    public static StringBuilder AppendJoin(this StringBuilder sb, string separator, params string[] values)
        => sb.Append(String.Join(separator, values));
    public static StringBuilder AppendJoin(this StringBuilder sb, string separator, params object[] values)
        => sb.Append(String.Join(separator, values));
    public static StringBuilder AppendJoin(this StringBuilder sb, char separator, params string[] values)
        => sb.Append(String.Join(separator.ToString(), values));
    public static StringBuilder AppendJoin(this StringBuilder sb, char separator, params object[] values)
        => sb.Append(String.Join(separator.ToString(), values));
    public static StringBuilder AppendJoin<T>(this StringBuilder sb, string separator, IEnumerable<T> values)
        => sb.Append(String.Join(separator, values));
    public static StringBuilder AppendJoin<T>(this StringBuilder sb, char separator, IEnumerable<T> values)
        => sb.Append(String.Join(separator.ToString(), values));
#pragma warning restore RCS1197

    public static int CombineHashCodes(int h1, int h2) => ((h1 << 5) + h1) ^ h2;
#else
    public static int CombineHashCodes(int h1, int h2) => HashCode.Combine(h1, h2);
#endif // NETSTANDARD2_0

    private static readonly Assembly _starkidAssembly = typeof(MiscUtils).Assembly;
    public static string GetStaticResource(string filePath) {
        using var stream = _starkidAssembly.GetManifestResourceStream("Blokyk.StarKid.Static." + filePath) ?? throw new InvalidOperationException("The requested resource 'StarKid.Static." + filePath + "' was not found");
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