namespace Recline.Generator;

internal enum MemberKind {
    None,
    CLI,
    Option,
    Command,
    Invalid,
    Useless,
}

internal class AttributeParser
{
    private readonly SemanticModel _model;
    private readonly Cache<ISymbol, ImmutableArray<Diagnostic>.Builder, (bool, AttributeListInfo)> _attrListCache;

    public AttributeParser(SemanticModel model) {
        _model = model;

        _attrListCache
            = new(
                SymbolEqualityComparer.Default,
                EqualityComparer<ImmutableArray<Diagnostic>.Builder>.Default,
                TryGetAttributeList
            );
    }

    static MemberKind ValidateAttributeListAndGetKind(AttributeListInfo attrList, ISymbol symbol, ref ImmutableArray<Diagnostic>.Builder diagnostics) {
        var kind = CategorizeAttributeList(attrList);

        if (kind is MemberKind.Useless && symbol is not IParameterSymbol) {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.OnlyDescAttr,
                    symbol.GetDefaultLocation()
                )
            );
            return MemberKind.Useless;
        }

        if (kind is not MemberKind.Invalid)
            return kind;

        // we don't care about cli because it will never be with the other attributes for a valid symbol
        var (_, cmd, _, opt, parseWith, subCmd, validateWith) = attrList;

        switch (cmd, opt, subCmd) {
            case (not null, not null, null):
            case (null, not null, not null):
            case (not null, not null, not null):
                diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.BothOptAndCmd,
                        symbol.GetDefaultLocation(),
                        symbol.GetErrorName()
                    )
                );
                break;
            case (not null, null, not null):
                diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.BothCmdAndSubCmd,
                        symbol.GetDefaultLocation(),
                        symbol.GetErrorName()
                    )
                );
                break;
            default:
                if (parseWith is not null) {
                    diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.ParseOnNonOptOrArg,
                            symbol.GetDefaultLocation(),
                            symbol.GetErrorName()
                        )
                    );
                }

                if (validateWith is not null) {
                    diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.ValidateOnNonOptOrArg,
                            symbol.GetDefaultLocation(),
                            symbol.GetErrorName()
                        )
                    );
                }

                break;
        }

        return MemberKind.Invalid;
    }

    public static MemberKind CategorizeAttributeList(AttributeListInfo attrList) {
        var (cli, cmd, desc, opt, parse, subCmd, valid) = attrList;

        return (cli, cmd, subCmd, opt, parse, valid, desc) switch {
            (    null,     null,     null,     null,     null,     null,     null) => MemberKind.None,
            (not null,     null,     null,     null,     null,     null,        _) => MemberKind.CLI,
            (    null, not null,     null,     null,     null,     null,        _) or
            (    null,     null, not null,     null,     null,     null,        _) => MemberKind.Command,
            (    null,     null,     null, not null,        _,        _,        _) => MemberKind.Option,
            (    null,     null,     null,     null,     null,     null, not null) => MemberKind.Useless,
            _ => MemberKind.Invalid,
        };
    }

    public bool TryGetAttributeList(ISymbol symbol, ref ImmutableArray<Diagnostic>.Builder diagnostics, out AttributeListInfo attrList) {
        (var isValid, attrList) = _attrListCache.GetValue(symbol, diagnostics);

        ValidateAttributeListAndGetKind(attrList, symbol, ref diagnostics);

        return isValid;
    }

    (bool, AttributeListInfo) TryGetAttributeList(ISymbol symbol, ImmutableArray<Diagnostic>.Builder diagnostics) {
        var attribList = new AttributeListInfo();
        var attrs = symbol.GetAttributes();

        CLIAttribute? cli = null;
        CommandAttribute? cmd = null;
        DescriptionAttribute? desc = null;
        OptionAttribute? opt = null;
        ParseWithAttribute? parseWith = null;
        SubCommandAttribute? subCmd = null;
        ValidateWithAttribute? validateWith = null;

        bool isValid = true;

        (bool, AttributeListInfo) error() => (false, attribList);

        foreach (var attr in attrs) {
            switch (attr.AttributeClass?.Name) {
                case Resources.CLIAttribName:
                    if (!TryParseCLIAttrib(attr, out cli))
                        return error();
                    break;
                case Resources.CmdAttribName:
                    if (!TryParseCmdAttrib(attr, out cmd))
                        return error();

                    if (String.IsNullOrWhiteSpace(cmd.CmdName)) {
                        diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.EmptyCmdName,
                                Utils.GetApplicationLocation(attr)
                            )
                        );

                        isValid = false;
                    }

                    break;
                case Resources.DescAttribName:
                    if (!TryParseDescAttrib(attr, out desc))
                        return error();

                    if (String.IsNullOrWhiteSpace(desc.Description)) {
                        diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.DescCantBeNull,
                                symbol.Locations[0],
                                symbol.GetErrorName()
                            )
                        );
                    }

                    break;
                case Resources.OptAttribName:
                    if (!TryParseOptAttrib(attr, out opt))
                        return error();

                    if (String.IsNullOrWhiteSpace(opt.LongName)) {
                        diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.EmptyOptLongName,
                                Utils.GetApplicationLocation(attr)
                            )
                        );

                        isValid = false;
                    }

                    if (Char.IsWhiteSpace(opt.Alias)) { // '\0' is not whitespace :P
                        diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.EmptyOptShortName,
                                Utils.GetApplicationLocation(attr)
                            )
                        );

                        isValid = false;
                    }

                    break;
                case Resources.ParseWithAttribName:
                    if (!TryParseParseAttrib(attr, diagnostics, out parseWith))
                        return error();
                    break;
                case Resources.SubCmdAttribName:
                    if (!TryParseSubCmdAttrib(attr, out subCmd))
                        return error();

                    if (String.IsNullOrWhiteSpace(subCmd.CmdName)) {
                        diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.EmptyCmdName,
                                Utils.GetApplicationLocation(attr)
                            )
                        );

                        isValid = false;
                    }

                    if (String.IsNullOrWhiteSpace(subCmd.ParentCmd)) {
                        diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.EmptyCmdName,
                                Utils.GetApplicationLocation(attr)
                            )
                        );

                        isValid = false;
                    }

                    break;
                case Resources.ValidateWithAttribName:
                    if (!TryParseValidateAttrib(attr, diagnostics, out validateWith))
                        return error();
                    break;
                default:
                    continue;
            }
        }

        attribList = new(cli, cmd, desc, opt, parseWith, subCmd, validateWith);

        return (isValid, attribList);
    }

    public bool TryParseCmdAttrib(AttributeData attr, [NotNullWhen(true)] out CommandAttribute? cmdAttr) {
        cmdAttr = null;

        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var cmdName))
            return false;

        cmdAttr = new(cmdName);
        return true;
    }

    public bool TryParseSubCmdAttrib(AttributeData attr, [NotNullWhen(true)] out SubCommandAttribute? subCmdAttr) {
        subCmdAttr = null;

        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var cmdName))
            return false;

        if (!TryGetCtorArg<string>(attr, 1, SpecialType.System_String, out var parentName))
            return false;

        /*bool inheritOptions = false;

        if (attr.NamedArguments.Length != 0) {
            var inheritOptionsArgPair = attr.NamedArguments.First();

            if (!inheritOptionsArgPair.Equals(default)) {
                if (!SymbolUtils.Equals(inheritOptionsArgPair.Value.Type, CommonTypes.BOOL))
                    return false;

                inheritOptions = (bool)inheritOptionsArgPair.Value.Value!;
            }
        }*/

        subCmdAttr = new(cmdName, parentName);

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

        optAttr = new OptionAttribute(
            longName,
            shortName
        ) {
            ArgName = argName
        };

        return true;
    }

    public bool TryParseDescAttrib(AttributeData attr, [NotNullWhen(true)] out DescriptionAttribute? descAttr) {
        descAttr = null;

        if (attr.ConstructorArguments.Length < 1)
            return false;

        if (attr.ConstructorArguments[0].Type?.SpecialType != SpecialType.System_String)
            return false;

        descAttr = new((string)attr.ConstructorArguments[0].Value!);

        return true;
    }

    public bool TryParseCLIAttrib(AttributeData attr, [NotNullWhen(true)] out CLIAttribute? cliAttr) {
        cliAttr = null;

        // appName
        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var appName))
            return false;

        // EntryPoint
        if (!TryGetProp<string?>(attr, nameof(CLIAttribute.EntryPoint), SpecialType.System_String, null, out var entryPoint))
            return false;

        if (!TryGetProp<int>(attr, nameof(CLIAttribute.HelpExitCode), SpecialType.System_Int32, 0, out var helpIsError))
            return false;

        cliAttr = new(appName) {
            EntryPoint = entryPoint,
            HelpExitCode = helpIsError
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

    public bool TryParseParseAttrib(AttributeData attr, ImmutableArray<Diagnostic>.Builder diags, [NotNullWhen(true)] out ParseWithAttribute? parseWithAttr) {
        parseWithAttr = null;

        var attrSyntax = (attr.ApplicationSyntaxReference!.GetSyntax() as AttributeSyntax)!;

        var argList = attrSyntax.ArgumentList!.Arguments;

        if (argList.Count != 1)
            return false;

        if (!TryGetNameOfArg(argList[0].Expression, out var parserName)) {
            diags.Add(
                Diagnostic.Create(
                    Diagnostics.ParseWithMustBeNameOfExpr,
                    Utils.GetApplicationLocation(attr),
                    argList[0].Expression.ToString()
                )
            );

            return false;
        }

        parseWithAttr = new ParseWithAttribute(parserName.GetReference(), parserName.ToString());

        return true;
    }

    public bool TryParseValidateAttrib(AttributeData attr, ImmutableArray<Diagnostic>.Builder diags, [NotNullWhen(true)] out ValidateWithAttribute? ValidateWithAttr) {
        ValidateWithAttr = null;

        var attrSyntax = (attr.ApplicationSyntaxReference!.GetSyntax() as AttributeSyntax)!;

        var argList = attrSyntax.ArgumentList!.Arguments;

        if (argList.Count == 0 || argList.Count > 2)
            return false;

        if (!TryGetNameOfArg(argList[0].Expression, out var validatorName)) {
            diags.Add(
                Diagnostic.Create(
                    Diagnostics.ValidateWithMustBeNameOfExpr,
                    Utils.GetApplicationLocation(attr),
                    argList[0].Expression.ToString()
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

    public bool TryGetDescription(AttributeData descAttrib, out string? desc)
        => TryGetCtorArg<string?>(descAttrib, 0, SpecialType.System_String, out desc);
}