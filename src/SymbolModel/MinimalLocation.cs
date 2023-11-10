namespace StarKid.Generator.SymbolModel;

public sealed class MinimalLocation(string filePath, TextSpan textSpan, LinePositionSpan lineSpan)
{
    public static readonly MinimalLocation Default
        = new("", TextSpan.FromBounds(0, 0), new(LinePosition.Zero, LinePosition.Zero));

    public string FilePath { get; } = filePath;
    public TextSpan TextSpan { get; } = textSpan;
    public LinePositionSpan LineSpan { get; } = lineSpan;


    public static implicit operator Location(MinimalLocation loc)
        => Location.Create(loc.FilePath, loc.TextSpan, loc.LineSpan);
    public static implicit operator MinimalLocation(Location loc)
        => new(loc.SourceTree?.FilePath ?? "<unknown>", loc.SourceSpan, loc.GetLineSpan().Span);

    public bool Equals(MinimalLocation? other)
        => other is not null && LineSpan == other.LineSpan && TextSpan == other.TextSpan && FilePath == other.FilePath;

    public override bool Equals(object? obj)
        => Equals(obj as MinimalLocation);

    public override string ToString()
        => "MinimalLocation { FilePath = " + FilePath + ", TextSpan = " + TextSpan + ", LineSpan = " + LineSpan + " }";

    public override int GetHashCode()
        => MiscUtils.CombineHashCodes(
            FilePath.GetHashCode(),
            MiscUtils.CombineHashCodes(
                TextSpan.GetHashCode(),
                LineSpan.GetHashCode()
            )
        );
}