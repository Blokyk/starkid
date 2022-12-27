using Recline.Generator.Model;

namespace Recline.Generator;

internal sealed partial class ModelBuilder
{
    private ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    private readonly HashSet<string> _topLevelOptLongNames = new();
    private readonly HashSet<char> _topLevelOptAliases = new();
    private readonly HashSet<string> _cmdNames = new();

    public ImmutableArray<Diagnostic> GetDiagnostics() => _diagnostics.ToImmutable();

    private readonly SemanticModel _model;
    private readonly AttributeParser _attrParser;
    private readonly ParserFinder _parserFinder;
    private readonly ValidatorFinder _validatorFinder = null!;
    private readonly ImmutableArray<IMethodSymbol>.Builder _nonCmdCandidateMethods = ImmutableArray.CreateBuilder<IMethodSymbol>();

    private ModelBuilder(AttributeParser parser, SemanticModel model) {
        _attrParser = parser;
        _model = model;
        _parserFinder = new(ref _diagnostics, _model);
        _validatorFinder = new(ref _diagnostics, _model);
    }

    public static bool TryCreateFromSymbol(INamedTypeSymbol classSymbol, AttributeParser parser, SemanticModel model, out ModelBuilder modelBuilder) {
        modelBuilder = new(parser, model);

        if (!parser.TryGetAttributeList(classSymbol, ref modelBuilder._diagnostics, out var attrList))
            return false;

        if (attrList.CLI is null)
            return false;

        if (!modelBuilder.TryGetCLI(classSymbol, attrList, out modelBuilder._cliData))
            return false;

        return true;
    }

    private CLITempData? _cliData;

    private record CLITempData(
        string AppName,
        string FullClassName,
        ImmutableArray<string> Usings,
        string? Description,
        int HelpExitCode,
        string? EntryPointName,
        Location ClassLocation
    );

    private readonly ImmutableArray<Option>.Builder _opts = ImmutableArray.CreateBuilder<Option>();
    private readonly ImmutableArray<Command>.Builder _cmds = ImmutableArray.CreateBuilder<Command>();

