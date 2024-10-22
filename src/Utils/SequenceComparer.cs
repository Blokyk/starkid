namespace StarKid.Generator.Utils;

public readonly struct SequenceComparer<T> : IEqualityComparer<IEnumerable<T>>
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
            acc = Polyfills.CombineHashCodes(
                acc,
                item is null
                    ? acc
                    : EqualityComparer<T>.Default.GetHashCode(item)
            );
        }

        return acc;
    }
}