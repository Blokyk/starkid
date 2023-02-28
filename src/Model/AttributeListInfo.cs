namespace Recline.Generator;

internal readonly struct AttributeListInfo {
    public readonly CommandGroupAttribute? CommandGroup { get; } = null;
    public readonly CommandAttribute? Command { get; } = null;
    public readonly OptionAttribute? Option { get; } = null;
    public readonly ParseWithAttribute? ParseWith { get; } = null;
    public readonly ValidateWithAttribute? ValidateWith { get; } = null;
    public readonly bool IsOnParameter { get; } = false;

    public AttributeListInfo(
        CommandGroupAttribute? commandGroup,
        CommandAttribute? command,
        OptionAttribute? option,
        ParseWithAttribute? parseWith,
        ValidateWithAttribute? validateWith,
        bool isOnParameter = false
    ) {
        CommandGroup = commandGroup;
        Command = command;
        Option = option;
        ParseWith = parseWith;
        ValidateWith = validateWith;
        IsOnParameter = isOnParameter;
    }

    internal bool IsEmpty
        => CommandGroup is null
        && Command is null
        && Option is null
        && ParseWith is null
        && ValidateWith is null
        ;

    internal CLIMemberKind Kind => AttributeListBuilder.CategorizeAttributeList(this);

    public void Deconstruct(
        out CommandGroupAttribute? commandGroup,
        out CommandAttribute? command,
        out OptionAttribute? option,
        out ParseWithAttribute? parseWith,
        out ValidateWithAttribute? validateWith,
        out bool isOnParameter
    ) {
        commandGroup = CommandGroup;
        command = Command;
        option = Option;
        parseWith = ParseWith;
        validateWith = ValidateWith;
        isOnParameter = IsOnParameter;
    }
}