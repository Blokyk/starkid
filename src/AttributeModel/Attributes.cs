namespace StarKid.Generator.AttributeModel;

public sealed record CommandGroupAttribute(
    string GroupName,
    string? DefaultCmdName,
    string? ShortDesc
);

public sealed record CommandAttribute(
    string CommandName,
    string? ShortDesc
);

public sealed record OptionAttribute(
    string LongName,
    char Alias,
    string? ArgName,
    bool IsGlobal
);

public sealed record ParseWithAttribute(
    ExpressionSyntax ParserNameExpr
) {
    public bool Equals(ParseWithAttribute? other)
        => other is not null && ParserNameExpr.IsEquivalentTo(other.ParserNameExpr);
    public override int GetHashCode() => SyntaxUtils.GetHashCode(ParserNameExpr);
}

public sealed record ValidateWithAttribute(
    ExpressionSyntax ValidatorNameExpr,
    string? ErrorMessage
) {
    public bool Equals(ValidateWithAttribute? other)
        => other is not null
        && ErrorMessage == other.ErrorMessage
        && ValidatorNameExpr.IsEquivalentTo(other.ValidatorNameExpr);
    public override int GetHashCode()
        => ErrorMessage is null
            ? SyntaxUtils.GetHashCode(ValidatorNameExpr)
            : Polyfills.CombineHashCodes(ErrorMessage.GetHashCode(), SyntaxUtils.GetHashCode(ValidatorNameExpr));
}

public sealed record ValidatePropAttribute(
    ExpressionSyntax PropertyNameExpr,
    bool ExpectedValue,
    string? ErrorMessage
) {
    public bool Equals(ValidatePropAttribute? other)
        => other is not null
        && ErrorMessage == other.ErrorMessage
        && ExpectedValue == other.ExpectedValue
        && PropertyNameExpr.IsEquivalentTo(other.PropertyNameExpr);
    public override int GetHashCode()
        => Polyfills.CombineHashCodes(
            SyntaxUtils.GetHashCode(PropertyNameExpr),
            Polyfills.CombineHashCodes(
                ExpectedValue.GetHashCode(),
                ErrorMessage?.GetHashCode() ?? 0
            )
        );
}