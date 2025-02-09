using StarKid.Generator.SymbolModel;

namespace StarKid.Generator.CommandModel;

public abstract record ValidatorInfo() {
    public string? Message { get; init; }

    public bool IsElementWiseValidator { get; init; }

    public abstract record Method(string FullName, MinimalMethodInfo MethodInfo) : ValidatorInfo {
        public record Bool(MinimalMethodInfo MethodInfo) : Method(MethodInfo.ToString(), MethodInfo);
        public record Exception(MinimalMethodInfo MethodInfo) : Method(MethodInfo.ToString(), MethodInfo);
        // public record String(string FullName, MinimalMethodInfo MethodInfo) : Method(FullName, MethodInfo);
    }

    public record Property(string PropertyName, MinimalMemberInfo PropertyInfo) : ValidatorInfo;

    public record Invalid(DiagnosticDescriptor Descriptor, params object[] MessageArgs) : ValidatorInfo;
}