namespace Recline.Generator.Model;

public sealed class MinimalLocation
{
    public string FilePath { get; }
    public TextSpan TextSpan { get; }
    public LinePositionSpan LineSpan { get; }
    public MinimalLocation(string filePath, TextSpan textSpan, LinePositionSpan lineSpan) {
        FilePath = filePath;
        TextSpan = textSpan;
        LineSpan = lineSpan;
    }

    public static implicit operator Location(MinimalLocation loc)
        => Location.Create(loc.FilePath, loc.TextSpan, loc.LineSpan);
    public static implicit operator MinimalLocation(Location loc)
        => new(loc.SourceTree!.FilePath, loc.SourceSpan, loc.GetLineSpan().Span);

    public bool Equals(MinimalLocation? other)
        => other is not null && LineSpan == other.LineSpan && TextSpan == other.TextSpan && FilePath == other.FilePath;

    public override bool Equals(object? obj)
        => Equals(obj as MinimalLocation);

    public override int GetHashCode()
        => Utils.CombineHashCodes(
            FilePath.GetHashCode(),
            Utils.CombineHashCodes(
                TextSpan.GetHashCode(),
                LineSpan.GetHashCode()
            )
        );
}