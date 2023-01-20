namespace Recline.Generator;

internal record AttributeListInfo(
    bool IsOnParameter = false,
    CommandGroupAttribute? CommandGroup = null,
    CommandAttribute? Command = null,
    DescriptionAttribute? Description = null,
    OptionAttribute? Option = null,
    ParseWithAttribute? ParseWith = null,
    ValidateWithAttribute? ValidateWith = null
) {
    internal bool IsEmpty
        => CommandGroup is null
        && Command is null
        && Description is null
        && Option is null
        && ParseWith is null
        && ValidateWith is null
        ;

    internal CLIMemberKind Kind => AttributeParser.CategorizeAttributeList(this);
}