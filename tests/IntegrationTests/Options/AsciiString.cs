using System.Diagnostics.CodeAnalysis;

public readonly struct AsciiString {
    public string InternalString { get; }
    public bool IsEmpty => InternalString is null or "";
    private AsciiString(string s) => InternalString = s;
    public static AsciiString From(string s) {
        if (TryParse(s, out var res))
            return res;

        var firstNonAsciiChar = s.FirstOrDefault(c => !Char.IsAscii(c));
        throw new InvalidOperationException("Char '" + firstNonAsciiChar + "' is not an ASCII character");
    }

    public static bool TryParse(string s, [MaybeNullWhen(false)] out AsciiString ascii) {
        ascii = default;
        if (!s.All(Char.IsAscii))
            return false;
        ascii = new(s);
        return true;
    }
}