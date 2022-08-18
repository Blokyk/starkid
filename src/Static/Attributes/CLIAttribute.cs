#nullable enable

using System;

namespace Recline;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CLIAttribute : CommandAttribute
{
    public string? EntryPoint { get; set; } = null;

    public int HelpExitCode { get; set; } = 0;

    public CLIAttribute(string cmdName) : base(cmdName) {}

    public void Deconstruct(
        out string cmdName,
        out string? entryPoint,
        out int helpExitCode
    ) {
        cmdName = CmdName;
        entryPoint = EntryPoint;
        helpExitCode = HelpExitCode;
    }
}