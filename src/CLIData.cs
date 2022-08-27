using Recline.Generator.Model;

namespace Recline.Generator;

public record CLIData(
    string AppName,
    string FullClassName,
    ImmutableArray<string> Usings,
    (Command cmd, ImmutableArray<Argument> args)? CmdAndArgs,
    ImmutableArray<Option> OptsAndSws,
    string? Description,
    ImmutableArray<Command> Cmds,
    int HelpExitCode
);