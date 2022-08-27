#if !GEN
namespace Recline;
#endif

/// <summary>
/// Indicates the function used to convert the raw string argument into the desired type
/// </summary>
/// <remarks>
/// The parsing function must take a single string parameter and return either <see cref="void" />, <see cref="bool" />, <see cref="int" />, <see cref="string" />? or <see cref="System.Exception?" />.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class ParseWithAttribute : System.Attribute
{
    public string ParserName { get; }

#if !GEN // god forbid me for my sins
    internal ITypeSymbol TypeSymbol { get; } = null!;
    internal ParseWithAttribute(ITypeSymbol typeSymbol, string parsingMethodName) {
        ParserName = parsingMethodName;
        TypeSymbol = typeSymbol;
    }
#else
    public ParseWithAttribute(string parsingMethodName)
        => ParserName = parsingMethodName;

    public ParseWithAttribute(Type containingType, string parsingMethodName) : this(parsingMethodName) {}
#endif
}