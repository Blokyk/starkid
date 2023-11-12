namespace StarKid.Tests;

public static class Utils
{
    public static bool Contains(this Location a, Location b)
        => a.SourceTree == b.SourceTree
        && a.SourceSpan.Contains(b.SourceSpan);
}