    public CLIData? MakeCLIData(out ImmutableArray<Diagnostic> diagnostics) {
        if (_cliData is null)
            throw new InvalidOperationException("Trying to get data from uninitialized ModelBuilder");

        var posArgs = ImmutableArray<Argument>.Empty;

        if (!TryBindEntryPoint(out var rootCmd)) {
            diagnostics = _diagnostics.ToImmutable();
            return null;
        }

        if (rootCmd is not null) {
            rootCmd = rootCmd with {
                Name = _cliData.AppName,
                Description = _cliData.Description ?? rootCmd.Description,
                ParentCmdMethodName = null,
                IsRoot = true
            };

            posArgs = rootCmd.Args;
        }

        for (int i = 0; i < _cmds.Count; i++) {
            if (!_cmds[i].IsTopLevel) {
                if (!TryBindParentCmd(_cmds[i], out var newCmd)) {
                    // Are you sure you marked '${_cmds[i].ParentCmdName}' with [Command] ?
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.CouldntFindParentCmd,
                            _cmds[i].BackingSymbol.Location,
                            _cmds[i].ParentCmdMethodName
                        )
                    );

                    diagnostics = _diagnostics.ToImmutable();

                    return null;
                }

                _cmds[i] = newCmd;
            } else if (rootCmd is not null) {
                _cmds[i].ParentCmd = rootCmd;
            }
        }

        diagnostics = _diagnostics.ToImmutable();

        return new CLIData(
            _cliData.AppName,
            _cliData.FullClassName,
            _cliData.Usings,
            rootCmd is null ? null : (rootCmd, posArgs),
            _opts.ToImmutable(),
            _cliData.Description,
            _cmds.ToImmutable(),
            _cliData.HelpExitCode
        );
    }

    public bool TryAdd(ISymbol symbol) {
        if (!_attrParser.TryGetAttributeList(symbol, ref _diagnostics, out var attrList))
            return false;

        switch (attrList.Kind) {
            case MemberKind.None:
            case MemberKind.Useless:
                if (symbol is IMethodSymbol methodSymbol && methodSymbol.Name == _cliData?.EntryPointName)
                    _nonCmdCandidateMethods.Add(methodSymbol);
                return true;
            case MemberKind.Option:
                if (!TryGetOption(symbol, attrList, _topLevelOptLongNames, _topLevelOptAliases, out var opt))
                    return false;
                _opts.Add(opt);
                return true;
            case MemberKind.Command:
                if (symbol is not IMethodSymbol method || !TryGetCommand(method, attrList, out var cmd, false))
                    return false;
                _cmds.Add(cmd);
                return true;
            case MemberKind.CLI:
            case MemberKind.Invalid:
            default:
                return false;
        }
    }

    bool TryGetCLI(INamedTypeSymbol classSymbol, AttributeListInfo attrList, [NotNullWhen(true)] out CLITempData? cliData) {
        cliData = null;

        var fullClassName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var (appName, entryPointName, helpExitCode) = attrList.CLI!;

        if (entryPointName is not null)
            entryPointName = Utils.GetLastNamePart(entryPointName);

        if (!TryGetAllUniqueUsings(classSymbol, out var usings))
            return false;

        string? appDesc = attrList.Description?.Description;

        cliData = new(
            appName,
            fullClassName,
            usings,
            appDesc,
            helpExitCode,
            entryPointName,
            classSymbol.GetDefaultLocation()
        );

        return true;
    }

    bool TryBindEntryPoint(out Command? rootCmd) {
        rootCmd = null;

        if (_cliData is null)
            throw new InvalidOperationException("Tried to bind entry point before processing CLI data");

        var entryPoint = _cliData.EntryPointName;

        if (entryPoint is null)
            return true;

        // technically, we could use classSymbol.GetMembers(entryPointName),
        // but that'd probably be slower
        rootCmd = _cmds.FirstOrDefault(
            cmd => cmd.BackingSymbol.Name == entryPoint
        );

        // if we didn't find a command with the right name
        if (rootCmd is null) {
            if (_nonCmdCandidateMethods.Count < 1) {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.CouldntFindRootCmd,
                        _cliData.ClassLocation,
                        entryPoint
                    )
                );

                return false;
            } else if (_nonCmdCandidateMethods.Count > 1) {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.TooManyRootCmd,
                        _cliData.ClassLocation,
                        entryPoint, _nonCmdCandidateMethods[0].ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), _nonCmdCandidateMethods[1].ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                    )
                );

                return false;
            }

            var method = _nonCmdCandidateMethods[0];

            if (!_attrParser.TryGetAttributeList(method, ref _diagnostics, out var attrList))
                return false;

            if (!TryGetCommand(method, attrList, out rootCmd, isEntryPoint: true))
                return false;
        }

        if (rootCmd.Options.Length != 0) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.OptsInEntryMethod,
                    rootCmd.BackingSymbol.Location, // note: could also be _cliData.ClassLocation
                    rootCmd.BackingSymbol.Name
                )
            );

            return false;
        }

        return true;
    }

    bool TryGetCommand(IMethodSymbol method, AttributeListInfo attrList, [NotNullWhen(true)] out Command? cmd, bool isEntryPoint = false) {
        cmd = null;

        if (method.MethodKind != MethodKind.Ordinary) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CmdMustBeOrdinary,
                    method.GetDefaultLocation(),
                    method.GetErrorName(), method.MethodKind
                )
            );

            return false;
        }

        bool hasExitCode = !method.ReturnsVoid;

        string cmdName;
        string? parentCmdName = null;
        bool inheritOptions = false; // todo: implement inheriting options by name-matching parameters

        if (attrList.Command is not null) {
            cmdName = attrList.Command.CmdName;
        } else if (attrList.SubCommand is not null) {
            cmdName = attrList.SubCommand.CmdName;
            parentCmdName = attrList.SubCommand.ParentCmd;
        } else {
            if (!isEntryPoint) {
                throw new InvalidOperationException("Trying to create a non-entrypoint command with neither a [Command] nor a [SubCommand] attribute");
            }

            cmdName = "";
        }

        string? desc = attrList.Description?.Description;

        bool isValid = true;

        if (!_cmdNames.Add(cmdName)) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CmdNameAlreadyExists,
                    method.GetDefaultLocation(),
                    cmdName, method.GetErrorName()
                )
            );

            isValid = false;
        }

        if (hasExitCode && !SymbolUtils.Equals(method.ReturnType, CommonTypes.INT32)) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CmdMustBeVoidOrInt,
                    method.GetDefaultLocation(),
                    method.GetErrorName(), method.ReturnType.GetNameWithNull()
                )
            );

            isValid = false;
        }

        if (!method.IsStatic) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CmdMustBeStatic,
                    method.GetDefaultLocation(),
                    method.GetErrorName()
                )
            );

            isValid = false;
        }

        if (method.IsGenericMethod) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CmdCantBeGeneric,
                    method.GetDefaultLocation(),
                    method.GetErrorName()
                )
            );

            isValid = false;
        }

        var opts = ImmutableArray.CreateBuilder<Option>(method.Parameters.Length);
        var args = ImmutableArray.CreateBuilder<Argument>(method.Parameters.Length);

        var optNames = new HashSet<string>();
        var optAliases = new HashSet<char>();

        bool hasParams = false;

        foreach (var param in method.Parameters) {
            if (!_attrParser.TryGetAttributeList(param, ref _diagnostics, out var paramAttrList))
                return false;

            if (paramAttrList.Kind == MemberKind.Option) {
                if (!TryGetOption(param, paramAttrList, optNames, optAliases, out var opt))
                    return false;
                opts.Add(opt);
            } else {
                if (!TryGetArg(param, paramAttrList, out var arg))
                    return false;

                args.Add(arg);
                hasParams |= arg.IsParams;
            }
        }

        if (!isValid)
            return false;

        cmd = new Command(
            hasExitCode,
            cmdName,
            desc,
            opts.ToImmutable(),
            args.ToImmutable()
        ) {
            InheritOptions = inheritOptions,
            BackingSymbol = MinimalMethodInfo.FromSymbol(method),
            ParentCmdMethodName = parentCmdName,
            HasParams = hasParams
        };

        return opts.Count + args.Count == method.Parameters.Length;
    }

    bool TryGetArg(IParameterSymbol param, AttributeListInfo attrList, [NotNullWhen(true)] out Argument? arg) {
        arg = null;

        string? defaultVal = null;
        string? argDesc = attrList.Description?.Description;

        foreach (var synRef in param.DeclaringSyntaxReferences) {
            if (synRef.GetSyntax() is not ParameterSyntax paramDec)
                return false;

            if (paramDec.Default is not null) {
                defaultVal = paramDec.Default.Value.ToString();
                break;
            }
        }

        bool isParams = param.IsParams;

        // todo: if type is T[], then treat it as "OneOrMore", if it's T[]? then it's "ZeroOrMore"

        // todo: allow custom 'params' args (e.g. params FileInfo[] files)
        // doesn't really matter rn, but it'd be nice to be able to use any type for params,
        // because i'd really like to just say 'params FileInfo[] files' instead of having
        // to transform/validate it myself
        if (isParams && (param.Type is not IArrayTypeSymbol paramArrayType || !SymbolUtils.Equals(paramArrayType.ElementType, CommonTypes.STR))) {
            _diagnostics.Add(
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
            parser = ParserInfo.StringIdentity;
        } else {
            if (attrList.ParseWith is null) {
                if (!_parserFinder.TryFindParserForType(param.Type, param.GetDefaultLocation(), out parser)) {
                    _parserFinder.AddDiagnosticsIfInvalid(parser);
                    return false;
                }
            } else {
                if (!_parserFinder.TryGetParserFromName(attrList.ParseWith, param.Type, out parser)) {
                    _parserFinder.AddDiagnosticsIfInvalid(parser);
                    return false;
                }
            }

            if (attrList.ValidateWith is not null && !_validatorFinder.TryGetValidator(attrList.ValidateWith, param.Type, out validator))
                return false;
        }

        arg = new Argument(
            MinimalTypeInfo.FromSymbol(param.Type),
            new Desc(
                param.Name,
                argDesc
            ),
            parser,
            defaultVal
        ) {
            BackingSymbol = MinimalParameterInfo.FromSymbol(param),
            Validator = validator,
            IsParams = isParams
        };

        return true;
    }

    bool TryGetOption(ISymbol symbol, AttributeListInfo attrInfo, HashSet<string> optNames, HashSet<char> optAliases, [NotNullWhen(true)] out Option? opt) {
        opt = null;

        static ITypeSymbol GetTypeForSymbol(ISymbol symbol)
            => symbol switch {
                IFieldSymbol field => field.Type,
                IPropertySymbol prop => prop.Type,
                IParameterSymbol param => param.Type,
                _ => throw new ArgumentException("Can't get .Type for " + symbol.GetType().Name + "s", nameof(symbol))
            };

        string longName = attrInfo.Option!.LongName;
        char shortName = attrInfo.Option!.Alias;
        string? descStr = attrInfo.Description?.Description;
        string? argName = attrInfo.Option!.ArgName;

        bool isValid = true;

        if (!optNames.Add(longName)) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.OptNameAlreadyExists,
                    symbol.GetDefaultLocation(),
                    longName, symbol.GetErrorName()
                )
            );

            isValid = false;
        }

        if (shortName != default && !optAliases.Add(shortName)) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.OptAliasAlreadyExists,
                    symbol.GetDefaultLocation(),
                    shortName, symbol.GetErrorName()
                )
            );

            isValid = false;
        }

        if (!symbol.IsStatic && symbol is not IParameterSymbol) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.OptMustBeStatic,
                    symbol.GetDefaultLocation(),
                    symbol.GetErrorName()
                )
            );

            isValid = false;
        }

        // todo: check that default value for fields and props is valid outside of scope
        // and maybe transform it if it's invalid (e.g. by qualifying names) ?
        // we could use ISymbol.GetMinimalQualifiedName(model, position, etc)
       var defaultVal = GetDefaultValueForSymbol(symbol, ref isValid);

        if (!isValid)
            return false;

        MinimalSymbolInfo backingSymbol
                = symbol is IParameterSymbol paramSymbol
                    ? MinimalParameterInfo.FromSymbol(paramSymbol)
                    : MinimalMemberInfo.FromSymbol(symbol);

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

        // if it's a flag
        if (SymbolUtils.Equals(type, CommonTypes.BOOL)) {
            opt = new Flag(
                new FlagDesc(longName, shortName, descStr),
                parser,
                backingSymbol,
                defaultVal?.ToString()
            ) {
                Validator = validator
            };

            return true;
        }

        opt = new Option(
            MinimalTypeInfo.FromSymbol(type),
            new OptDesc(
                longName, shortName, argName ?? symbol.Name, descStr
            ),
            parser,
            backingSymbol,
            defaultVal?.ToString()
        ) {
            Validator = validator
        };

        return true;
    }

    public bool TryBindParentCmd(Command sub, out Command newCmd) {
        newCmd = sub;

        for (int i = 0; i < _cmds.Count; i++) {
            var cmd = _cmds[i];

            if (sub.ParentCmdMethodName == cmd.BackingSymbol.Name) {
                newCmd = sub with { ParentCmd = cmd, ParentCmdMethodName = cmd.Name };
                return true;
            }
        }

        return false;
    }

    public bool TryGetAllUniqueUsings(INamedTypeSymbol classSymbol, out ImmutableArray<string> usings) {
        usings = ImmutableArray<string>.Empty;

        var nodes = classSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).Cast<ClassDeclarationSyntax>();

        var usingList = new List<string>();

        foreach (var node in nodes) {
            if (!TryGetUsings(node, out var newUsings))
                return false;

            usingList.AddRange(newUsings);
        }

        usings = usingList.Distinct().ToImmutableArray();
        return true;
    }

    public bool TryGetUsings(ClassDeclarationSyntax classDec, out string[] usings) {
        usings = Array.Empty<string>();

        var parent = classDec.Parent;

        // keep moving out of nested classes
        while (parent is
                not null
                and not NamespaceDeclarationSyntax
                and not FileScopedNamespaceDeclarationSyntax
                and not CompilationUnitSyntax) { }

        var usingsSyntaxList = new SyntaxList<UsingDirectiveSyntax>();

        if (parent is CompilationUnitSyntax unit) {
            usingsSyntaxList = unit.Usings;
        } else if (parent is BaseNamespaceDeclarationSyntax ns) {
            usingsSyntaxList = ns.Usings;
        }

        usings = usingsSyntaxList.Select(u => u.Name.ToString()).ToArray();

        return true;
    }

    ExpressionSyntax? GetDefaultValueForSymbol(ISymbol symbol, ref bool isValid) {
        switch (symbol) {
            case IFieldSymbol field:
                if (field.IsReadOnly || field.IsConst) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.NonWritableOptField,
                            symbol.GetDefaultLocation(),
                            symbol.GetErrorName()
                        )
                    );

                    isValid = false;
                }

                var varDec
                    = symbol.DeclaringSyntaxReferences
                        .Select(refs => refs.GetSyntax())
                        .OfType<VariableDeclaratorSyntax>()
                        .FirstOrDefault(vds => vds.Initializer is not null);

                if (varDec is not null)
                    return varDec.Initializer!.Value;

                break;
            case IPropertySymbol prop: {
                if (prop.IsReadOnly || prop.SetMethod!.DeclaredAccessibility is not Accessibility.Public or Accessibility.NotApplicable) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.NonWritableOptProp,
                            symbol.GetDefaultLocation(),
                            symbol.GetErrorName()
                        )
                    );

                    isValid = false;
                }

                var propDec
                    = symbol.DeclaringSyntaxReferences
                        .Select(refs => refs.GetSyntax())
                        .OfType<PropertyDeclarationSyntax>()
                        .FirstOrDefault(pds => pds.Initializer is not null);

                if (propDec is not null)
                    return propDec.Initializer!.Value;

                break;
            }
            case IParameterSymbol parameterSymbol: {
                var paramDec
                    = symbol.DeclaringSyntaxReferences
                        .Select(refs => refs.GetSyntax())
                        .OfType<ParameterSyntax>()
                        .FirstOrDefault(ps => ps.Default is not null);

                if (paramDec is not null)
                    return paramDec.Default!.Value;

                break;
            }
            default:
                throw new ArgumentException("Symbol must an IFieldSymbol, IPropertySymbol, IParameterSymbol, but it was " + symbol.GetType().Name, nameof(symbol));
        }

        return null;
    }
}