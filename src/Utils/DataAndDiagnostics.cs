namespace StarKid.Generator;

internal struct DataAndDiagnostics<T> : IEquatable<DataAndDiagnostics<T>>
{
    public T? Data { get; set; }

    // we specifically don't want order to matter here, because
    private readonly HashSet<Diagnostic> _diags;
    public readonly int DiagnosticsCount => _diags.Count;

    public readonly ImmutableValueArray<Diagnostic> GetDiagnostics()
        => _diags.ToImmutableValueArray();

    public DataAndDiagnostics() {
        Data = default(T);
        _diags = new();
    }

    public DataAndDiagnostics(T data) : this()
        => Data = data;

#pragma warning disable IDE0251 // Yes, roslyn, this method does in fact have side-effects :|
    public void AddDiagnostic(Diagnostic diag)
        => _diags.Add(diag);
#pragma warning restore

    // fixme: implement different equality for diagnostics and data

    public readonly bool Equals(DataAndDiagnostics<T> other)
        => EqualityComparer<T?>.Default.Equals(Data, other.Data)
        && _diags.SetEquals(other._diags);

    public override readonly int GetHashCode()
        => Data is null
         ? _diags.GetHashCode()
         : Utils.CombineHashCodes(Data.GetHashCode(), _diags.GetHashCode());

    public override readonly bool Equals(object? obj)
        => obj is DataAndDiagnostics<T> generatorDataWrapper && Equals(generatorDataWrapper);
}