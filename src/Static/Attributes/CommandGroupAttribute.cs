#if !GEN
namespace Recline;
#endif

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class CommandGroupAttribute : System.Attribute
{
    public string CmdName { get; }
    public string? DefaultCmdName { get; set; }

    public CommandGroupAttribute(string cmdName)
        => CmdName = cmdName;

    public void Deconstruct(
        out string cmdName,
        out string? defaultCmd
    ) {
        cmdName = CmdName;
        defaultCmd = DefaultCmdName;
    }
}