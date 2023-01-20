namespace Recline.Generator;

internal struct GeneratorDataWrapper<T> : IEquatable<GeneratorDataWrapper<T>>
{
    public T? Data { get; set; }

    private readonly HashSet<Diagnostic> _diags;
    public int DiagnosticsCount => _diags.Count;

    public ImmutableArray<Diagnostic> GetDiagnostics()
        => _diags.ToImmutableArray();

    public GeneratorDataWrapper() {
        Data = default(T);
        _diags = new();
    }

    public GeneratorDataWrapper(T data) : this()
        => Data = data;

    public void AddDiagnostic(Diagnostic diag)
        => _diags.Add(diag);

    // fixme: implement different equality for diagnostics and data

    public bool Equals(GeneratorDataWrapper<T> other)
        => EqualityComparer<T?>.Default.Equals(Data, other.Data)
        && _diags.SetEquals(other._diags);

    public override int GetHashCode()
        => Data is null
         ? _diags.GetHashCode()
         : Utils.CombineHashCodes(Data.GetHashCode(), _diags.GetHashCode());
}