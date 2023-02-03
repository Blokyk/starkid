namespace Recline.Generator;

public readonly struct Cache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _map;
    private readonly Func<TKey, TValue> _getter;

    public readonly bool IsInitialized = false;

    public Cache(IEqualityComparer<TKey> comparer, Func<TKey, TValue> generator) {
        _getter = generator;
        _map = new(comparer);
        IsInitialized = true;
    }

    public TValue GetValue(TKey key) {
        ThrowIfNotInitialized();

        if (!_map.TryGetValue(key, out var val)) {
            val = _getter(key);
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

public readonly struct Cache<TKey, TArg, TValue>
{
    private readonly Dictionary<(TKey, TArg), TValue> _map;
    private readonly Func<TKey, TArg, TValue> _getter;

    public Cache(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TArg> argComparer, Func<TKey, TArg, TValue> generator) {
        _getter = generator;
        _map = new(new TupleComparer<TKey, TArg>(keyComparer, argComparer));
    }

    [System.Diagnostics.DebuggerHidden]
    public TValue GetValue(TKey key, TArg arg) {
        if (!_map.TryGetValue((key, arg), out var val)) {
            val = _getter(key, arg);
            _map.Add((key, arg), val);
        }

        return val;
    }
}