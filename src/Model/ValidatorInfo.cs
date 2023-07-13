namespace Recline.Generator.Model;

public abstract record ValidatorInfo() {
    public string? Message { get; init; }

    public abstract record Method(string FullName, MinimalMethodInfo MethodInfo) : ValidatorInfo {
        public record Bool(string FullName, MinimalMethodInfo MethodInfo) : Method(FullName, MethodInfo);
        public record Exception(string FullName, MinimalMethodInfo MethodInfo) : Method(FullName, MethodInfo);
        // public record String(string FullName, MinimalMethodInfo MethodInfo) : Method(FullName, MethodInfo);
    }

    public record Property(string PropertyName, MinimalMemberInfo PropertyInfo) : ValidatorInfo;

    public record Invalid(Diagnostic? Diagnostic = null) : ValidatorInfo;
}