using System.Collections;

namespace StarKid.Generator.Utils;

/// <summary>
/// This method provides a wrapper for an <see cref="ImmutableArray{T}" /> that overrides the equality operation to provide elementwise comparison.
/// The default equality operation for an <see cref="ImmutableArray{T}" /> is reference equality of the underlying array, which is too strict
/// for many scenarios. This wrapper type allows us to use <see cref="ImmutableArray{T}" />s in our other record types without having to write an Equals method
/// that we may forget to update if we add new elements to the record.
/// </summary>
public readonly record struct ImmutableValueArray<T>(ImmutableArray<T> Array, IEqualityComparer<T> Comparer) : IEnumerable<T>
{
    public static readonly ImmutableValueArray<T> Empty = new([]);

    private readonly bool _useDefaultComparer = false;

    public bool IsDefault => Array.IsDefault;
    public bool IsDefaultOrEmpty => Array.IsDefaultOrEmpty;

    public ImmutableValueArray(ImmutableArray<T> array)
        : this(array, EqualityComparer<T>.Default)
        => _useDefaultComparer = true;

    public ImmutableValueArray<T> Add(T item)
        => _useDefaultComparer
            ? new(Array.Add(item))
            : new(Array.Add(item), Comparer);

    public T this[int i] => Array[i];

    public int Length => Array.Length;
    public ImmutableValueArray<T> Insert(int index, T item)
        => _useDefaultComparer
            ? new(Array.Insert(index, item))
            : new(Array.Insert(index, item), Comparer);

    public override int GetHashCode() {
        int res = 0;
        for (int i = 0; i < Array.Length; i++)
            res = Polyfills.CombineHashCodes(res, Array[i] is null ? 0 : Comparer.GetHashCode(Array[i]!));
        return res;
    }

#if NETSTANDARD2_0
    public bool Equals(ImmutableValueArray<T> other) => Array.SequenceEqual(other.Array, Comparer);
#else
    public bool Equals(ImmutableValueArray<T> other)
        => _useDefaultComparer
            ? Array.AsSpan().SequenceEqual(other.Array.AsSpan())
            : Array.AsSpan().SequenceEqual(other.Array.AsSpan(), Comparer);
#endif

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Array).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Array).GetEnumerator();
}

public static partial class CollectionExtensions
{
    public static ImmutableValueArray<T> ToImmutableValueArray<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer) => new([..source], comparer);
    public static ImmutableValueArray<T> ToImmutableValueArray<T>(this IEnumerable<T> source) => new([..source]);
    public static ImmutableValueArray<T> ToImmutableValueArray<T>(this ImmutableArray<T>.Builder builder, IEqualityComparer<T> comparer) => new(builder.ToImmutable(), comparer);
    public static ImmutableValueArray<T> ToImmutableValueArray<T>(this ImmutableArray<T>.Builder builder) => new(builder.ToImmutable());

    public static ImmutableValueArray<T> DrainToImmutableValueArray<T>(this ImmutableArray<T>.Builder builder) => new(builder.DrainToImmutable());
    public static ImmutableValueArray<T> MoveToImmutableValueArray<T>(this ImmutableArray<T>.Builder builder) => new(builder.MoveToImmutable());

    public static ImmutableValueArray<T> WithSequenceEquality<T>(this ImmutableArray<T> source) => new(source);
    public static ImmutableValueArray<T> ToValueArray<T>(this ImmutableArray<T> source) => new(source);
}