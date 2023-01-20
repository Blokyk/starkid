#if !GEN
namespace Recline;
#endif

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class SubCommandAttribute : CommandAttribute
{
    public string ParentCmd { get; }

    public SubCommandAttribute(string cmdName, string parentCmdName) : base(cmdName)
        => ParentCmd = parentCmdName;

    public void Deconstruct(
        out string cmdName,
        out string parentCmdName
    ) {
        cmdName = CmdName;
        parentCmdName = ParentCmd;
    }
}