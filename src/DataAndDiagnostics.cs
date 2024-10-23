namespace StarKid.Generator;

internal readonly struct DataAndDiagnostics<T> : IEquatable<DataAndDiagnostics<T>>
{
    public readonly T Data;

    // we specifically don't want order to matter here, because
    private readonly HashSet<Diagnostic> _diags;
    public readonly int DiagnosticsCount => _diags.Count;

    public readonly ImmutableValueArray<Diagnostic> GetDiagnostics()
        => _diags.ToImmutableValueArray();

    public DataAndDiagnostics(Func<Action<Diagnostic>, T> dataFunc) {
        _diags = [];
        Data = dataFunc(AddDiagnostic);
    }

    public DataAndDiagnostics(T data) : this() {
        _diags = [];
        Data = data;
    }

    public void AddDiagnostic(Diagnostic diag)
        => _diags.Add(diag);

    // fixme: implement different equality for diagnostics and data

    public readonly bool Equals(DataAndDiagnostics<T> other)
        => EqualityComparer<T?>.Default.Equals(Data, other.Data)
        && _diags.SetEquals(other._diags);

    public override readonly int GetHashCode()
        => Data is null
         ? _diags.GetHashCode()
         : Polyfills.CombineHashCodes(Data.GetHashCode(), _diags.GetHashCode());

    public override readonly bool Equals(object? obj)
        => obj is DataAndDiagnostics<T> generatorDataWrapper && Equals(generatorDataWrapper);
}