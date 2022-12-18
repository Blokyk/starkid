#if !GEN
namespace Recline;
#endif

/// <summary>
/// Indicates the function used to validate the argument/option
/// </summary>
/// <remarks>
/// The validating function must take a single parameter with this member's type and return <see cref="System.Boolean"/> or <see cref="System.Exception"/> />.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class ValidateWithAttribute : System.Attribute, IEquatable<ValidateWithAttribute> {
    public string ValidatorName { get; }
    public string? ErrorMessage { get; set; }

#if !GEN
    public SyntaxReference ValidatorNameSyntaxRef { get; }

    public ValidateWithAttribute(SyntaxReference validatorNameRef, string validatorName) {
        ValidatorNameSyntaxRef = validatorNameRef;
        ValidatorName = validatorName;
    }
#else
    public ValidateWithAttribute(string nameofValidatorMethod)
        => ValidatorName = nameofValidatorMethod;

    public ValidateWithAttribute(string nameofValidatorMethod, string errorMessage)
        : this(nameofValidatorMethod)
        => ErrorMessage = errorMessage;
#endif

    public bool Equals(ValidateWithAttribute? other)
        => ValidatorName == other?.ValidatorName
        && ErrorMessage  == other?.ErrorMessage;
    public override int GetHashCode() {
        var hash = 1009;

        unchecked {
            hash = (hash * 9176) + ValidatorName.GetHashCode();
            hash = (hash * 9176) + (ErrorMessage?.GetHashCode() ?? 0);
        }

        return hash;
    }
}