using Recline.Generator.Model;

namespace Recline.Generator;

internal class ModelBuilder
{
    private ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    public ImmutableArray<Diagnostic> GetDiagnostics() => _diagnostics.ToImmutable();

    private AttributeParser _attribParser;

    public ModelBuilder(AttributeParser parser) => this._attribParser = parser;

    public bool TryGetEntryPoint(string entryPointName, Command[] cmds, IEnumerable<IMethodSymbol> classMethods, Location attributeLocation, out Command rootCmd) {
        rootCmd = cmds.FirstOrDefault(
            cmd => cmd.BackingSymbol.Name == entryPointName
        );

        if (rootCmd is null) {
            // technically, we could use classSymbol.GetMembers(entryPointName),
            // but that'd probably be slower

            // we don't actually have to use .ToArray here, we could just try to iterate
            // and error if we can't call MoveNext() exactly once. But that would be
            // an incredibly small optimisation

            var candidates = classMethods.Where(
                m => m.Name == entryPointName
            ).ToArray();

            if (candidates.Length < 1) {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.CouldntFindRootCmd,
                        attributeLocation,
                        entryPointName
                    )
                );

                return false;
            } else if (candidates.Length > 1) {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.TooManyRootCmd,
                        attributeLocation,
                        entryPointName, candidates[0].ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), candidates[1].ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                    )
                );

                return false;
            }

            var method = candidates[0];

            if (!TryGetCommand(method, out rootCmd!, isEntryPoint: true))
                return false;
        }

        if (rootCmd is null)
            throw new Exception("wtf??");

        if (rootCmd.Options.Length != 0) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.OptsInEntryMethod,
                    attributeLocation, // FIXME: should be rootCmd.BackingSymbol.Location
                    rootCmd.BackingSymbol.Name
                )
            );

            return false;
        }

        return true;
    }

    public bool TryGetCommand(IMethodSymbol method, out Command? cmd, bool isEntryPoint = false) {
        cmd = null;
        var attributes = method.GetAttributes();

        bool hasExitCode = !method.ReturnsVoid;

        if (attributes.IsDefaultOrEmpty) {
            return true;
        }

        string cmdName = "";
        string? parentCmdName = null;
        string? desc = null;
        bool inheritOptions = false;

        bool hadCmdAttr = false;
        bool hadOptAttr = false;
        bool isValidCmd = true;

        foreach (var attr in attributes) {
            switch (attr.AttributeClass?.Name) {
                case Ressources.OptAttribName: // in case this is an option method, abort
                    hadOptAttr = true;
                    break;
                case Ressources.DescAttribName: {
                    if (!_attribParser.TryParseDescAttrib(attr, out var descAttr))
                        return false;

                    desc = descAttr.Desc;
                    break;
                }
                case Ressources.CmdAttribName: {
                    if (hadCmdAttr) {
                        _diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.BothCmdAndSubCmd,
                                method.Locations[0],
                                method.GetErrorName()
                            )
                        );

                        isValidCmd = false;
                    }

                    hadCmdAttr = true;

                    if (!_attribParser.TryParseCmdAttrib(attr, out var cmdAttr))
                        return false;

                    cmdName = cmdAttr.CmdName;

                    if (string.IsNullOrWhiteSpace(cmdName)) {
                        _diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.EmptyCmdName,
                                attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None
                            )
                        );

                        isValidCmd = false;
                    }

                    break;
                }
                case Ressources.SubCmdAttribName: {
                    if (hadCmdAttr) {
                        _diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.BothCmdAndSubCmd,
                                method.Locations[0],
                                method.GetErrorName()
                            )
                        );

                        isValidCmd = false;
                    }

                    hadCmdAttr = true;

                    if (!_attribParser.TryParseSubCmdAttrib(attr, out var subCmdAttr))
                        return false;

                    (cmdName, parentCmdName) = subCmdAttr;

                    if (string.IsNullOrWhiteSpace(cmdName)) {
                        _diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.EmptyCmdName,
                                attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None
                            )
                        );

                        isValidCmd = false;
                    }

                    if (parentCmdName is not null) // the other case will be handled by BindParentCmd()
                        parentCmdName = Utils.GetLastNamePart(parentCmdName.AsSpan());

                    break;
                }
            }
        }

        if (!hadCmdAttr && !isEntryPoint)
                return true;

        if (hadOptAttr) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.BothOptAndCmd,
                    method.Locations[0],
                    method.GetErrorName()
                )
            );

            isValidCmd = false;
        }

        // Once we know we're dealing with a command, we can validate

        if (method.MethodKind != MethodKind.Ordinary) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CmdMustBeOrdinary,
                    method.Locations[0],
                    method.GetErrorName(), method.MethodKind
                )
            );

            isValidCmd = false;
        }

        if (hasExitCode && !Utils.Equals(method.ReturnType, Utils.INT32)) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CmdMustBeVoidOrInt,
                    method.Locations[0],
                    method.GetErrorName(), method.ReturnType.GetNameWithNull()
                )
            );

            isValidCmd = false;
        }

        if (!method.IsStatic) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CmdMustBeStatic,
                    method.Locations[0],
                    method.GetErrorName()
                )
            );

            isValidCmd = false;
        }

        if (method.IsGenericMethod) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CmdCantBeGeneric,
                    method.Locations[0],
                    method.GetErrorName()
                )
            );

            isValidCmd = false;
        }

        if (!isValidCmd)
            return false;

        var opts = new List<Option>(method.Parameters.Length);
        var args = new List<Argument>(method.Parameters.Length);

        bool hasParams = false;

        foreach (var param in method.Parameters) {
            if (!TryGetOptions(param, out var opt))
                return false;

            if (opt is not null) {
                opts.Add(opt);
            } else {
                if (!TryGetArg(param, out var arg))
                    return false;

                if (arg is null)
                    throw new Exception("Parameter " + param.ToDisplayString() + " is neither an option nor an argument...");

                args.Add(arg);
                hasParams |= arg.IsParams;
            }
        }

        cmd = new Command(
            hasExitCode,
            cmdName,
            desc,
            opts.ToArray(),
            args.ToArray()
        ) {
            InheritOptions = inheritOptions,
            BackingSymbol = MinimalMethodInfo.FromSymbol(method),
            ParentSymbolName = parentCmdName,
            HasParams = hasParams
        };

        return opts.Count + args.Count == method.Parameters.Length;
    }

    public bool TryGetArg(IParameterSymbol param, out Argument? arg) {
        arg = null;

        string? defaultVal = null;
        string? argDesc = null;

        var attributes = param.GetAttributes();

        foreach (var attr in attributes) {
            if (attr.AttributeClass?.Name == Ressources.OptAttribName)
                return true;

            if ((attr.AttributeClass?.Name) == Ressources.DescAttribName) {
                if (attr.ConstructorArguments.Length < 1)
                    return false;

                argDesc = (string?)attr.ConstructorArguments[0].Value;

                if (argDesc is null) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.DescCantBeNull,
                            param.Locations[0],
                            param.Name
                        )
                    );

                    return false;
                }

                break;
            }
        }

        foreach (var syntax in param.DeclaringSyntaxReferences.Select(r => r.GetSyntax())) {
            if (syntax is not ParameterSyntax paramDec)
                return false;

            if (paramDec.Default is not null) {
                defaultVal = paramDec.Default.Value.ToString();
                break;
            }
        }

        arg = new Argument(
            MinimalTypeInfo.FromSymbol(param.Type),
            new Desc(
                param.Name,
                argDesc
            ),
            defaultVal
        ) {
            BackingSymbol = MinimalParameterInfo.FromSymbol(param),
            IsParams = param.IsParams
        };

        return true;
    }

    public bool TryGetOptions(ISymbol symbol, out Option? opt) {
        opt = null;
        var attributes = symbol.GetAttributes();

        if (attributes.IsDefaultOrEmpty) {
            return true;
        }

        string longName = "";
        char shortName = '\0';
        string? argName = null;
        string? desc = null;

        bool hadOptAttrib = false;
        bool hadCmdAttr = false;

        bool isValidOpt = true;

        foreach (var attr in attributes) {

            switch (attr.AttributeClass?.Name) {
                case Ressources.CmdAttribName:
                case Ressources.SubCmdAttribName:
                    hadCmdAttr = true;
                    break;
                case Ressources.OptAttribName:
                    if (hadOptAttrib)
                        return false;

                    hadOptAttrib = true;

                    if (!_attribParser.TryParseOptAttrib(attr, out var optAttr))
                        return false;

                    (longName, shortName, _) = optAttr;

                    if (string.IsNullOrWhiteSpace(longName)) {
                        _diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.EmptyOptLongName,
                                attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None
                            )
                        );

                        isValidOpt = false;
                    }

                    if (char.IsWhiteSpace(shortName)) { // '\0' is not whitespace :P
                        _diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.EmptyOptShortName,
                                attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None
                            )
                        );

                        isValidOpt = false;
                    }

                    if (optAttr.ArgName is not null)
                        argName = optAttr.ArgName;

                    break;
                case Ressources.DescAttribName:
                    if (!_attribParser.TryParseDescAttrib(attr, out var descAttr))
                        return false;

                    desc = descAttr.Desc;
                    break;
                default: // TODO: warn if cli attribute
                    continue;
            }
        }

        if (!hadOptAttrib)
            return true;

        if (hadCmdAttr) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.BothOptAndCmd,
                    symbol.Locations[0],
                    symbol.GetErrorName()
                )
            );

            return false; // no point in trying to parse this lol
        }

        if (!symbol.IsStatic && symbol is not IParameterSymbol) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.OptMustBeStatic,
                    symbol.Locations[0],
                    symbol.GetErrorName()
                )
            );

            isValidOpt = false;
        }

        if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsReadOnly) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.OptMustNotBeReadonly,
                    symbol.Locations[0],
                    symbol.GetErrorName()
                )
            );

            isValidOpt = false;
        }

        ITypeSymbol type;
        string? defaultVal = null;

        switch (symbol) {
            case IFieldSymbol field:
                if (field.IsReadOnly || field.IsConst) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.NonWritableOptField,
                            symbol.Locations[0],
                            symbol.GetErrorName()
                        )
                    );

                    isValidOpt = false;
                }

                type = field.Type;

                var varDec = (VariableDeclaratorSyntax)symbol.DeclaringSyntaxReferences[0].GetSyntax();

                if (varDec.Initializer is not null)
                    defaultVal = varDec.Initializer.Value.ToString();

                break;
            case IPropertySymbol prop: {
                if (prop.IsReadOnly || prop.SetMethod!.DeclaredAccessibility is not Accessibility.Public or Accessibility.NotApplicable) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.NonWritableOptProp,
                            symbol.Locations[0],
                            symbol.GetErrorName()
                        )
                    );

                    isValidOpt = false;
                }

                type = prop.Type;

                var propDec = (PropertyDeclarationSyntax)symbol.DeclaringSyntaxReferences[0].GetSyntax();

                if (propDec.Initializer is not null)
                    defaultVal = propDec.Initializer.Value.ToString();


                // TODO: check that defaultVal is valid outside of the containing class
                // and maybe transform it (when possible) if not (e.g. qualifying names) ?
                // we could use ISymbol.GetMinimalQualifiedName(model, position, etc)

                break;
            }
            case IParameterSymbol parameterSymbol: {
                type = parameterSymbol.Type;

                var paramDec = (ParameterSyntax)parameterSymbol.DeclaringSyntaxReferences[0].GetSyntax();

                if (paramDec.Default is not null)
                    defaultVal = paramDec.Default.Value.ToString();

                break;
            }
            case IMethodSymbol methodSymbol: {
                type = methodSymbol.ReturnType;

                bool needsAutoHandling = !methodSymbol.ReturnsVoid;

                isValidOpt = true;

                if (needsAutoHandling
                    && !Utils.Equals(type, Utils.BOOL)
                    && !Utils.Equals(type, Utils.INT32)
                    && !Utils.Equals(type, Utils.STR)
                    && !Utils.Equals(type, Utils.EXCEPT)
                ) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.OptMethodWrongReturnType,
                            methodSymbol.Locations[0],
                            methodSymbol.GetErrorName(), methodSymbol.ReturnType.GetErrorName()
                        )
                    );

                    isValidOpt = false;
                }

                if (!methodSymbol.IsStatic) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.OptMustBeStatic,
                            methodSymbol.Locations[0],
                            methodSymbol.GetErrorName()
                        )
                    );

                    isValidOpt = false;
                }

                if (methodSymbol.IsGenericMethod) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.OptMethodCantBeGeneric,
                            methodSymbol.Locations[0],
                            methodSymbol.GetErrorName()
                        )
                    );

                    isValidOpt = false;
                }

                if (methodSymbol.Parameters.Length > 1) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.OptMethodTooManyArguments,
                            methodSymbol.Locations[0],
                            methodSymbol.GetErrorName(), methodSymbol.Parameters.Length
                        )
                    );

                    isValidOpt = false;
                }

                var rawArgParam = methodSymbol.Parameters.FirstOrDefault();

                if (rawArgParam is not null && !Utils.Equals(rawArgParam.Type, Utils.STR)) {
                    _diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.OptMethodWrongParamType,
                            rawArgParam.Locations[0],
                            rawArgParam.GetErrorName(), rawArgParam.Type.GetErrorName()
                        )
                    );

                    isValidOpt = false;
                }

                if (!isValidOpt)
                    return false;

                if (argName is null)
                    argName = rawArgParam?.Name ?? "";

                opt = new MethodOption(
                    new OptDesc(
                        longName,
                        shortName,
                        argName,
                        desc
                    ),
                    needsAutoHandling
                ) {
                    BackingSymbol = MinimalMethodInfo.FromSymbol(methodSymbol),
                    IsSwitch = rawArgParam is null,
                };

                if (opt.IsSwitch) {
                    opt = opt with {
                        Desc = new SwitchDesc(longName, shortName, desc)
                    };
                }

                return true;
            }
            default:
                throw new ArgumentException("Symbol must an IFieldSymbol, IPropertySymbol, IParameterSymbol or IMethodSymbol, but it was " + symbol.GetType().Name, nameof(symbol));
        }

        if (!isValidOpt)
            return false;

        opt = new Option(
            MinimalTypeInfo.FromSymbol(type),
            new OptDesc(
                longName, shortName, argName ?? symbol.Name, desc
            ),
            defaultVal
        ) {
            BackingSymbol
                = symbol is IParameterSymbol paramSymbol
                    ? MinimalParameterInfo.FromSymbol(paramSymbol)
                    : MinimalMemberInfo.FromSymbol(symbol)
        };

        if (opt.IsSwitch) {
            opt = opt with {
                Desc = new SwitchDesc(longName, shortName, desc)
            };
        }

        return true;
    }

    public bool TryBindParentCmd(Command sub, Command[] cmds, out Command newCmd) {
        newCmd = null!;

        for (int i = 0; i < cmds.Length; i++) {
            var cmd = cmds[i];

            if (sub.ParentSymbolName == cmd.BackingSymbol.Name) {
                newCmd = sub with { ParentCmd = cmd, ParentSymbolName = cmd.Name };
                break;
            }
        }

        if (newCmd is not null)
            return true;

        _diagnostics.Add(
            Diagnostic.Create(
                Diagnostics.CouldntFindParentCmd,
                Location.None, // find right location
                sub.Name, sub.ParentSymbolName
            )
        );

        return false;
    }

    public bool TryGetAllUniqueUsings(INamedTypeSymbol classSymbol, out string[] usings) {
        usings = Array.Empty<string>();
        var nodes = classSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).Cast<ClassDeclarationSyntax>();

        var usingList = new List<string>();

        foreach (var node in nodes) {
            if (!TryGetUsings(node, out var newUsings))
                return false;

            usingList.AddRange(newUsings);
        }

        usings = usingList.Distinct().ToArray();
        return true;
    }

    public bool TryGetUsings(ClassDeclarationSyntax classDec, out string[] usings) {
        usings = Array.Empty<string>();

        var parent = classDec.Parent;

        // keep moving out of nested classes
        while (parent != null &&
                parent is not NamespaceDeclarationSyntax
                && parent is not FileScopedNamespaceDeclarationSyntax
                && parent is not CompilationUnitSyntax) { }

        var usingsSyntaxList = new SyntaxList<UsingDirectiveSyntax>();

        if (parent is CompilationUnitSyntax unit) {
            usingsSyntaxList = unit.Usings;
        } else if (parent is BaseNamespaceDeclarationSyntax ns) {
            usingsSyntaxList = ns.Usings;
        }

        usings = usingsSyntaxList.Select(u => u.Name.ToString()).ToArray();

        return true;
    }
}