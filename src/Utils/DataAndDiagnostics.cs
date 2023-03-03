namespace Recline.Generator;

internal struct DataAndDiagnostics<T> : IEquatable<DataAndDiagnostics<T>>
{
    public T? Data { get; set; }

    private readonly HashSet<Diagnostic> _diags;
    public int DiagnosticsCount => _diags.Count;

    public ImmutableArray<Diagnostic> GetDiagnostics()
        => _diags.ToImmutableArray();

    public DataAndDiagnostics() {
        Data = default(T);
        _diags = new();
    }

    public DataAndDiagnostics(T data) : this()
        => Data = data;

    public void AddDiagnostic(Diagnostic diag)
        => _diags.Add(diag);

    // fixme: implement different equality for diagnostics and data

    public bool Equals(DataAndDiagnostics<T> other)
        => EqualityComparer<T?>.Default.Equals(Data, other.Data)
        && _diags.SetEquals(other._diags);

    public override int GetHashCode()
        => Data is null
         ? _diags.GetHashCode()
         : Utils.CombineHashCodes(Data.GetHashCode(), _diags.GetHashCode());

    public override bool Equals(object? obj)
        => obj is DataAndDiagnostics<T> generatorDataWrapper && Equals(generatorDataWrapper);
}