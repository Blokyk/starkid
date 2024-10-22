internal static class Polyfills
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
}