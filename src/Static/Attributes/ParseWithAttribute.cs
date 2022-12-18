#if !GEN
namespace Recline;
#endif

/// <summary>
/// Indicates the function used to convert the raw string argument into the desired type
/// </summary>
/// <remarks>
/// The parsing function must take a single string parameter and return either <see cref="void" />, <see cref="System.Boolean" />, <see cref="System.Int32" />, <see cref="System.String" />? or <see cref="System.Exception" />?.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class ParseWithAttribute : System.Attribute, IEquatable<ParseWithAttribute> {
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
}