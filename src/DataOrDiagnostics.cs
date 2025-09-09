namespace StarKid.Generator;

internal readonly struct DataOrDiagnostics<T> : IEquatable<DataOrDiagnostics<T>>
{
    private readonly T? _data;

    public readonly ImmutableValueArray<Diagnostic> Diagnostics { get; }

    [MemberNotNullWhen(true, nameof(_data))]
    public bool HasData => _data is not null;

    public DataOrDiagnostics(T data) {
        _data = data;
        Diagnostics = [];
    }

    public DataOrDiagnostics(ImmutableArray<Diagnostic> diags) : this(diags.ToValueArray()) {}
    public DataOrDiagnostics(ImmutableValueArray<Diagnostic> diags) {
        _data = default;
        Diagnostics = diags;
    }

    public bool TryGetData([NotNullWhen(true)] out T? data) {
        data = _data;
        return HasData;
    }

    public bool Equals(DataOrDiagnostics<T> other)
        => HasData
            ? other.HasData && _data.Equals(other._data)
            : Diagnostics.SequenceEqual(other.Diagnostics);

    public override readonly int GetHashCode()
        => HasData
            ? _data.GetHashCode()
            : Diagnostics.GetHashCode();

    public override readonly bool Equals(object? obj)
        => obj is DataAndDiagnostics<T> generatorDataWrapper && Equals(generatorDataWrapper);
}

internal static class DataOrDiagnostics
{
    public static DataOrDiagnostics<T> From<T>(Func<Action<Diagnostic>, T?> dataProvider) {
        var diagsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();
        var t = dataProvider(diagsBuilder.Add);

        return diagsBuilder.Count is 0
            ? new(t!)
            : new(diagsBuilder.DrainToImmutable());
    }

    public static DataOrDiagnostics<TResult> Map<T, TResult>(this DataOrDiagnostics<T> dataOrDiags, Func<T, TResult> f)
        => dataOrDiags.TryGetData(out var data) ? new(f(data)) : new(dataOrDiags.Diagnostics);

    public static DataOrDiagnostics<TResult> Map<T, TResult>(this DataOrDiagnostics<T> dataOrDiags, Func<Action<Diagnostic>, T, TResult?> f) {
        if (!dataOrDiags.TryGetData(out var data))
            return new(dataOrDiags.Diagnostics); // just pass along the old diags in a newly typed container

        var newDiags = ImmutableArray.CreateBuilder<Diagnostic>();
        var t = f(newDiags.Add, data);

        return newDiags.Count is 0
            ? new(t!)
            : new(newDiags.DrainToImmutable());
    }
}