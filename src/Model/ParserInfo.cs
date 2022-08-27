namespace Recline.Generator.Model;

public abstract record ParserInfo(MinimalTypeInfo TargetType) {
    public static readonly DirectMethod AsBool = new DirectMethod(Resources.GenNamespace + "." + Resources.ProgClassName + ".AsBool", Utils.BOOLMinInfo);
    public static readonly Identity StringIdentity = new(Utils.STRMinInfo);
    public static readonly Invalid Error = new();

    public record Identity(MinimalTypeInfo TargetType) : DirectMethod("", TargetType);
    public record Constructor(MinimalTypeInfo TargetType) : ParserInfo(TargetType);
    public record DirectMethod(string FullName, MinimalTypeInfo TargetType) : ParserInfo(TargetType);
    public record BoolOutMethod(string FullName, MinimalTypeInfo TargetType) : ParserInfo(TargetType);
    public record Invalid(Microsoft.CodeAnalysis.Diagnostic? Diagnostic = null) : ParserInfo((null as MinimalTypeInfo)!);
}