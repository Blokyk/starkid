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
    private readonly Cache<ISymbol, ImmutableArray<Diagnostic>.Builder, (bool, AttributeListInfo)> _attrListCache;

    public AttributeParser() => _attrListCache = new(SymbolEqualityComparer.Default, TryGetAttributeList);

    static MemberKind ValidateAttributeList(AttributeListInfo attrList, ISymbol symbol, ref ImmutableArray<Diagnostic>.Builder diagnostics) {
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

        ValidateAttributeList(attrList, symbol, ref diagnostics);

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
                    if (!TryParseParseAttrib(attr, symbol.ContainingType, out parseWith))
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
                    if (!TryParseValidateAttrib(attr, symbol.ContainingType, out validateWith))
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

        if (attr.ConstructorArguments.Length < 1)
            return false;

        if (!SymbolUtils.Equals(attr.ConstructorArguments[0].Type, CommonTypes.STR))
            return false;

        cmdAttr = new((string)attr.ConstructorArguments[0].Value!);
        return true;
    }

    public bool TryParseSubCmdAttrib(AttributeData attr, [NotNullWhen(true)] out SubCommandAttribute? subCmdAttr) {
        subCmdAttr = null;

        if (attr.ConstructorArguments.Length < 2)
            return false;
        if (attr.NamedArguments.Length > 1)
            return false;

        if (!SymbolUtils.Equals(attr.ConstructorArguments[0].Type, CommonTypes.STR))
            return false;
        var cmdName = (string)attr.ConstructorArguments[0].Value!;

        if (!SymbolUtils.Equals(attr.ConstructorArguments[1].Type, CommonTypes.STR))
            return false;
        var parentName = (string)attr.ConstructorArguments[1].Value!;

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

        string longName;
        char shortName = '\0';

        if (attr.ConstructorArguments.Length < 1)
            return false;

        if (!SymbolUtils.Equals(attr.ConstructorArguments[0].Type, CommonTypes.STR))
            return false;

        longName = (string)attr.ConstructorArguments[0].Value!;

        if (attr.ConstructorArguments.Length == 2) {
            if (!SymbolUtils.Equals(attr.ConstructorArguments[1].Type, CommonTypes.CHAR))
                return false;

            shortName = (char)attr.ConstructorArguments[1].Value!;
        }

        string? argName = null;

        var argNameArg = attr.NamedArguments.FirstOrDefault(kv => kv.Key == nameof(OptionAttribute.ArgName));

        if (!argNameArg.Equals(default))
            argName = (string)argNameArg.Value.Value!;

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

        if (!SymbolUtils.Equals(attr.ConstructorArguments[0].Type, CommonTypes.STR))
            return false;

        descAttr = new((string)attr.ConstructorArguments[0].Value!);

        return true;
    }

    public bool TryParseCLIAttrib(AttributeData attr, [NotNullWhen(true)] out CLIAttribute? cliAttr) {
        cliAttr = null;

        // appName
        if (!TryGetCtorArg<string>(attr, 0, CommonTypes.STR, out var appName))
            return false;

        // EntryPoint
        if (!TryGetProp<string?>(attr, nameof(CLIAttribute.EntryPoint), CommonTypes.STR, null, out var entryPoint))
            return false;

        if (!TryGetProp<int>(attr, nameof(CLIAttribute.HelpExitCode), CommonTypes.INT32, 0, out var helpIsError))
            return false;

        cliAttr = new(appName) {
            EntryPoint = entryPoint,
            HelpExitCode = helpIsError
        };

        return true;
    }

    public bool TryParseParseAttrib(AttributeData attr, ITypeSymbol applicationType,  [NotNullWhen(true)] out ParseWithAttribute? parseWithAttr) {
        parseWithAttr = null;

        var ctorArgs = attr.ConstructorArguments;

        if (ctorArgs.Length < 1)
            return false;

        int nameCtorIdx = 0;

        var containingType = applicationType;

        if (ctorArgs.Length == 2) {
            if (ctorArgs[0].Value is not ITypeSymbol type)
                return false;

            containingType = type;
            nameCtorIdx++;
        }

        if (!TryGetCtorArg<string>(attr, nameCtorIdx, CommonTypes.STR, out var parserName))
            return false;

        parseWithAttr = new ParseWithAttribute(containingType, parserName);

        return true;
    }

    public bool TryParseValidateAttrib(AttributeData attr, ITypeSymbol applicationType, [NotNullWhen(true)] out ValidateWithAttribute? parseWithAttr) {
        parseWithAttr = null;

        var ctorArgs = attr.ConstructorArguments;

        if (ctorArgs.Length < 1)
            return false;

        int nameCtorIdx = 0;

        var containingType = applicationType;

        if (ctorArgs.Length == 2) {
            if (ctorArgs[0].Value is not ITypeSymbol type)
                return false;

            containingType = type;
            nameCtorIdx++;
        }

        if (!TryGetCtorArg<string>(attr, nameCtorIdx, CommonTypes.STR, out var parserName))
            return false;

        parseWithAttr = new ValidateWithAttribute(containingType, parserName);

        return true;
    }

    public bool TryGetCtorArg<T>(AttributeData attrib, int ctorIdx, INamedTypeSymbol type, [NotNullWhen(true)] out T? val) {
        val = default;

        var ctorArgs = attrib.ConstructorArguments;

        if (ctorArgs.Length < ctorIdx + 1) {
            return false;
        }

        if (!SymbolUtils.Equals(ctorArgs[ctorIdx].Type, type) || ctorArgs[ctorIdx].Value is not T)
            return false;

        val = (T)ctorArgs[ctorIdx].Value!;

        return true;
    }

    public bool TryGetProp<T>(AttributeData attrib, string propName, INamedTypeSymbol type, T defaultVal, out T val) {
        val = defaultVal;

        var namedArgs = attrib.NamedArguments;

        if (namedArgs.IsDefaultOrEmpty)
            return true;

        var arg = namedArgs.FirstOrDefault(
            kv => kv.Key == propName
        ).Value;

        if (arg.Equals(default))
            return true;

        if (!SymbolUtils.Equals(arg.Type, type) || arg.Value is not T)
            return false;

        val = (T)arg.Value!;

        return true;
    }

    public bool TryGetDescription(AttributeData descAttrib, out string? desc)
        => TryGetCtorArg<string?>(descAttrib, 0, CommonTypes.STR, out desc);
}