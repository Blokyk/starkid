namespace StarKid.Generator.Utils;

internal class TupleComparer<T, U>(
    IEqualityComparer<T> tComparer,
    IEqualityComparer<U> uComparer
) : IEqualityComparer<Tuple<T, U>>, IEqualityComparer<ValueTuple<T, U>>
{
    public TupleComparer() : this(EqualityComparer<T>.Default, EqualityComparer<U>.Default) { }

    public bool Equals(Tuple<T, U>? x, Tuple<T, U>? y)
        => x is null
                ? y is null
                : y is not null
                    && tComparer.Equals(
                            x.Item1,
                            y.Item1
                        )
                    && uComparer.Equals(
                            x.Item2,
                            y.Item2
                        );

    public int GetHashCode(Tuple<T, U> obj)
        => GetHashCode(obj.ToValueTuple());

    public bool Equals((T, U) x, (T, U) y)
        => tComparer.Equals(x.Item1, y.Item1) && uComparer.Equals(x.Item2, y.Item2);
    public int GetHashCode((T, U) obj)
        => MiscUtils.CombineHashCodes(
            obj.Item1 is null ? 0 : tComparer.GetHashCode(obj.Item1),
            obj.Item2 is null ? 0 : uComparer.GetHashCode(obj.Item2)
        );
}