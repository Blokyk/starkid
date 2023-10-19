using StarKid.Generator.SymbolModel;

namespace StarKid.Generator.CommandModel;

public abstract record ParserInfo {
    public static readonly DirectMethod AsBool = new("StarKid.Generated.StarKidProgram.AsBool", CommonTypes.BOOL);
    public static readonly Identity StringIdentity = new(CommonTypes.STR);

    public abstract MinimalTypeInfo TargetType { get; init; }

    public sealed record Identity(MinimalTypeInfo TargetType) : ParserInfo;
    public sealed record Constructor(MinimalTypeInfo TargetType) : ParserInfo;
    public sealed record DirectMethod(string FullName, MinimalTypeInfo TargetType) : ParserInfo;
    public sealed record BoolOutMethod(string FullName, MinimalTypeInfo TargetType) : ParserInfo;

    public sealed record Invalid(DiagnosticDescriptor Descriptor, params object[] MessageArgs) : ParserInfo {
        public override MinimalTypeInfo TargetType {
            get => throw new InvalidOperationException("Trying to get the target type of an invalid ParserInfo!");
            init => throw new InvalidOperationException("Trying to get the target type of an invalid ParserInfo!");
        }
    }
}