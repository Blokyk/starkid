#pragma warning disable RCS1197 // Optimize StringBuilder call

using Recline.Generator.Model;

namespace Recline.Generator;

internal static class CodegenHelpers
{
    public static string GenerateUsingsHeaderCode(ImmutableArray<string> usings) =>
        usings.IsDefaultOrEmpty
            ? ""
            : "using " + String.Join(";\nusing ", usings) + ";\n\n";

    public static StringBuilder AppendOptionFunction(this StringBuilder sb, Option opt, InvokableBase groupOrCmd) {
        if (groupOrCmd is Command) {
            sb
            .Append("\t\tprivate static ")
            .Append(opt.Type.FullName)
            .Append(' ')
            .Append(opt.BackingSymbol.Name);

            if (opt.DefaultValueExpr is not null) {
                sb
                .Append(" = ")
                .Append(opt.DefaultValueExpr);
            }

            sb
            .Append(';')
            .AppendLine();
        }

        var argExpr = GetParsingExpression(opt.Parser, opt.BackingSymbol.Name, opt.DefaultValueExpr);
        var validExpr = GetValidatingExpression(argExpr, opt.Name, opt.Validator);

        var fieldPrefix
            = groupOrCmd is Group group
            ? group.FullClassName + "."
            : "";

        string expr
            // = opt.BackingSymbol.ToString() + " = " + validExpr;
            = fieldPrefix + SymbolUtils.GetSafeName(opt.BackingSymbol.Name) + " = " + validExpr;

        // internal static void {optName}Action(string[?] __arg) => Validate(Parse(__arg));
        return sb
            .Append(@"
        internal static void ")
            .Append(opt.BackingSymbol.Name)
            .Append("Action(string")
            .Append(opt is Flag ? "?" : "")
            .Append(" __arg) => ")
            .Append(expr)
            .Append(';')
            .AppendLine();
    }

    public static string GetParsingExpression(ParserInfo parser, string? argName, string? defaultValueExpr) {
        if (parser == ParserInfo.AsBool) {
            var name = ParserInfo.AsBool.FullName;
            if (defaultValueExpr is null)
                return name + "(__arg)";
            else
                return name + "(__arg, !" + defaultValueExpr + ")";
        }

        string expr = "";

        var targetType = parser.TargetType;

        if (targetType.SpecialType == SpecialType.System_Boolean) {
            expr = "__arg is null ? true : ";
        }

        expr += parser switch {
            ParserInfo.Identity => "__arg" + (argName is null ? "" : " ?? " + argName),
            ParserInfo.DirectMethod dm => "ThrowIfParseError<" + targetType.FullName + ">(" + dm.FullName + ", __arg ?? \"\")",
            ParserInfo.Constructor ctor => "new " + ctor.TargetType.FullName + "(__arg ?? \"\")",
            ParserInfo.BoolOutMethod bom => "ThrowIfTryParseNotTrue<" + targetType.FullName + ">(" + bom.FullName + ", __arg ?? \"\")",
            _ => throw new Exception(parser.GetType().Name + " is not a supported ParserInfo type."),
        };

        if (targetType.IsNullable && defaultValueExpr is not null)
            expr = '(' + expr + " ?? " + defaultValueExpr + ')';

        return expr;
    }

    public static string GetValidatingExpression(string argExpr, string argName, ValidatorInfo? validator) {
        if (validator is null)
            return argExpr;

        string funcExpr;
        string exprStr;

        switch (validator) {
            case ValidatorInfo.Method method:
                funcExpr = method.FullName;
                exprStr = method.MethodInfo.Name + "(" + argName + ")";
                break;
            case ValidatorInfo.Property prop:
                funcExpr = "(arg) => arg." + prop.PropertyName;
                exprStr = argName + "." + prop.PropertyName;
                break;
            default:
                throw new Exception(validator.GetType().Name + " is not a supported ValidatorInfo type.");
        }

        return
            "ThrowIfNotValid(" +
                $"{argExpr}, " +
                $"{funcExpr}, " +
                $"\"{argName}\", " +
                $"{(validator.Message is null ? "null" : SyntaxFactory.Literal(validator.Message))}, " +
                $"\"{exprStr}\"" +
            ")";
    }

    public static StringBuilder AppendDictEntry(this StringBuilder sb, string key, string value)
        => sb.Append("\t\t\t{ \"").Append(key).Append("\", ").Append(value).Append(" },");

    public static StringBuilder AppendOptDictionaryLine(this StringBuilder sb, string longName, char shortName, string methodName) {
        sb.AppendDictEntry("--" + longName, methodName);

        if (shortName is not '\0')
            sb.AppendLine().AppendDictEntry("-" + shortName, methodName);

        return sb.AppendLine();
    }
}