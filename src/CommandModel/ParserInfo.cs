using StarKid.Generator.SymbolModel;

namespace StarKid.Generator.CommandModel;

public abstract record ParserInfo
{
    public abstract MinimalTypeInfo TargetType { get; init; }

    public sealed record AsBool : DirectMethod
    {
        public static readonly AsBool Instance = new();
        public new const string FullName = "StarKid.Generated.StarKidProgram.AsBool";
        private AsBool() : base(FullName, CommonTypes.BOOL) { }
    }

    public sealed record StringIdentity : Identity
    {
        public static readonly StringIdentity Instance = new();
        private StringIdentity() : base(CommonTypes.STR) { }
    }

    public sealed record BoolOutMethod(string FullName, MinimalTypeInfo TargetType) : ParserInfo;
    public sealed record Constructor(MinimalTypeInfo TargetType) : ParserInfo;
    public record DirectMethod(string FullName, MinimalTypeInfo TargetType) : ParserInfo;
    public record Identity(MinimalTypeInfo TargetType) : ParserInfo;

    public sealed record Invalid(DiagnosticDescriptor Descriptor, params object[] MessageArgs) : ParserInfo
    {
        public override MinimalTypeInfo TargetType {
            get => throw new InvalidOperationException("Trying to get the target type of an invalid ParserInfo!");
            init => throw new InvalidOperationException("Trying to get the target type of an invalid ParserInfo!");
        }
    }
}