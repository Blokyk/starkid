namespace Recline.Generator;

internal readonly struct AttributeListInfo {
    public readonly CommandGroupAttribute? CommandGroup { get; } = null;
    public readonly CommandAttribute? Command { get; } = null;
    public readonly OptionAttribute? Option { get; } = null;
    public readonly ParseWithAttribute? ParseWith { get; } = null;
    public readonly ImmutableArray<ValidateWithAttribute> ValidateWithList { get; } = ImmutableArray<ValidateWithAttribute>.Empty;
    public readonly bool IsOnParameter { get; } = false;

    public AttributeListInfo(
        CommandGroupAttribute? commandGroup,
        CommandAttribute? command,
        OptionAttribute? option,
        ParseWithAttribute? parseWith,
        ImmutableArray<ValidateWithAttribute> validateWithList,
        bool isOnParameter = false
    ) {
        CommandGroup = commandGroup;
        Command = command;
        Option = option;
        ParseWith = parseWith;
        ValidateWithList = validateWithList;
        IsOnParameter = isOnParameter;
    }

    internal bool IsEmpty
        => CommandGroup is null
        && Command is null
        && Option is null
        && ParseWith is null
        && ValidateWithList.Length == 0
        ;

    internal CLIMemberKind Kind => AttributeListBuilder.CategorizeAttributeList(this);

    public void Deconstruct(
        out CommandGroupAttribute? commandGroup,
        out CommandAttribute? command,
        out OptionAttribute? option,
        out ParseWithAttribute? parseWith,
        out ImmutableArray<ValidateWithAttribute> validateWithList,
        out bool isOnParameter
    ) {
        commandGroup = CommandGroup;
        command = Command;
        option = Option;
        parseWith = ParseWith;
        validateWithList = ValidateWithList;
        isOnParameter = IsOnParameter;
    }
}