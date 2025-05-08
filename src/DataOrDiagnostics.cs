namespace StarKid.Generator;

internal readonly struct DataOrDiagnostics<T> : IEquatable<DataOrDiagnostics<T>>
{
    public readonly T? Data { get; }

    public readonly ImmutableValueArray<Diagnostic> Diagnostics { get; }

    [MemberNotNullWhen(true, nameof(Data))]
    public readonly bool HasData { get; }

    public DataOrDiagnostics(T data) {
        Data = data;
        HasData = true;
        Diagnostics = ImmutableValueArray<Diagnostic>.Empty;
    }

    public DataOrDiagnostics(ImmutableArray<Diagnostic> diags) : this(diags.ToValueArray()) {}
    public DataOrDiagnostics(ImmutableValueArray<Diagnostic> diags) {
        Data = default;
        HasData = false;
        Diagnostics = diags;
    }

    public bool TryGetData([NotNullWhen(true)] out T? data) {
        data = Data;
        return HasData;
    }

    public bool Equals(DataOrDiagnostics<T> other)
        => HasData
            ? other.HasData && Data.Equals(other.Data)
            : Diagnostics.SequenceEqual(other.Diagnostics);

    public override readonly int GetHashCode()
        => HasData
            ? Data.GetHashCode()
            : Diagnostics.GetHashCode();

    public override readonly bool Equals(object? obj)
        => obj is DataOrDiagnostics<T> generatorDataWrapper && Equals(generatorDataWrapper);
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