#if !GEN
namespace Recline;
#endif

/// <summary>
/// Indicates the function used to validate the argument/option
/// </summary>
/// <remarks>
/// The validating function must take a single parameter with this member's type and return either <see cref="void" />, <see cref="bool" />, <see cref="int" />, <see cref="string" />? or <see cref="System.Exception?" />.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class ValidateWithAttribute : System.Attribute {
    public string ValidatorName { get; }

#if !GEN // god forbid me for my sins
    internal ITypeSymbol TypeSymbol { get; } = null!;
    internal ValidateWithAttribute(ITypeSymbol typeSymbol, string validatorMethodName) {
        ValidatorName = validatorMethodName;
        TypeSymbol = typeSymbol;
    }
#else
    public ValidateWithAttribute(string validatorMethodName)
        => ValidatorName = validatorMethodName;

    public ValidateWithAttribute(Type containingType, string validatorMethodName) : this(validatorMethodName) {}
#endif
}