namespace Recline
{
    /// <summary>
    /// Indicates the function used to convert the raw string argument into the desired type
    /// </summary>
    /// <remarks>
    /// The parsing function must take a single string parameter, and returns either the target type, or any of: <see cref="void" />, <see cref="System.Boolean" />, <see cref="System.Int32" />, <see cref="System.String" />? or <see cref="System.Exception" />?.
    /// </remarks>
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class ParseWithAttribute : System.Attribute, System.IEquatable<ParseWithAttribute>
    {
        public string ParserName { get; }

#if !GEN
        public SyntaxReference ParserNameSyntaxRef { get; }

        public ParseWithAttribute(SyntaxReference parserNameRef, string parserName) {
            ParserNameSyntaxRef = parserNameRef;
            ParserName = parserName;
        }
#else
        public ParseWithAttribute(string nameofParsingMethod)
            => ParserName = nameofParsingMethod;
#endif

        public bool Equals(ParseWithAttribute? other)
            => ParserName == other?.ParserName;
        public override int GetHashCode()
            => ParserName.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as ParseWithAttribute);
    }
}