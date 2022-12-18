namespace Recline.Generator;

internal static class Utils
{
    public static readonly Random Random = new();

    public static bool TryGetConstantValue<T>(this SemanticModel model, SyntaxNode node, out T value) {
        var opt = model.GetConstantValue(node);

        if (!opt.HasValue || opt.Value is not T tVal) {
            value = default(T)!;
            return false;
        }

        value = tVal;
        return true;
    }

    public static string GetLastNamePart(string fullStr) {
        int lastDotIdx = 0;

        for (int i = 0; i < fullStr.Length; i++) {
            if (fullStr[i] == '.' && i + 1 < fullStr.Length)
                lastDotIdx = i + 1;
        }

        return fullStr.Substring(lastDotIdx);
    }

    /*public static string GetFullNameWithNull(this MinimalTypeInfo symbol) {
        if (symbol.Name == "Nullable")

        return GetRawName(symbol) + (symbol.NullableAnnotation != NullableAnnotation.Annotated ? "" : "?");
    }*/

    public static Location GetLocation(this SyntaxReference syntaxRef)
        => Location.Create(syntaxRef.SyntaxTree, syntaxRef.Span);

    public static Location GetApplicationLocation(AttributeData attr)
        => attr.ApplicationSyntaxReference?.GetLocation() ?? Location.None;

    internal static int CombineHashCodes(int h1, int h2) =>  ((h1 << 5) + h1) ^ h2;

    internal static IEqualityComparer<ParseWithAttribute> ParseWithAttributeComparer = new ReclineAttributesComparer();
    internal static IEqualityComparer<ValidateWithAttribute> ValidateWithAttributeComparer = new ReclineAttributesComparer();

    private class ReclineAttributesComparer : IEqualityComparer<ParseWithAttribute>, IEqualityComparer<ValidateWithAttribute>
    {
        public bool Equals(ParseWithAttribute x, ParseWithAttribute y)
            => x.Equals(y);
        public int GetHashCode(ParseWithAttribute obj)
            => obj.GetHashCode();

        public bool Equals(ValidateWithAttribute x, ValidateWithAttribute y)
            => x.Equals(y);
        public int GetHashCode(ValidateWithAttribute obj)
            => obj.GetHashCode();
    }
}

internal class TupleComparer<T, U> : IEqualityComparer<Tuple<T, U>>, IEqualityComparer<ValueTuple<T, U>>
{
    private readonly IEqualityComparer<T> _tComparer;
    private readonly IEqualityComparer<U> _uComparer;

    public TupleComparer() : this(EqualityComparer<T>.Default, EqualityComparer<U>.Default) {}
    public TupleComparer(IEqualityComparer<T> tComparer, IEqualityComparer<U> uComparer) {
        _tComparer = tComparer;
        _uComparer = uComparer;
    }

    public bool Equals(Tuple<T, U> x, Tuple<T, U> y)
        => _tComparer.Equals(x.Item1, y.Item1) && _uComparer.Equals(x.Item2, y.Item2);
    public int GetHashCode(Tuple<T, U> obj)
        => Utils.CombineHashCodes(_tComparer.GetHashCode(obj.Item1), _uComparer.GetHashCode(obj.Item2));

    public bool Equals((T, U) x, (T, U) y)
        => _tComparer.Equals(x.Item1, y.Item1) && _uComparer.Equals(x.Item2, y.Item2);
    public int GetHashCode((T, U) obj)
        => Utils.CombineHashCodes(_tComparer.GetHashCode(obj.Item1), _uComparer.GetHashCode(obj.Item2));
}