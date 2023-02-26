using Recline.Generator.Model;

namespace Recline.Generator;

internal sealed class GroupBuilder
{
    private readonly HashSet<string> _cmdNames = new();

    private readonly SemanticModel _model;
    private readonly AttributeListBuilder _attrListBuilder;
    private readonly ParserFinder _parserFinder;
    private readonly ValidatorFinder _validatorFinder = null!;

    private readonly Action<Diagnostic> _addDiagnostic;

    // todo: switch to completely static with a ResetCache method
    private GroupBuilder(AttributeListBuilder attrListBuilder, SemanticModel model, Action<Diagnostic> addDiagnostic) {
        _attrListBuilder = attrListBuilder;
        _model = model;
        _addDiagnostic = addDiagnostic;
        _parserFinder = new(_addDiagnostic, _model);
        _validatorFinder = new(_addDiagnostic, _model);
    }

    public static bool TryCreateGroupFrom(INamedTypeSymbol classSymbol, AttributeListBuilder attrListBuilder, SemanticModel model, Action<Diagnostic> addDiagnostic, [NotNullWhen(true)] out Group? group) {
        group = null;

        var groupBuilder = new GroupBuilder(attrListBuilder, model, addDiagnostic);

        if (!groupBuilder.IsValidGroupClass(classSymbol))
            return false;

        if (!attrListBuilder.TryGetAttributeList(classSymbol, out var attrList))
            return false;

        if (attrList.CommandGroup is null)
            return false;

        var docInfo = GetDocInfo(classSymbol);

        var (name, defaultCmdName, shortDesc) = attrList.CommandGroup;
        var minClassSymbol = MinimalTypeInfo.FromSymbol(classSymbol);

        var parentSymbol = minClassSymbol.ContainingType;

        group = new(
            name,
            minClassSymbol.FullName,
            parentSymbol?.FullName,
            minClassSymbol
        ) {
            Description = DescriptionInfo.From(shortDesc, docInfo)
        };

        foreach (var member in classSymbol.GetMembers()) {
            switch (member.Kind) {
                case SymbolKind.Field:
                case SymbolKind.Property:
                    if (!groupBuilder._attrListBuilder.TryGetAttributeList(member, out var optAttrInfo))
                        return false;
                    if (optAttrInfo.Kind != CLIMemberKind.Option)
                        continue;

                    if (!groupBuilder.TryCreateOptionFrom(member, optAttrInfo, out var option))
                        return false;

                    TryBindChildDocInfo(ref option, docInfo);

                    group.AddOption(option);
                    break;

                case SymbolKind.Method:
                    if (!groupBuilder._attrListBuilder.TryGetAttributeList(member, out var cmdAttInfo))
                        return false;
                    if (cmdAttInfo.Kind != CLIMemberKind.Command)
                        continue;

                    if (!groupBuilder.TryCreateCommandFrom((IMethodSymbol)member, cmdAttInfo, group, out var cmd))
                        return false;

                    if (!groupBuilder.TryRegisterCommandName(cmd, member))
                        return false;

                    // if the cmd's "real name" is '#'
                    if (cmd.IsHiddenCommand) {
                        if (defaultCmdName != "#") {
                            addDiagnostic(
                                Diagnostic.Create(
                                    Diagnostics.UselessSpecialCmdName,
                                    cmd.Location
                                )
                            );
                        } else {
                            group.SetDefaultCommand(cmd);
                        }
                    } else if (cmd.Name == defaultCmdName) {
                        // note: by this point, we know that there's no duplicate names for commands,
                        //       so we don't need to check if it's already assigned or whatever

                        group.SetDefaultCommand(cmd);
                    }

                    group.AddCommand(cmd);

                    break;
            }
        }

        if (defaultCmdName is not null && group.DefaultCommand is null) {
            addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.CouldntFindDefaultCmd,
                    minClassSymbol.Location,
                    defaultCmdName
                )
            );

