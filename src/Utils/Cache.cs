namespace StarKid.Generator.Utils;

public readonly struct Cache<TKey, TValue>(
    IEqualityComparer<TKey> comparer,
    Func<TKey, TValue> generator
) where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _map = new(comparer);
    public readonly bool IsInitialized = true;

    public TValue GetValue(TKey key) {
        ThrowIfNotInitialized();

        if (!_map.TryGetValue(key, out var val)) {
            val = generator(key);
            _map.Add(key, val);
        }

        return val;
    }

    internal void ForceAdd(TKey key, TValue value) {
        ThrowIfNotInitialized();

        _map.Add(key, value);
    }

    private void ThrowIfNotInitialized() {
        if (!IsInitialized)
            throw new InvalidOperationException("The cache was not correctly initialized.");
    }
}

public readonly struct Cache<TKey, TArg, TValue>(
    IEqualityComparer<TKey> keyComparer,
    IEqualityComparer<TArg> argComparer,
    Func<TKey, TArg, TValue> generator
) {
    private readonly Dictionary<(TKey, TArg), TValue> _map
        = new(new TupleComparer<TKey, TArg>(keyComparer, argComparer));

    [System.Diagnostics.DebuggerHidden]
    public TValue GetValue(TKey key, TArg arg) {
        if (!_map.TryGetValue((key, arg), out var val)) {
            val = generator(key, arg);
            _map.Add((key, arg), val);
        }

        return val;
    }
}