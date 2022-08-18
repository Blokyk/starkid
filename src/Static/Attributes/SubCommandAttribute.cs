#nullable enable

using System;

namespace Recline;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class SubCommandAttribute : CommandAttribute
{
    public string ParentCmdMethodName { get; }

    public bool InheritOptions { get; set; } = false;

    public SubCommandAttribute(string cmdName, string parentCmdMethodName) : base(cmdName) {
        ParentCmdMethodName = parentCmdMethodName;
    }

    public void Deconstruct(
        out string cmdName,
        out string parentCmdMethodName,
        out bool inheritOptions
    ) {
        cmdName = CmdName;
        parentCmdMethodName = ParentCmdMethodName;
        inheritOptions = InheritOptions;
    }
}