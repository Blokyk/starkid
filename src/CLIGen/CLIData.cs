using CLIGen.Generator.Model;

namespace CLIGen.Generator;

public record CLIData(
    string AppName,
    string FullCLassName,
    string[] Usings,
    (Command cmd, Argument[] args)? CmdAndArgs,
    Option[] OptsAndSws,
    string? Description,
    Command[] Cmds,
    int HelpExitCode
);