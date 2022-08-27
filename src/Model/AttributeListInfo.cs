namespace Recline.Generator;

internal record AttributeListInfo(
    CLIAttribute? CLI = null,
    CommandAttribute? Command = null,
    DescriptionAttribute? Description = null,
    OptionAttribute? Option = null,
    ParseWithAttribute? ParseWith = null,
    SubCommandAttribute? SubCommand = null,
    ValidateWithAttribute? ValidateWith = null
) {
    internal bool IsEmpty
        => CLI is null
        && Command is null
        && Description is null
        && Option is null
        && ParseWith is null
        && SubCommand is null
        && ValidateWith is null
        ;

    internal MemberKind Kind => AttributeParser.CategorizeAttributeList(this);
}