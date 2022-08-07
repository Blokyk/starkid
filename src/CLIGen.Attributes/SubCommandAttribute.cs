namespace CLIGen;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class SubCommandAttribute : CommandAttribute
{
    public string ParentCmd { get; }

    public bool InheritOptions { get; set; } = false;

    public SubCommandAttribute(string cmdName, string parentCmd) : base(cmdName) {
        ParentCmd = parentCmd;
    }

    public void Deconstruct(
        out string cmdName,
        out string parentCmd,
        out bool inheritOptions
    ) {
        cmdName = CmdName;
        parentCmd = ParentCmd;
        inheritOptions = InheritOptions;
    }
}