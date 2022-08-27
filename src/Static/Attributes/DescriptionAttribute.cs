#if !GEN
namespace Recline;
#endif

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DescriptionAttribute : System.Attribute
{
    public string Description { get; }
    public DescriptionAttribute(string desc) => Description = desc;

    public void Deconstruct(
        out string desc
    ) {
        desc = Description;
    }
}