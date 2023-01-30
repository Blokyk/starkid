#if !GEN
namespace Recline;
#endif

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class CommandGroupAttribute : System.Attribute
{
    public string GroupName { get; }
    public string? DefaultCmdName { get; set; }

    public CommandGroupAttribute(string groupName)
        => GroupName = groupName;

    public void Deconstruct(
        out string groupName,
        out string? defaultCmd
    ) {
        groupName = GroupName;
        defaultCmd = DefaultCmdName;
    }
}