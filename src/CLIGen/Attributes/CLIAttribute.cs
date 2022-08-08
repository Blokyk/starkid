#nullable enable

using System;

namespace CLIGen;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CLIAttribute : CommandAttribute
{
    public string? EntryPoint { get; set; } = null;

    public CLIAttribute(string cmdName) : base(cmdName) {}

    public void Deconstruct(
        out string cmdName,
        out string? entryPoint
    ) {
        cmdName = CmdName;
        entryPoint = EntryPoint;
    }
}