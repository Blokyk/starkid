namespace Recline.Generator;

public readonly struct Cache<TKey, TValue>
{
    private readonly Dictionary<TKey, TValue> _map;
    private readonly Func<TKey, TValue> _getter;

    public Cache(IEqualityComparer<TKey> comparer, Func<TKey, TValue> generator) {
        _getter = generator;
        _map = new(comparer);
    }

    public Cache(Func<TKey, TValue> generator) : this(EqualityComparer<TKey>.Default, generator) {}

    public TValue GetValue(TKey key) {
        if (!_map.TryGetValue(key, out var val)) {
            val = _getter(key);
            _map.Add(key, val);
        }

        return val;
    }
}

public readonly struct Cache<TKey, TArg, TValue>
{
    private readonly Dictionary<TKey, TValue> _map;
    private readonly Func<TKey, TArg, TValue> _getter;

    public Cache(IEqualityComparer<TKey> comparer, Func<TKey, TArg, TValue> generator) {
        _getter = generator;
        _map = new(comparer);
    }

    public Cache(Func<TKey, TArg, TValue> generator) : this(EqualityComparer<TKey>.Default, generator) { }

    [System.Diagnostics.DebuggerHidden]
    public TValue GetValue(TKey key, TArg arg) {
        if (!_map.TryGetValue(key, out var val)) {
            val = _getter(key, arg);
            _map.Add(key, val);
        }

        return val;
    }
}