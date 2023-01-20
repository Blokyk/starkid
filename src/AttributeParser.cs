using System.Diagnostics;

namespace Recline.Generator;

internal enum CLIMemberKind {
    None,
    Argument,
    Group,
    Option,
    Command,
    Invalid,
    DescriptionOnly,
}

internal class AttributeParser
{
    private readonly Action<Diagnostic> _addDiagnostic;
    private readonly Cache<ISymbol, (bool, AttributeListInfo)> _attrListCache;

    public AttributeParser(Action<Diagnostic> addDiagnostic) {
        _addDiagnostic = addDiagnostic;

        _attrListCache
            = new(
                SymbolEqualityComparer.Default,
                TryGetAttributeList
            );
    }

    /// <summary>
    /// Reports diagnostics for invalid member kinds
    /// </summary>
    CLIMemberKind ValidateAttributeListAndGetKind(AttributeListInfo attrList, ISymbol symbol) {
        var kind = CategorizeAttributeList(attrList);

        if (kind is CLIMemberKind.DescriptionOnly && symbol is not IParameterSymbol) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.OnlyDescAttr,
                    symbol.GetDefaultLocation()
                )
            );

            return CLIMemberKind.DescriptionOnly;
        }

        if (kind is not CLIMemberKind.Invalid)
            return kind;

        // past this point, we are trying to figure out why this wasn't valid

        // we don't care about group because it will never be with the other attributes for a valid symbol
        var (isOnParam, _, cmd, _, opt, parseWith, validateWith) = attrList;

        if (opt is null) {
            Debug.Assert(!isOnParam);

            if (parseWith is not null) {
                _addDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.ParseOnNonOptOrArg,
                        symbol.GetDefaultLocation(),
                        symbol.GetErrorName()
                    )
                );
            }

            if (validateWith is not null) {
                _addDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.ValidateOnNonOptOrArg,
                        symbol.GetDefaultLocation(),
                        symbol.GetErrorName()
                    )
                );
            }
        } else { // if opt is not null
            if (cmd is not null) {
                _addDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.BothOptAndCmd,
                        symbol.GetDefaultLocation(),
                        symbol.GetErrorName()
                    )
                );
            }
        }

        return CLIMemberKind.Invalid;
    }

    public static CLIMemberKind CategorizeAttributeList(AttributeListInfo attrList) {
        var (isOnParam, group, cmd, desc, opt, parse, valid) = attrList;

        return
            (isOnParam,    group,      cmd,      opt,    parse,    valid,     desc) switch {
            (    false,     null,     null,     null,     null,     null,     null) => CLIMemberKind.None,
            (    false, not null,     null,     null,     null,     null,        _) => CLIMemberKind.Group,
            (    false,     null, not null,     null,     null,     null,        _) => CLIMemberKind.Command,
            (        _,     null,     null, not null,        _,        _,        _) => CLIMemberKind.Option,
            (     true,     null,     null,     null,        _,        _,        _) => CLIMemberKind.Argument,
            (    false,     null,     null,     null,     null,     null, not null) => CLIMemberKind.DescriptionOnly,
            _ => CLIMemberKind.Invalid,
        };
    }

    public bool TryGetAttributeList(ISymbol symbol, out AttributeListInfo attrList) {
        (var isValid, attrList) = _attrListCache.GetValue(symbol);

        ValidateAttributeListAndGetKind(attrList, symbol);

        return isValid;
    }

    (bool, AttributeListInfo) TryGetAttributeList(ISymbol symbol) {
        var attribList = new AttributeListInfo();
        var attrs = symbol.GetAttributes();

        CommandGroupAttribute? group = null;
        CommandAttribute? cmd = null;
        DescriptionAttribute? desc = null;
        OptionAttribute? opt = null;
        ParseWithAttribute? parseWith = null;
        ValidateWithAttribute? validateWith = null;

        bool isValid = true;

        (bool, AttributeListInfo) error() => (false, attribList);

        foreach (var attr in attrs) {
            switch (attr.AttributeClass?.Name) {
                case Resources.CmdGroupAttribName:
                    if (!TryParseGroupAttrib(attr, out group))
                        return error();
                    break;
                case Resources.CmdAttribName:
                    if (!TryParseCmdAttrib(attr, out cmd))
                        return error();

                    // todo: refactor those out into a Validate*Name/Content(...) method
                    if (String.IsNullOrWhiteSpace(cmd.CmdName)) {
                        _addDiagnostic(
                            Diagnostic.Create(
                                Diagnostics.EmptyCmdName,
                                Utils.GetApplicationLocation(attr)
                            )
                        );

                        isValid = false;
                    }

                    foreach (var c in cmd.CmdName) {
                        if (Utils.IsAsciiLetter(c))
                            continue;
                        if (Utils.IsAsciiDigit(c))
                            continue;
                        if (c == '-')
                            continue;
                        if (c == '_')
                            continue;
                        if (c == '#' && cmd.CmdName.Length == 1)
                            continue;

                        _addDiagnostic(
                            Diagnostic.Create(
                                Diagnostics.InvalidCmdName,
                                Utils.GetApplicationLocation(attr),
                                cmd.CmdName
                            )
                        );

                        isValid = false;
                        break;
                    }

                    break;
                case Resources.DescAttribName:
                    if (!TryParseDescAttrib(attr, out desc))
                        return error();

                    if (String.IsNullOrWhiteSpace(desc.Description)) {
                        _addDiagnostic(
                            Diagnostic.Create(
                                Diagnostics.DescCantBeNull,
                                Utils.GetApplicationLocation(attr),
                                symbol.GetErrorName()
                            )
                        );
                    }

                    break;
                case Resources.OptAttribName:
                    if (!TryParseOptAttrib(attr, out opt))
                        return error();

                    if (String.IsNullOrWhiteSpace(opt.LongName)) {
                        _addDiagnostic(
                            Diagnostic.Create(
                                Diagnostics.EmptyOptLongName,
                                Utils.GetApplicationLocation(attr)
                            )
                        );

                        isValid = false;
                    }

                    if (Char.IsWhiteSpace(opt.Alias)) { // '\0' is not whitespace :P
                        _addDiagnostic(
                            Diagnostic.Create(
                                Diagnostics.EmptyOptShortName,
                                Utils.GetApplicationLocation(attr)
                            )
                        );

                        isValid = false;
                    }

                    break;
                case Resources.ParseWithAttribName:
                    if (!TryParseParseAttrib(attr, out parseWith))
                        return error();
                    break;
                case Resources.ValidateWithAttribName:
                    if (!TryParseValidateAttrib(attr, out validateWith))
                        return error();
                    break;
                default:
                    continue;
            }
        }

        attribList = new(symbol is IParameterSymbol, group, cmd, desc, opt, parseWith, validateWith);

        return (isValid, attribList);
    }

    public bool TryParseCmdAttrib(AttributeData attr, [NotNullWhen(true)] out CommandAttribute? cmdAttr) {
        cmdAttr = null;

        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var cmdName))
            return false;

        cmdAttr = new(cmdName);
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

    public bool TryParseGroupAttrib(AttributeData attr, [NotNullWhen(true)] out CommandGroupAttribute? groupAttr) {
        groupAttr = null;

        // appName
        if (!TryGetCtorArg<string>(attr, 0, SpecialType.System_String, out var appName))
            return false;

        // DefaultCommandName
        if (!TryGetProp<string?>(attr, nameof(CommandGroupAttribute.DefaultCmdName), SpecialType.System_String, null, out var defaultCmdName))
            return false;

        groupAttr = new(appName) {
            DefaultCmdName = defaultCmdName
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
                    argList[0].Expression.ToString()
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