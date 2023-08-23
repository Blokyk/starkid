#pragma warning disable RCS1197 // Optimize StringBuilder call

using Recline.Generator.Model;

namespace Recline.Generator;

internal static class CodegenHelpers
{
    public static string GenerateUsingsHeaderCode(IEnumerable<string> usings) =>
        !usings.Any()
            ? ""
            : "using " + String.Join(";\nusing ", usings) + ";\n\n";

    public static string GetFullExpression(Argument arg)
        => GetValidatingExpression(
            GetParsingExpression(arg.Parser, arg.DefaultValueExpr),
            arg.Name,
            arg.Type.IsNullable,
            arg.Validators
        );

    public static string GetFullExpression(Option opt)
        => GetValidatingExpression(
            GetParsingExpression(opt.Parser, opt.DefaultValueExpr),
            opt.Name,
            opt.Type.IsNullable,
            opt.Validators
        );

    public static string GetParsingExpression(ParserInfo parser, string? defaultValueExpr) {
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
            ParserInfo.Identity => "__arg",
            ParserInfo.DirectMethod dm => "ThrowIfParseError<" + targetType.FullName + ">(" + dm.FullName + ", __arg ?? \"\")",
            ParserInfo.Constructor ctor => "new " + ctor.TargetType.FullName + "(__arg ?? \"\")",
            ParserInfo.BoolOutMethod bom => "ThrowIfTryParseNotTrue<" + targetType.FullName + ">(" + bom.FullName + ", __arg ?? \"\")",
            _ => throw new Exception(parser.GetType().Name + " is not a supported ParserInfo type."),
        };

        if (targetType.IsNullable && defaultValueExpr is not null)
            expr = '(' + expr + " ?? " + defaultValueExpr + ')';

        return expr;
    }

    public static string GetValidatingExpression(string argExpr, string argName, bool isNullable, ImmutableArray<ValidatorInfo> validators) {
        if (validators.Length == 0)
            return argExpr;

        var currExpr = argExpr;

        foreach (var validator in validators) {
            string funcExpr;
            string exprStr;

            switch (validator) {
                case ValidatorInfo.Method method:
                    funcExpr = method.FullName;
                    exprStr = method.MethodInfo.Name + "(" + argName + ")";
                    break;
                case ValidatorInfo.Property prop:
                    funcExpr = "static (arg) => arg." + prop.PropertyName;
                    exprStr = argName + "." + prop.PropertyName;
                    break;
                default:
                    throw new Exception(validator.GetType().Name + " is not a supported ValidatorInfo type.");
            }

            var throwFunc = isNullable ? "ThrowIfNotValidNullable(" : "ThrowIfNotValid(";

            currExpr =
                throwFunc +
                    $"{currExpr}, " +
                    $"{funcExpr}, " +
                    $"\"{argName}\", " +
                    $"{(validator.Message is null ? "null" : SyntaxFactory.Literal(validator.Message))}, " +
                    $"\"{exprStr}\"" +
                ")";
        }

        return currExpr;
    }

    public static StringBuilder AppendDictEntry(this StringBuilder sb, string key, string value)
        => sb.Append("\t\t\t{ \"").Append(key).Append("\", ").Append(value).Append(" },");
}