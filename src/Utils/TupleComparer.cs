namespace StarKid.Generator.Utils;

internal class TupleComparer<T, U> : IEqualityComparer<Tuple<T, U>>, IEqualityComparer<ValueTuple<T, U>>
{
    private readonly IEqualityComparer<T> _tComparer;
    private readonly IEqualityComparer<U> _uComparer;

    public TupleComparer() : this(EqualityComparer<T>.Default, EqualityComparer<U>.Default) { }
    public TupleComparer(IEqualityComparer<T> tComparer, IEqualityComparer<U> uComparer) {
        _tComparer = tComparer;
        _uComparer = uComparer;
    }

    public bool Equals(Tuple<T, U>? x, Tuple<T, U>? y)
        => x is null
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
        => MiscUtils.CombineHashCodes(
            obj.Item1 is null ? 0 : _tComparer.GetHashCode(obj.Item1),
            obj.Item2 is null ? 0 : _uComparer.GetHashCode(obj.Item2)
        );
}