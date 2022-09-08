#if !GEN
namespace Recline;
#endif

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class OptionAttribute : System.Attribute
{
    public string LongName { get; }
    public char Alias { get; }

    public string? ArgName { get; set; }

    public OptionAttribute(string longName, char shortName = '\0') {
        LongName = longName;
        Alias = shortName;
    }

    public void Deconstruct(
        out string longName,
        out char alias,
        out string? argName
    ) {
        longName = LongName;
        alias = Alias;
        argName = ArgName;
    }
}