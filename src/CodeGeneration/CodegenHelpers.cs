#pragma warning disable RCS1197 // Optimize StringBuilder call

using StarKid.Generator.CommandModel;
using StarKid.Generator.SymbolModel;

namespace StarKid.Generator.CodeGeneration;

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

    public static string GetFullExpression(Option opt) {
        var validators
            = opt.IsRepeatableOption()
            ? opt.Validators.Where(v => v.IsElementWiseValidator)
            : opt.Validators.Where(v => !v.IsElementWiseValidator);

        return
            GetValidatingExpression(
                GetParsingExpression(opt.Parser, opt.DefaultValueExpr),
                opt.Name,
                opt.Type.IsNullable,
                validators
            );
    }

    public static string GetParsingExpression(ParserInfo parser, string? defaultValueExpr) {
        string expr = "";

        var targetType = parser.TargetType;

        if (targetType.SpecialType is SpecialType.System_Boolean) {
            expr = "__arg is null ? true : ";
        }

        static string getNonNullableTypeName(MinimalTypeInfo type)
            => type is MinimalNullableValueTypeInfo nullableInfo
             ? getNonNullableTypeName(nullableInfo.ValueType)
             : type.FullName;

        expr += parser switch {
            ParserInfo.Identity => "__arg",
            ParserInfo.AsBool
                => ParserInfo.AsBool.FullName + (defaultValueExpr is null ? "(__arg)" : "(__arg, !" + defaultValueExpr + ")"),
            ParserInfo.DirectMethod dm
                => "ThrowIfParseError<" + getNonNullableTypeName(targetType) + ">(" + dm.FullName + ", __arg ?? \"\")",
            ParserInfo.Constructor ctor
                => "new " + getNonNullableTypeName(ctor.TargetType) + "(__arg ?? \"\")",
            ParserInfo.BoolOutMethod bom
                => "ThrowIfTryParseNotTrue<" + getNonNullableTypeName(targetType) + ">(" + bom.FullName + ", __arg ?? \"\")",
            _ => throw new Exception(parser.GetType().Name + " is not a supported ParserInfo type."),
        };

        if (targetType.IsNullable && defaultValueExpr is not null)
            expr = '(' + expr + " ?? " + defaultValueExpr + ')';

        return expr;
    }

    public static string GetValidatingExpression(string argExpr, string argName, bool isNullable, IEnumerable<ValidatorInfo> validators) {
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
                    var expected = prop.ExpectedValue ? "true":"false"; // coz it's uppercase by default
                    funcExpr = "static (arg) => arg." + prop.PropertyName + " is " + expected;
                    exprStr = argName + "." + prop.PropertyName + " is " + expected;
                    break;
                default:
                    throw new Exception(validator.GetType().Name + " is not a supported ValidatorInfo type.");
            }

            var throwFunc = isNullable ? "ThrowIfNotValidNullable(" : "ThrowIfNotValid(";

            var msg = validator.Message is null ? "null" : SymbolDisplay.FormatLiteral(validator.Message, quote: true);

            currExpr =
                throwFunc +
                    $"{currExpr}, " +
                    $"{funcExpr}, " +
                    $"\"{argName}\", " +
                    $"{msg}, " +
                    $"\"{exprStr}\"" +
                ")";
        }

        return currExpr;
    }

    public static StringBuilder AppendDictEntry(this StringBuilder sb, string key, string value)
        => sb.Append("\t\t\t{ \"").Append(key).Append("\", ").Append(value).Append(" },");
}