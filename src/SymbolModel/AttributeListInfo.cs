namespace StarKid.Generator.SymbolModel;

internal readonly record struct AttributeListInfo(
    CommandGroupAttribute? CommandGroup,
    CommandAttribute? Command,
    OptionAttribute? Option,
    ParseWithAttribute? ParseWith,
    ImmutableValueArray<ValidateWithAttribute> ValidateWithList,
    bool IsOnParameter
) {
    internal bool IsEmpty
        => CommandGroup is null
        && Command is null
        && Option is null
        && ParseWith is null
        && ValidateWithList.Length == 0
        ;

    internal CLIMemberKind Kind => AttributeListBuilder.CategorizeAttributeList(this);
}