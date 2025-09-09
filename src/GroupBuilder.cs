using StarKid.Generator.AttributeModel;
using StarKid.Generator.CommandModel;
using StarKid.Generator.SymbolModel;

namespace StarKid.Generator;

internal sealed class GroupBuilder
{
    private readonly HashSet<string> _cmdNames = new();

    private readonly Compilation _compilation;
    private readonly AttributeListBuilder _attrListBuilder;
    private readonly ParserFinder _parserFinder;
    private readonly ValidatorFinder _validatorFinder = null!;

    private readonly Action<Diagnostic> _addDiagnostic;

    // protected because test project inherits to instantiate it
    internal GroupBuilder(AttributeListBuilder attrListBuilder, Compilation compilation, Action<Diagnostic> addDiagnostic) {
        _attrListBuilder = attrListBuilder;
        _compilation = compilation;
        _addDiagnostic = addDiagnostic;
        _parserFinder = new(_addDiagnostic, _compilation);
        _validatorFinder = new(_addDiagnostic, _compilation);
    }

    public static bool TryCreateGroupFrom(INamedTypeSymbol classSymbol, AttributeListBuilder attrListBuilder, Compilation compilation, Action<Diagnostic> addDiagnostic, [NotNullWhen(true)] out Group? group) {
        group = null;

        var groupBuilder = new GroupBuilder(attrListBuilder, compilation, addDiagnostic);

        if (!groupBuilder.IsValidGroupClass(classSymbol))
            return false;

        if (!attrListBuilder.TryGetAttributeList(classSymbol, out var attrList))
            return false;

        if (attrList.CommandGroup is null)
            return false;

        var docInfo = SymbolUtils.GetDocInfo(classSymbol);

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
                        // since right now we don't allow any other hidden command than '#',
                        // if this group has a "normal" command as default, this is command
                        // is useless
                        if (defaultCmdName != "#") {
                            addDiagnostic(
                                Diagnostic.Create(
                                    Diagnostics.UselessSpecialCmdName,
                                    cmd.Location,
                                    defaultCmdName
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

    public bool TryCreateCommandFrom(IMethodSymbol method, AttributeListInfo attrList, Group containingGroup, [NotNullWhen(true)] out Command? cmd) {
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

        var docInfo = SymbolUtils.GetDocInfo(method);

        var minMethodSymbol = MinimalMethodInfo.FromSymbol(method);

        string cmdName = attrList.Command.CommandName;
        string? desc = attrList.Command.ShortDesc;

        bool isValid = true;

        if (minMethodSymbol.ReturnType.SpecialType is not (SpecialType.System_Int32 or SpecialType.System_Void)) {
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
                    minMethodSymbol.Location
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
            if (!IsParamValidForCommand(param, _addDiagnostic))
                return false;

            if (!_attrListBuilder.TryGetAttributeList(param, out var paramAttrList))
                return false;

            if (paramAttrList.Kind == CLIMemberKind.Option) {
                if (!TryCreateOptionFrom(param, paramAttrList, out var opt))
                    return false;

                // having a global option in a non-group command is useless
                if (opt.IsGlobal) {
                    _addDiagnostic(
                        Diagnostic.Create(Diagnostics.IsGlobalOnNonGroupOpt, param.GetDefaultLocation())
                    );
                }

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

    public bool TryCreateOptionFrom(ISymbol symbol, AttributeListInfo attrInfo, [NotNullWhen(true)] out Option? option) {
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
        bool isGlobal = attrInfo.Option!.IsGlobal;

        if (!IsSymbolValidForOption(symbol, _addDiagnostic))
            return false;

        MinimalSymbolInfo backingSymbol
                = symbol is IParameterSymbol paramSymbol
                    ? MinimalParameterInfo.FromSymbol(paramSymbol)
                    : MinimalMemberInfo.FromSymbol(symbol);

        var type = GetTypeForSymbol(symbol);

        if (!TryGetParser(attrInfo.ParseWith, type, symbol, isOption: true, out var parser))
            return false;

        if (!TryGetMethodValidators(attrInfo.ValidateWithList.Array, type, symbol, out var methodValidators))
            return false;

        if (!TryGetPropertyValidators(attrInfo.ValidatePropList.Array, type, symbol, out var propValidators))
            return false;

        var typeMinInfo = MinimalTypeInfo.FromSymbol(type);

        var defaultValStr = SymbolUtils.GetDefaultValue(symbol);
        var docInfo = SymbolUtils.GetDocInfo(symbol);

        var allValidators = methodValidators.AddRange(propValidators);

        // if it's a flag
        if (typeMinInfo.SpecialType == SpecialType.System_Boolean) {
            option = new Flag(
                longName,
                shortName,
                isGlobal,
                parser,
                backingSymbol,
                defaultValStr
            ) {
                Validators = allValidators.ToValueArray(),
                Description = docInfo?.Summary
            };

            return true;
        }

        option = new Option(
            typeMinInfo,
            longName,
            shortName,
            isGlobal,
            parser,
            backingSymbol,
            defaultValStr
        ) {
            Validators = allValidators.ToValueArray(),
            Description = docInfo?.Summary,
            CustomArgName = argName
        };

        return true;
    }

    public bool TryGetArg(IParameterSymbol param, AttributeListInfo attrList, [NotNullWhen(true)] out Argument? arg) {
        arg = null;

        var parserTargetType
            // we have to check for IArrayTypeSymbol cause it's possible
            // we have to parse invalid code like `params int nums`
            = param.IsParams && param.Type is IArrayTypeSymbol { ElementType: var itemType }
            ? itemType
            : param.Type;

        if (!TryGetParser(attrList.ParseWith, parserTargetType, param, isOption: false, out var parser))
            return false;

        if (!TryGetMethodValidators(attrList.ValidateWithList.Array, param.Type, param, out var methodValidators))
            return false;

        if (!TryGetPropertyValidators(attrList.ValidatePropList.Array, param.Type, param, out var propValidators))
            return false;

        var paramMinInfo = MinimalParameterInfo.FromSymbol(param);
        var defaultVal = SymbolUtils.GetDefaultValue(param);

        var allValidators = methodValidators.AddRange(propValidators);

        arg = new Argument(
            parser,
            paramMinInfo,
            defaultVal
        ) {
            Validators = allValidators.ToValueArray(),
        };

        return true;
    }

    public bool TryGetParser(
        ParseWithAttribute? attr,
        ITypeSymbol type,
        ISymbol symbol,
        bool isOption,
        [NotNullWhen(true)] out ParserInfo? parser
    ) {
        parser = null;
        if (attr is null) {
            var targetType = type;

            // if this is an array option, there's no way we can auto-find a parser
            // for the array itself; however, this might be a repeatable option, in
            // which case we instead want to parse the items' type
            if (type is IArrayTypeSymbol { ElementType: var itemType }) {
                if (!isOption) {
                    _addDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.NoAutoParserForArrayArg,
                            symbol.GetDefaultLocation()
                        )
                    );

                    return false;
                }

                targetType = itemType;
            }

            if (!_parserFinder.TryFindParserForType(targetType, symbol.GetDefaultLocation(), out parser))
                return false;
        } else {
            // in the case of arguments, no need to check afterwards if
            // it's element-wise since passing `isOption = false` ensures
            // that it'll get rejected anyway
            if (!_parserFinder.TryGetParserFromName(attr, type, isOption, out parser))
                return false;
        }

        return true;
    }

    public bool TryGetMethodValidators(
        ImmutableArray<ValidateWithAttribute> attrs,
        ITypeSymbol type,
        ISymbol _,
        out ImmutableArray<ValidatorInfo> validators
    ) {
        validators = [];

        if (attrs is [])
            return true;

        var builder = ImmutableArray.CreateBuilder<ValidatorInfo>(attrs.Length);

        for (int i = 0; i < attrs.Length; i++) {
            if (!_validatorFinder.TryGetValidator(attrs[i], type, out var validator))
                return false;
            builder.Add(validator);
        }

        validators = builder.MoveToImmutable();
        return true;
    }

    public bool TryGetPropertyValidators(
        ImmutableArray<ValidatePropAttribute> attrs,
        ITypeSymbol type,
        ISymbol _,
        out ImmutableArray<ValidatorInfo> validators
    ) {
        validators = [];

        if (attrs.IsDefaultOrEmpty)
            return true;

        var builder = ImmutableArray.CreateBuilder<ValidatorInfo>(attrs.Length);

        for (int i = 0; i < attrs.Length; i++) {
            if (!_validatorFinder.TryGetValidator(attrs[i], type, out var validator))
                return false;
            builder.Add(validator);
        }

        validators = builder.MoveToImmutable();
        return true;
    }

    public bool TryRegisterCommandName(Command cmd, ISymbol _) {
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
        if (classSymbol.IsGenericType) {
            _addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.GroupClassMustNotBeGeneric,
                    classSymbol.GetDefaultLocation()
                )
            );

            return false;
        }

        var curr = classSymbol;
        while (curr is not null) {
            if (!curr.IsStatic) {
                _addDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.GroupClassMustBeStatic,
                        curr.GetDefaultLocation()
                    )
                );

                return false;
            }

            curr = curr.ContainingType;
        }

        return true;
    }

    static bool IsParamValidForCommand(IParameterSymbol param, Action<Diagnostic> addDiagnostic) {
        // we can't have ref/out/in parameters
        if (param.RefKind != RefKind.None) {
            addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.CmdParamCantBeRef,
                    param.GetDefaultLocation()
                )
            );
            return false;
        }

        if (param.Type.IsRefLikeType) {
            addDiagnostic(
                Diagnostic.Create(
                    Diagnostics.CmdParamTypeCantBeRefStruct,
                    param.GetDefaultLocation()
                )
            );
            return false;
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

    static void TryBindChildDocInfo(ref Option option, DocumentationInfo? docInfo) {
        // if (docInfo is not null)
        //     throw new Exception($"TryBindChildDocInfo({option}, {String.Join(", ", docInfo?.ParamSummaries)})");
        if (docInfo is not null && docInfo.ParamSummaries.TryGetValue(option.BackingSymbol.Name, out var paramDesc))
            option.Description = paramDesc;
    }

    static void TryBindChildDocInfo(ref Argument arg, DocumentationInfo? docInfo) {
        if (docInfo is not null && docInfo.ParamSummaries.TryGetValue(arg.BackingSymbol.Name, out var paramDesc))
            arg.Description = paramDesc;
    }
}