            return false;
        }

        return true;
    }

    private bool TryCreateCommandFrom(IMethodSymbol method, AttributeListInfo attrList, Group containingGroup, [NotNullWhen(true)] out Command? cmd) {
        cmd = null;

        if (method.MethodKind != MethodKind.Ordinary) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.CmdMustBeOrdinary,
                    method.GetDefaultLocation(),
                    method.GetErrorName(), method.MethodKind
                )
            );

            return false;
        }

        if (attrList.Command is null)
            return false;

        var docInfo = GetDocInfo(method);

        var minMethodSymbol = MinimalMethodInfo.FromSymbol(method);

        bool hasExitCode = !minMethodSymbol.ReturnsVoid;

        string cmdName = attrList.Command.CmdName;
        string? desc = attrList.Command.ShortDesc;

        bool isValid = true;

        if (hasExitCode && minMethodSymbol.ReturnType != CommonTypes.INT32MinInfo) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.CmdMustBeVoidOrInt,
                    minMethodSymbol.Location,
                    method.GetErrorName(), method.ReturnType.GetNameWithNull()
                )
            );

            isValid = false;
        }

        if (minMethodSymbol.IsGeneric) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.CmdCantBeGeneric,
                    minMethodSymbol.Location,
                    method.GetErrorName()
                )
            );

            isValid = false;
        }

        cmd = new Command(
            cmdName,
            containingGroup,
            minMethodSymbol
        ) {
            Description = DescriptionInfo.From(desc, docInfo)
        };

        foreach (var param in method.Parameters) {
            if (!_attrListBuilder.TryGetAttributeList(param, out var paramAttrList))
                return false;

            if (paramAttrList.Kind == CLIMemberKind.Option) {
                if (!TryCreateOptionFrom(param, paramAttrList, out var opt))
                    return false;

                TryBindChildDocInfo(ref opt, docInfo);

                cmd.AddOption(opt);
            } else {
                if (!TryGetArg(param, paramAttrList, out var arg))
                    return false;

                TryBindChildDocInfo(ref arg, docInfo);

                cmd.AddArg(arg);
            }
        }

        return isValid;
    }

    private bool TryCreateOptionFrom(ISymbol symbol, AttributeListInfo attrInfo, [NotNullWhen(true)] out Option? option) {
        option = null;

        static ITypeSymbol GetTypeForSymbol(ISymbol symbol)
            => symbol switch {
                IFieldSymbol field => field.Type,
                IPropertySymbol prop => prop.Type,
                IParameterSymbol param => param.Type,
                _ => throw new ArgumentException("Can't get .Type for " + symbol.GetType().Name + "s", nameof(symbol))
            };

        string longName = attrInfo.Option!.LongName;
        char shortName = attrInfo.Option!.Alias;
        string? argName = attrInfo.Option!.ArgName;
        var docInfo = GetDocInfo(symbol);

        bool isValid = IsSymbolValidForOption(symbol, _addDiagnostic);

        var defaultValStr = GetDefaultValueForSymbol(symbol);

        if (!isValid)
            return false;

        MinimalSymbolInfo backingSymbol
                = symbol is IParameterSymbol paramSymbol
                    ? MinimalParameterInfo.FromSymbol(paramSymbol)
                    : MinimalMemberInfo.FromSymbol(symbol);

        // todo(#4): special-case arrays/lists to be "repeatable" options
        var type = GetTypeForSymbol(symbol);

        ParserInfo? parser;
        ValidatorInfo? validator = null;

        if (attrInfo.ParseWith is null) {
            if (!_parserFinder.TryFindParserForType(type, symbol.GetDefaultLocation(), out parser))
                return false;
        } else {
            if (!_parserFinder.TryGetParserFromName(attrInfo.ParseWith, type, out parser))
                return false;
        }

        if (attrInfo.ValidateWith is not null && !_validatorFinder.TryGetValidator(attrInfo.ValidateWith, type, out validator))
            return false;

        var typeMinInfo = MinimalTypeInfo.FromSymbol(type);

        // if it's a flag
        if (typeMinInfo == CommonTypes.BOOLMinInfo) {
            option = new Flag(
                longName,
                shortName,
                parser,
                backingSymbol,
                defaultValStr
            ) {
                Validator = validator,
                Description = docInfo?.Summary
            };

            return true;
        }

        option = new Option(
            typeMinInfo,
            longName,
            shortName,
            argName ?? symbol.Name,
            parser,
            backingSymbol,
            defaultValStr
        ) {
            Validator = validator,
            Description = docInfo?.Summary
        };

        return true;
    }

    bool TryGetArg(IParameterSymbol param, AttributeListInfo attrList, [NotNullWhen(true)] out Argument? arg) {
        arg = null;

        string? defaultVal = null;

        foreach (var synRef in param.DeclaringSyntaxReferences) {
            if (synRef.GetSyntax() is not ParameterSyntax paramDec)
                return false;

            if (paramDec.Default is not null) {
                defaultVal = paramDec.Default.Value.ToString();
                break;
            }
        }

        bool isParams = param.IsParams;

        // todo(#3): allow custom 'params' args (e.g. params FileInfo[] files)
        // doesn't really matter rn, but it'd be nice to be able to use any type for params,
        // because i'd really like to just say 'params FileInfo[] files' instead of having
        // to transform/validate it myself
        if (isParams && (param.Type is not IArrayTypeSymbol paramArrayType || !SymbolUtils.Equals(paramArrayType.ElementType, CommonTypes.STR))) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.ParamsHasToBeString,
                    param.GetDefaultLocation(),
                    param.Type.GetErrorName()
                )
            );
        }

        ParserInfo? parser;
        ValidatorInfo? validator = null;

        if (isParams) {
            if (attrList.ParseWith is not null) {
                _addDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.ParamsCantBeParsed,
                        param.GetDefaultLocation()
                    )
                );
            }

            if (attrList.ValidateWith is not null) {
                _addDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.ParamsCantBeValidated,
                        param.GetDefaultLocation()
                    )
                );
            }

            parser = ParserInfo.StringIdentity;
        } else {
            if (attrList.ParseWith is null) {
                if (!_parserFinder.TryFindParserForType(param.Type, param.GetDefaultLocation(), out parser)) {
                    _parserFinder.AddDiagnosticIfInvalid(parser);
                    return false;
                }
            } else {
                if (!_parserFinder.TryGetParserFromName(attrList.ParseWith, param.Type, out parser)) {
                    _parserFinder.AddDiagnosticIfInvalid(parser);
                    return false;
                }
            }

            if (attrList.ValidateWith is not null && !_validatorFinder.TryGetValidator(attrList.ValidateWith, param.Type, out validator))
                return false;
        }

        var paramMinInfo = MinimalParameterInfo.FromSymbol(param);

        arg = new Argument(
            paramMinInfo.Type,
            param.Name,
            parser,
            paramMinInfo,
            defaultVal
        ) {
            Validator = validator,
        };

        return true;
    }

    private bool TryRegisterCommandName(Command cmd, ISymbol _) {
        if (!_cmdNames.Add(cmd.Name)) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.CmdNameAlreadyExists,
                    cmd.Location,
                    cmd.Name
                )
            );

            return false;
        }

        return true;
    }

    bool IsValidGroupClass(INamedTypeSymbol classSymbol) {
        if (!classSymbol.IsStatic)
            return false;

        if (classSymbol.IsGenericType) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.GroupClassMustBeStatic,
                    classSymbol.GetDefaultLocation(),
                    classSymbol.GetErrorName()
                )
            );
        }

        return true;
    }

    static bool IsSymbolValidForOption(ISymbol symbol, Action<Diagnostic> addDiagnostic) {
        switch (symbol) {
            case IFieldSymbol field:
                if (field.IsReadOnly || field.IsConst) {
                    addDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.NonWritableOptField,
                            symbol.GetDefaultLocation(),
                            symbol.GetErrorName()
                        )
                    );

                    return false;
                }

                break;
            case IPropertySymbol prop:
                var accessibility = prop.SetMethod?.DeclaredAccessibility ?? Accessibility.Private;
                if (prop.IsReadOnly || accessibility is < Accessibility.Internal and not Accessibility.NotApplicable) {
                    addDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.NonWritableOptProp,
                            symbol.GetDefaultLocation(),
                            symbol.GetErrorName()
                        )
                    );

                    return false;
                }

                break;
            case IParameterSymbol parameterSymbol:
                if (parameterSymbol.IsParams) {
                    addDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.ParamsCantBeOption,
                            symbol.GetDefaultLocation(),
                            symbol.GetErrorName()
                        )
                    );

                    return false;
                }

                break;
            default:
                return false;
        }

        return true;
    }

    static string? GetDefaultValueForSymbol(ISymbol symbol) {
        if (symbol is IParameterSymbol parameterSymbol) {
            if (!parameterSymbol.HasExplicitDefaultValue)
                return null;

            var defaultVal = parameterSymbol.ExplicitDefaultValue;

            if (defaultVal is null)
                return null;

            return defaultVal switch {
                string s => '"' + s + '"',
                char c => "'" + c + "'",
                bool b => b ? "true" : "false",
                _ => defaultVal.ToString() // fixme(#2): this could break cause of culture
            };
        }

        return null;
    }

    static DocumentationInfo? GetDocInfo(ISymbol symbol) {
        var xml = symbol.GetDocumentationCommentXml(preferredCulture: System.Globalization.CultureInfo.InvariantCulture, expandIncludes: true);
        return xml is null
            ? null
            : DocumentationParser.ParseDocumentationInfoFrom(xml);
    }

    static void TryBindChildDocInfo(ref Option option, DocumentationInfo? docInfo) {
        if (docInfo is not null && docInfo.ParamSummaries.TryGetValue(option.BackingSymbol.Name, out var paramDesc))
            option.Description = paramDesc;
    }

    static void TryBindChildDocInfo(ref Argument arg, DocumentationInfo? docInfo) {
        if (docInfo is not null && docInfo.ParamSummaries.TryGetValue(arg.BackingSymbol.Name, out var paramDesc))
            arg.Description = paramDesc;
    }
}