using System.Diagnostics;

namespace StarKid.Generator.AttributeModel;

internal class AttributeListBuilder
{
    private readonly Action<Diagnostic> _addDiagnostic;
    private readonly Cache<ISymbol, (bool, AttributeListInfo)> _attrListCache;
    private readonly AttributeParser _parser;

    public AttributeListBuilder(Action<Diagnostic> addDiagnostic) {
        _addDiagnostic = addDiagnostic;

        _attrListCache
            = new(
                SymbolEqualityComparer.Default,
                TryGetAttributeListCore
            );

        _parser = new(addDiagnostic);
    }

    /// <summary>
    /// Reports diagnostics for invalid member kinds
    /// </summary>
    CLIMemberKind ValidateAttributeListAndGetKind(AttributeListInfo attrList, ISymbol symbol) {
        var kind = CategorizeAttributeList(attrList);

        if (kind is not CLIMemberKind.Invalid)
            return kind;

        // past this point, we are trying to figure out why this wasn't valid

        // we don't care about group because it will never be with the other attributes for a valid symbol
        var (_, cmd, opt, parseWith, validateWithList, validatePropList, isOnParam) = attrList;

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

            if (!validateWithList.IsDefaultOrEmpty) {
                _addDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.ValidateOnNonOptOrArg,
                        symbol.GetDefaultLocation(),
                        symbol.GetErrorName()
                    )
                );
            }

            if (!validatePropList.IsDefaultOrEmpty) {
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
        // we check here cause otherwise can't pattern-match on valid.Length
        if (attrList.IsUninitialized)
            return CLIMemberKind.Invalid;

        var (group, cmd, opt, parse, valid, validProp, isOnParam) = attrList;

        return
            (isOnParam,    group,      cmd,      opt,    parse,       valid,   validProp) switch {
            (    false,     null,     null,     null,     null, {Length: 0}, {Length: 0}) => CLIMemberKind.None,
            (    false, not null,     null,     null,     null, {Length: 0}, {Length: 0}) => CLIMemberKind.Group,
            (    false,     null, not null,     null,     null, {Length: 0}, {Length: 0}) => CLIMemberKind.Command,
            (        _,     null,     null, not null,        _,           _,           _) => CLIMemberKind.Option,
            (     true,     null,     null,     null,        _,           _,           _) => CLIMemberKind.Argument,
            _ => CLIMemberKind.Invalid,
        };
    }

    public bool TryGetAttributeList(ISymbol symbol, out AttributeListInfo attrList) {
        (var isValid, attrList) = _attrListCache.GetValue(symbol);

        ValidateAttributeListAndGetKind(attrList, symbol);

        return isValid;
    }

    (bool, AttributeListInfo) TryGetAttributeListCore(ISymbol symbol) {
        var attribList = default(AttributeListInfo);
        var attrs = symbol.GetAttributes();

        CommandGroupAttribute? group = null;
        CommandAttribute? cmd = null;
        OptionAttribute? opt = null;
        ParseWithAttribute? parseWith = null;
        var validateWithList = ImmutableArray.CreateBuilder<ValidateWithAttribute>(attrs.Length);
        var validatePropList = ImmutableArray.CreateBuilder<ValidatePropAttribute>(attrs.Length);

        bool isValid = true;

        (bool, AttributeListInfo) error() => (false, attribList);

        foreach (var attr in attrs) {
            switch (attr.AttributeClass?.Name) {
                case "CommandGroupAttribute":
                    if (!_parser.TryParseGroupAttrib(attr, out group))
                        return error();

                    isValid = ValidateName(
                        group.GroupName,
                        SyntaxUtils.GetApplicationLocation(attr),
                        isForCommands: true
                    );

                    break;
                case "CommandAttribute":
                    if (!_parser.TryParseCmdAttrib(attr, out cmd))
                        return error();

                    isValid = ValidateName(
                        cmd.CommandName,
                        SyntaxUtils.GetApplicationLocation(attr),
                        isForCommands: true
                    );

                    break;
                case "OptionAttribute":
                    if (!_parser.TryParseOptAttrib(attr, out opt))
                        return error();

                    isValid = ValidateOptionName(
                        opt.LongName,
                        opt.Alias,
                        SyntaxUtils.GetApplicationLocation(attr)
                    );

                    break;
                case "ParseWithAttribute":
                    if (!_parser.TryParseParseAttrib(attr, out parseWith))
                        return error();
                    break;
                case "ValidateWithAttribute":
                    if (!_parser.TryParseValidateAttrib(attr, out var validateWith))
                        return error();
                    validateWithList.Add(validateWith);
                    break;
                case "ValidatePropAttribute":
                    if (!_parser.TryParseValidatePropAttrib(attr, out var validateProp))
                        return error();
                    validatePropList.Add(validateProp);
                    break;
                default:
                    continue;
            }
        }

        attribList = new(
            group,
            cmd,
            opt,
            parseWith,
            validateWithList.ToImmutableValueArray(),
            validatePropList.ToImmutableValueArray(),
            symbol is IParameterSymbol
        );

        return (isValid, attribList);
    }

    public bool ValidateName(string name, Location location, bool isForCommands) {
        if (String.IsNullOrEmpty(name)) {
            _addDiagnostic(
                Diagnostic.Create(
                    isForCommands ? Diagnostics.EmptyCmdName : Diagnostics.EmptyOptLongName,
                    location
                )
            );

            return false;
        }

        if (name.StartsWith('-')) {
            if (name.Length < 2 || name[1] == '-') {
                _addDiagnostic(
                    Diagnostic.Create(
                            Diagnostics.NameCantStartWithDash,
                            location
                    )
                );
            } else { // if it's exactly '--'
                if (name.Length == 2) {
                    _addDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.DashDashForbiddenName,
                            location
                        )
                    );
                } else {
                    _addDiagnostic(
                        Diagnostic.Create(
                            isForCommands
                                ? Diagnostics.CmdNameCantBeginWithDashDash
                                : Diagnostics.OptNameCantBeginWithDashDash,
                            location,
                            name[2..]
                        )
                    );
                }
            }

            return false;
        }

        var nameIsValid
            = name.All(
                c => MiscUtils.IsAsciiLetter(c)
                  || MiscUtils.IsAsciiDigit(c)
                  || c == '-'
                  || c == '_'
            );

        if (nameIsValid)
            return true;

        // if the only character is '#' (The Special Name(tm))
        if (isForCommands && name == "#")
            return true;

        _addDiagnostic(
            Diagnostic.Create(
                isForCommands ? Diagnostics.InvalidCmdName : Diagnostics.InvalidOptLongName,
                location,
                name
            )
        );

        return false;
    }

    public bool ValidateOptionName(string longName, char alias, Location location) {
        if (alias != '\0' && !MiscUtils.IsAsciiLetter(alias) && !MiscUtils.IsAsciiDigit(alias)) {
            _addDiagnostic(
                Diagnostic.Create(
                    Char.IsWhiteSpace(alias) ? Diagnostics.EmptyOptAlias : Diagnostics.InvalidOptAlias,
                    location,
                    alias
                )
            );

            return false;
        }

        if (String.IsNullOrWhiteSpace(longName)) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.EmptyOptLongName,
                    location
                )
            );

            return false;
        }

        if (longName == "help" || alias == 'h' ) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.OptCantBeNamedHelp,
                   location
                )
            );

            return false;
        }

        return ValidateName(longName, location, isForCommands: false);
    }
}