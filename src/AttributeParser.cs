namespace Recline.Generator;

internal class AttributeParser
{
    private readonly Action<Diagnostic> _addDiagnostic;
    public AttributeParser(Action<Diagnostic> addDiagnostic)
        => _addDiagnostic = addDiagnostic;

    public bool TryParseCmdAttrib(AttributeData attr, [NotNullWhen(true)] out CommandAttribute? cmdAttr) {
        cmdAttr = null;

        // cmdName
        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var cmdName))
            return false;

        // ShortDesc
        if (!TryGetProp<string?>(attr, nameof(CommandAttribute.ShortDesc), SpecialType.System_String, null, out var shortDesc))
            return false;

        cmdAttr = new(cmdName) {
            ShortDesc = shortDesc
        };

        return true;
    }

    public bool TryParseGroupAttrib(AttributeData attr, [NotNullWhen(true)] out CommandGroupAttribute? groupAttr) {
        groupAttr = null;

        // groupName
        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var appName))
            return false;

        // DefaultCommandName
        if (!TryGetProp<string?>(attr, nameof(CommandGroupAttribute.DefaultCmdName), SpecialType.System_String, null, out var defaultCmdName))
            return false;

        // ShortDesc
        if (!TryGetProp<string?>(attr, nameof(CommandGroupAttribute.ShortDesc), SpecialType.System_String, null, out var shortDesc))
            return false;

        groupAttr = new(appName) {
            DefaultCmdName = defaultCmdName,
            ShortDesc = shortDesc
        };

        return true;
    }

    public bool TryParseOptAttrib(AttributeData attr, [NotNullWhen(true)] out OptionAttribute? optAttr) {
        optAttr = null;

        char shortName = '\0';

        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var longName))
            return false;

        if (attr.ConstructorArguments.Length == 2) {
            if (!TryGetCtorArg<char>(attr, 1, SpecialType.System_Char, out shortName))
                return false;
        }

        if (!TryGetProp<string?>(attr, nameof(OptionAttribute.ArgName), SpecialType.System_String, null, out var argName))
            return false;

        if (!TryGetProp<bool>(attr, nameof(OptionAttribute.IsGlobal), SpecialType.System_Boolean, false, out var isGlobal))
            return false;

        optAttr = new OptionAttribute(
            longName,
            shortName
        ) {
            ArgName = argName,
            IsGlobal = isGlobal,
        };

        return true;
    }

    private bool TryGetNameOfArg(ExpressionSyntax expr, [NotNullWhen(true)] out ExpressionSyntax? nameExpr) {
        nameExpr = null;

        if (expr is not InvocationExpressionSyntax methodCallSyntax)
            return false;

        if (methodCallSyntax.Expression is not IdentifierNameSyntax nameofSyntax)
            return false;

        // no, IsKind or IsContextualKeyword don't work. don't ask me why
        // technically, the proper way to do this seems to be:
        //     SyntaxFacts.GetContextualKeywordKind(nameofSyntax.Identifier.ValueText)
        //  == SyntaxKind.NameOfKeyword
        if (nameofSyntax.Identifier.Text != "nameof")
            return false;

        if (methodCallSyntax.ArgumentList.Arguments.Count != 1)
            return false;

        var argExpr = methodCallSyntax.ArgumentList.Arguments[0].Expression;

        nameExpr = argExpr switch {
            MemberAccessExpressionSyntax maes => maes,
            NameSyntax name => name,
            _ => null
        };

        return nameExpr is not null;
    }

    public bool TryParseParseAttrib(AttributeData attr, [NotNullWhen(true)] out ParseWithAttribute? parseWithAttr) {
        parseWithAttr = null;

        var attrSyntax = (attr.ApplicationSyntaxReference!.GetSyntax() as AttributeSyntax)!;

        var argList = attrSyntax.ArgumentList!.Arguments;

        if (argList.Count != 1)
            return false;

        if (!TryGetNameOfArg(argList[0].Expression, out var parserName)) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.ParseWithMustBeNameOfExpr,
                    Utils.GetApplicationLocation(attr),
                    argList[0].Expression
                )
            );

            return false;
        }

        parseWithAttr = new ParseWithAttribute(parserName.GetReference(), parserName.ToString());

        return true;
    }

    public bool TryParseValidateAttrib(AttributeData attr, [NotNullWhen(true)] out ValidateWithAttribute? ValidateWithAttr) {
        ValidateWithAttr = null;

        var attrSyntax = (attr.ApplicationSyntaxReference!.GetSyntax() as AttributeSyntax)!;

        var argList = attrSyntax.ArgumentList!.Arguments;

        if (argList.Count is 0 or > 2)
            return false;

        if (!TryGetNameOfArg(argList[0].Expression, out var validatorName)) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.ValidateWithMustBeNameOfExpr,
                    Utils.GetApplicationLocation(attr),
                    argList[0].Expression
                )
            );

            return false;
        }

        string? msg = null;

        if (argList.Count == 2) {
            if (!TryGetCtorArg<string>(attr, 1, SpecialType.System_String, out msg))
                return false;
        }

        ValidateWithAttr
            = new ValidateWithAttribute(
                validatorName.GetReference(),
                validatorName.ToString()
            ) { ErrorMessage = msg };

        return true;
    }

    public bool TryGetCtorArg<T>(AttributeData attrib, int ctorIdx, SpecialType type, [NotNullWhen(true)] out T? val) {
        val = default;

        var ctorArgs = attrib.ConstructorArguments;

        if (ctorArgs.Length < ctorIdx + 1) {
            return false;
        }

        if (ctorArgs[ctorIdx].Type?.SpecialType != type)
            return false;

        val = (T)ctorArgs[ctorIdx].Value!;

        return true;
    }

    public bool TryGetProp<T>(AttributeData attrib, string propName, SpecialType type, T defaultVal, out T val) {
        val = defaultVal;

        var namedArgs = attrib.NamedArguments;

        if (namedArgs.IsDefaultOrEmpty)
            return true;

        var arg = namedArgs.FirstOrDefault(
            kv => kv.Key == propName
        ).Value;

        if (arg.Equals(default))
            return true;

        if (arg.Type?.SpecialType != type)
            return false;

        val = (T)arg.Value!;

        return true;
    }
}