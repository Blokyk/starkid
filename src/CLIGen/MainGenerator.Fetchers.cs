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
    static bool TryGetCommand(IMethodSymbol method, SemanticModel model, out Command? cmd) {
        var attributes = method.GetAttributes();

        cmd = null;

        bool hasExitCode = !method.ReturnsVoid;

        if (attributes.IsDefaultOrEmpty || method.MethodKind != MethodKind.Ordinary) {
            return true;
        }

        string cmdName = "";
        string? parentCmd = null;
        string? desc = null;
        bool inheritOptions = false;

        bool hadCmdAttr = false;

        foreach (var attr in attributes) {
            switch (attr.AttributeClass?.Name) {
                case Ressources.OptAttribName: // in case this is an option method, abort
                    return true;
                case Ressources.DescAttribName: {
                    desc = (string?)attr.ConstructorArguments[0].Value;
                    break;
                } case Ressources.CmdAttribName: {
                    if (hadCmdAttr)
                        return false;

                    hadCmdAttr = true;

                    if (!Utils.TryParseCmdAttrib(attr, out var cmdAttr))
                        return false;

                    cmdName = cmdAttr.CmdName;

                    break;
                } case Ressources.SubCmdAttribName: {
                    if (hadCmdAttr)
                        return false;

                    hadCmdAttr = true;

                    if (!Utils.TryParseSubCmdAttrib(attr, out var subCmdAttr))
                        return false;

                    (cmdName, parentCmd, inheritOptions) = subCmdAttr;

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

        foreach (var argParam in method.Parameters) {
            if (!TryGetArg(argParam, model, out var arg))
                return false;

            if (arg is not null)
                args.Add(arg);
        }

        cmd = new Command(
            hasExitCode,
            cmdName,
            desc,
            parentCmd,
            opts,
            args
        ) {
            InheritOptions = inheritOptions,
            BackingSymbol = method
        };

        return opts.Count + args.Count == method.Parameters.Length;
    }

    static bool TryGetArg(IParameterSymbol param, SemanticModel model, out Argument? arg) {
        arg = null;

        ExpressionSyntax? defaultVal = null;
        string? argDesc = null;

        var argAttributes = param.GetAttributes();

        foreach (var attr in argAttributes) {
            if (attr.AttributeClass?.Name != Ressources.DescAttribName)
                continue;

            if (attr.ConstructorArguments.Length < 1)
                return false;

            argDesc = (string?)attr.ConstructorArguments[0].Value;
        }

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
            param.Name,
            argDesc,
            defaultVal
        );

        return true;
    }

    static bool TryGetOptions(ISymbol member, SemanticModel model, out Option? opt) {
        var attributes = member.GetAttributes();

        opt = null;

        if (attributes.IsDefaultOrEmpty) {
            return true;
        }

        string longName = "";
        char shortName = '\0';
        string argName = member.Name;
        string? desc = null;

        bool hadOptAttrib = false;

        foreach (var attr in attributes) {

            switch (attr.AttributeClass?.Name) {
                case Ressources.OptAttribName:
                    if (hadOptAttrib)
                        return false;

                    hadOptAttrib = true;

                    if (!Utils.TryParseOptAttrib(attr, out var optAttr))
                        return false;

                    (longName, shortName, _) = optAttr;

                    if (optAttr.ArgName is not null)
                        argName = optAttr.ArgName;

                    break;
                case Ressources.DescAttribName:
                    if (!Utils.TryParseDescAttrib(attr, out var descAttr))
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

        switch (member) {
            case IFieldSymbol:
            case IPropertySymbol: {
                type = (member.Kind is SymbolKind.Field)
                    ? ((IFieldSymbol)member).Type
                    : ((IPropertySymbol)member).Type;

                foreach (var syntax in member.DeclaringSyntaxReferences.Select(r => r.GetSyntax())) {
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

                if (needsAutoHandling && !Utils.Equals(type, Utils.BOOL))
                    return false;

                if (!methodSymbol.IsStatic | methodSymbol.IsGenericMethod)
                    return false;

                if (methodSymbol.Parameters.Length != 1)
                    return false;

                var rawArgParam = methodSymbol.Parameters[0];

                if (!Utils.Equals(rawArgParam.Type, Utils.STR))
                    return false;

                if (argName == "")
                    argName = rawArgParam.Name;

                opt = new MethodOption(
                    new OptDesc(
                        longName,
                        shortName,
                        argName,
                        desc
                    ),
                    needsAutoHandling
                ) {
                    BackingSymbol = methodSymbol
                };

                return true;
            }
            default:
                return false;
        }

        opt = new Option(
            type,
            new OptDesc(
                longName, shortName, argName, desc
            ),
            defaultVal
        ) {
            BackingSymbol = member
        };

        return true;
    }
}