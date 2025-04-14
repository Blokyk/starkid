namespace StarKid.Generator.AttributeModel;

internal class AttributeParser(Action<Diagnostic> addDiagnostic)
{
    public bool TryParseCmdAttrib(AttributeData attr, [NotNullWhen(true)] out CommandAttribute? cmdAttr) {
        cmdAttr = null;

        // cmdName
        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var cmdName))
            return false;

        // ShortDesc
        if (!TryGetProp<string?>(attr, "ShortDesc", SpecialType.System_String, null, out var shortDesc))
            return false;

        cmdAttr = new(cmdName, shortDesc);

        return true;
    }

    public bool TryParseGroupAttrib(AttributeData attr, [NotNullWhen(true)] out CommandGroupAttribute? groupAttr) {
        groupAttr = null;

        // groupName
        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var groupName))
            return false;

        // DefaultCommandName
        if (!TryGetProp<string?>(attr, "DefaultCmdName", SpecialType.System_String, null, out var defaultCmdName))
            return false;

        // ShortDesc
        if (!TryGetProp<string?>(attr, "ShortDesc", SpecialType.System_String, null, out var shortDesc))
            return false;

        groupAttr = new(groupName, defaultCmdName, shortDesc);

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

        if (!TryGetProp<string?>(attr, "ArgName", SpecialType.System_String, null, out var argName))
            return false;

        if (!TryGetProp<bool>(attr, "IsGlobal", SpecialType.System_Boolean, false, out var isGlobal))
            return false;

        optAttr = new OptionAttribute(longName, shortName, argName, isGlobal);

        return true;
    }

    public bool TryParseParseAttrib(AttributeData attr, [NotNullWhen(true)] out ParseWithAttribute? parseWithAttr) {
        parseWithAttr = null;

        var attrSyntax = attr.ApplicationSyntaxReference?.GetSyntax();
        if (attrSyntax is not AttributeSyntax { ArgumentList.Arguments: { Count: 1 } argList })
            return false;

        if (!TryGetNameOfArg(argList[0].Expression, out var parserName)) {
            addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.ParseWithMustBeNameOfExpr,
                    SyntaxUtils.GetApplicationLocation(attr),
                    argList[0].Expression
                )
            );

            return false;
        }

        parseWithAttr = new ParseWithAttribute(parserName);

        return true;
    }

    public bool TryParseValidateAttrib(AttributeData attr, [NotNullWhen(true)] out ValidateWithAttribute? ValidateWithAttr) {
        ValidateWithAttr = null;

        var attrSyntax = attr.ApplicationSyntaxReference?.GetSyntax();
        if (attrSyntax is not AttributeSyntax { ArgumentList.Arguments: { Count: 1 or 2 } argList })
            return false;

        if (!TryGetNameOfArg(argList[0].Expression, out var validatorName)) {
            addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.ValidateWithMustBeNameOfExpr,
                    SyntaxUtils.GetApplicationLocation(attr),
                    argList[0].Expression
                )
            );

            return false;
        }

        if (!TryGetProp<string?>(attr, nameof(ValidateWithAttribute.ErrorMessage), SpecialType.System_String, null, out var msg))
            return false;

        ValidateWithAttr = new ValidateWithAttribute(validatorName, msg);

        return true;
    }

    private bool TryGetNameOfArg(ExpressionSyntax expr, [NotNullWhen(true)] out ExpressionSyntax? nameExpr) {
        nameExpr = null;

        if (expr is not InvocationExpressionSyntax { ArgumentList.Arguments: [ var arg ] } methodCallSyntax)
            return false;

        // technically, the proper way to do this is
        //     SyntaxFacts.GetContextualKeywordKind(nameofSyntax.Identifier.ValueText)
        //  == SyntaxKind.NameOfKeyword
        if (methodCallSyntax.Expression is not IdentifierNameSyntax { Identifier.Text: "nameof" } nameofSyntax)
            return false;

        nameExpr = arg?.Expression switch {
            MemberAccessExpressionSyntax maes => maes,
            NameSyntax name => name,
            _ => null
        };

        return nameExpr is not null;
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