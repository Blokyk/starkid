#nullable enable

using System;

namespace Recline;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class CommandAttribute : System.Attribute
{
    public string CmdName { get; }

    public CommandAttribute(string cmdName) => CmdName = cmdName;

    public void Deconstruct(
        out string cmdName
    ) {
        cmdName = CmdName;
    }
}