using System.Collections.Immutable;

using CLIGen;
using CLIGen.Generator;
using CLIGen.Generator.Model;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static CLIGen.Generator.Ressources;
using System.Diagnostics;

namespace CLIGen.Generator;

public partial class MainGenerator : IIncrementalGenerator
{
    static bool TryGetEntryPointCommand(IMethodSymbol method, SemanticModel model, out Command cmd) {
        cmd = null!;
        var attributes = method.GetAttributes();

        if (!method.IsStatic | method.IsGenericMethod)
            return false;

        bool hasExitCode = !method.ReturnsVoid;

        if (hasExitCode && !Utils.Equals(method.ReturnType, Utils.INT32))
            return false;

        string cmdName = "";
        string? parentCmdName = null;
        string? desc = null;
        bool inheritOptions = false;

        foreach (var attr in attributes) {
            switch (attr.AttributeClass?.Name) {
                case Ressources.OptAttribName: // an entry point can't be marked option
                    return false;
                case Ressources.DescAttribName: {
                    if (!AttributeParser.TryParseDescAttrib(attr, out var descAttr))
                        return false;

                    desc = descAttr.Desc;
                    break;
                }
                case Ressources.CmdAttribName: {
                    if (!AttributeParser.TryParseCmdAttrib(attr, out var cmdAttr))
                        return false;

                    cmdName = cmdAttr.CmdName;

                    break;
                }
                case Ressources.SubCmdAttribName: {
                    if (!AttributeParser.TryParseSubCmdAttrib(attr, out var subCmdAttr))
                        return false;

                    (cmdName, parentCmdName, inheritOptions) = subCmdAttr;

                    parentCmdName = Utils.GetLastNamePart(parentCmdName.AsSpan());

                    break;
                }
                default:
                    continue;
            }
        }

        var opts = new List<Option>(method.Parameters.Length);

        foreach (var optParam in method.Parameters) {
            if (!TryGetOptions(optParam, model, out var opt))
                return false;

            if (opt is not null)
                opts.Add(opt);
        }

        var args = new List<Argument>(method.Parameters.Length);

        bool hasParams = false;

        foreach (var argParam in method.Parameters) {
            if (!TryGetArg(argParam, model, out var arg, out var isParams))
                return false;

            if (arg is not null) {
                args.Add(arg);
                hasParams |= isParams;
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

    static bool TryGetCommand(IMethodSymbol method, SemanticModel model, out Command? cmd) {
        cmd = null;
        var attributes = method.GetAttributes();

        bool hasExitCode = !method.ReturnsVoid;

        if (attributes.IsDefaultOrEmpty || method.MethodKind != MethodKind.Ordinary) {
            return true;
        }

        string cmdName = "";
        string? parentCmdName = null;
        string? desc = null;
        bool inheritOptions = false;

        bool hadCmdAttr = false;

        foreach (var attr in attributes) {
            switch (attr.AttributeClass?.Name) {
                case Ressources.OptAttribName: // in case this is an option method, abort
                    return !hadCmdAttr;
                case Ressources.DescAttribName: {
                    if (!AttributeParser.TryParseDescAttrib(attr, out var descAttr))
                        return false;

                    desc = descAttr.Desc;
                    break;
                } case Ressources.CmdAttribName: {
                    if (hadCmdAttr)
                        return false;

                    hadCmdAttr = true;

                    if (!AttributeParser.TryParseCmdAttrib(attr, out var cmdAttr))
                        return false;

                    cmdName = cmdAttr.CmdName;

                    break;
                } case Ressources.SubCmdAttribName: {
                    if (hadCmdAttr)
                        return false;

                    hadCmdAttr = true;

                    if (!AttributeParser.TryParseSubCmdAttrib(attr, out var subCmdAttr))
                        return false;

                    (cmdName, parentCmdName, inheritOptions) = subCmdAttr;

                    break;
                } default:
                    continue;
            }
        }

        if (!hadCmdAttr)
            return true;

        // Once we know we're dealing with a command, we can validate

        if (hasExitCode && !Utils.Equals(method.ReturnType, Utils.INT32))
            return false;

        if (!method.IsStatic | method.IsGenericMethod)
            return false;

        var opts = new List<Option>(method.Parameters.Length);

        foreach (var optParam in method.Parameters) {
            if (!TryGetOptions(optParam, model, out var opt))
                return false;

            if (opt is not null)
                opts.Add(opt);
        }

        var args = new List<Argument>(method.Parameters.Length);

        bool hasParams = false;

        foreach (var argParam in method.Parameters) {
            if (!TryGetArg(argParam, model, out var arg, out var isParams))
                return false;

            if (arg is not null) {
                args.Add(arg);
                hasParams |= isParams;
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

    static bool TryGetArg(IParameterSymbol param, SemanticModel model, out Argument? arg, out bool hasParams) {
        arg = null;
        hasParams = false;

        ExpressionSyntax? defaultVal = null;
        string? argDesc = null;

        var attributes = param.GetAttributes();

        foreach (var attr in attributes) {
            if (attr.AttributeClass?.Name == Ressources.OptAttribName)
                return true;

            if (attr.AttributeClass?.Name != Ressources.DescAttribName)
                continue;

            if (attr.ConstructorArguments.Length < 1)
                return false;

            argDesc = (string?)attr.ConstructorArguments[0].Value;
        }

        if (param.IsParams)
            hasParams = true;

        foreach (var syntax in param.DeclaringSyntaxReferences.Select(r => r.GetSyntax())) {
            if (syntax is not ParameterSyntax paramDec)
                return false;

            if (paramDec.Default is not null) {
                defaultVal = paramDec.Default.Value;
                break;
            }
        }

        arg = new Argument(
            param.Type,
            new Desc(
                param.Name,
                argDesc
            ),
            defaultVal
        ) {
            BackingSymbol = MinimalParameterInfo.FromSymbol(param),
            IsParams = hasParams
        };

        return true;
    }

    static bool TryGetOptions(ISymbol symbol, SemanticModel model, out Option? opt) {
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

        foreach (var attr in attributes) {

            switch (attr.AttributeClass?.Name) {
                case Ressources.OptAttribName:
                    if (hadOptAttrib)
                        return false;

                    hadOptAttrib = true;

                    if (!AttributeParser.TryParseOptAttrib(attr, out var optAttr))
                        return false;

                    (longName, shortName, _) = optAttr;

                    if (optAttr.ArgName is not null)
                        argName = optAttr.ArgName;

                    break;
                case Ressources.DescAttribName:
                    if (!AttributeParser.TryParseDescAttrib(attr, out var descAttr))
                        return false;

                    desc = descAttr.Desc;
                    break;
                default: // TODO: warn if cli attribute
                    continue;
            }
        }

        if (!hadOptAttrib)
            return true;

        ITypeSymbol type;
        ExpressionSyntax? defaultVal = null;

        switch (symbol) {
            case IFieldSymbol:
            case IPropertySymbol: {
                type = (symbol.Kind is SymbolKind.Field)
                    ? ((IFieldSymbol)symbol).Type
                    : ((IPropertySymbol)symbol).Type;

                foreach (var syntax in symbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax())) {
                    if (syntax is PropertyDeclarationSyntax propDec) {
                        if (propDec.Initializer is not null) {
                            defaultVal = propDec.Initializer.Value;
                            break;
                        }
                    } else if (syntax is VariableDeclaratorSyntax fieldDec) {
                        // IFieldSymbol are not declared by FieldDeclarationSyntax...
                        if (fieldDec.Initializer is not null) {
                            defaultVal = fieldDec.Initializer!.Value;
                            break;
                        }
                    } else {
                        return false;
                    }
                }

                // TODO: check that defaultVal is valid outside of the containing class
                // and maybe transform it (when possible) if not (e.g. qualifying names) ?
                // we could use ISymbol.GetMinimalQualifiedName(model, position, etc)

                break;
            }
            case IParameterSymbol parameterSymbol: {
                type = parameterSymbol.Type;

                foreach (var syntax in parameterSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax())) {
                    if (syntax is not ParameterSyntax paramDec)
                        return false;

                    if (paramDec.Default is not null) {
                        defaultVal = paramDec.Default.Value;
                        break;
                    }
                }

                break;
            }
            case IMethodSymbol methodSymbol: {
                type = methodSymbol.ReturnType;

                bool needsAutoHandling = !methodSymbol.ReturnsVoid;

                if (needsAutoHandling
                    && !Utils.Equals(type, Utils.BOOL)
                    && !Utils.Equals(type, Utils.INT32)
                    && !Utils.Equals(type, Utils.STR)
                    && !Utils.Equals(type, Utils.EXCEPT)
                ) {
                    return false;
                }

                if (!methodSymbol.IsStatic | methodSymbol.IsGenericMethod)
                    return false;

                if (methodSymbol.Parameters.Length > 1)
                    return false;

                var rawArgParam = methodSymbol.Parameters.FirstOrDefault();

                if (rawArgParam is not null && !Utils.Equals(rawArgParam.Type, Utils.STR))
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
                return false;
        }

        opt = new Option(
            type,
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

    static bool TryBindParentCmd(Command sub, Command[] cmds, out Command newCmd) {
        newCmd = null!;

        for (int i = 0; i < cmds.Length; i++) {
            var cmd = cmds[i];

            if (sub.ParentSymbolName == cmd.BackingSymbol.Name) {
                newCmd = sub with { ParentCmd = cmd, ParentSymbolName = cmd.Name };
                break;
            }
        }

        return newCmd is not null;
    }

    static bool TryGetAllUniqueUsings(INamedTypeSymbol classSymbol, out string[] usings) {
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

    static bool TryGetUsings(ClassDeclarationSyntax classDec, out string[] usings) {
        usings = Array.Empty<string>();

        var parent = classDec.Parent;

        // keep moving out of nested classes
        while (parent != null &&
                parent is not NamespaceDeclarationSyntax
                && parent is not FileScopedNamespaceDeclarationSyntax
                && parent is not CompilationUnitSyntax) {}

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