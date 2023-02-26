#pragma warning disable RCS1197 // Optimize StringBuilder call

using Recline.Generator.Model;

namespace Recline.Generator;

internal static partial class CodeGenerator
{
    private static LanguageVersion _langVersion;

    public static string ToSourceCode(Group rootGroup) {
        var sb = new StringBuilder();
        GroupCodeGenerator.AddSourceCode(sb, rootGroup, true);
        return sb.ToString();
    }

    static void AddOptionDictionary(StringBuilder sb, Union<Group, Command> groupOrCmd, bool isFlags) {
        void appendAllOptions(IEnumerable<Option> options, string prefix) {
            foreach (var opt in options) {
                sb
                .AppendOptDictionaryLine(
                    opt.Name,
                    opt.Alias,
                    prefix + opt.BackingSymbol.Name + "Action"
                );
            }
        }

        void appendGroupOptions(Group group) {
            appendAllOptions(isFlags ? group.Flags : group.Options, group.ID + "CmdDesc.");

            if (group.ParentGroup is not null)
                appendGroupOptions(group.ParentGroup);
        }

        string dictName = isFlags ? "_flags" : "_options";

        sb.Append(@"
        internal static readonly Dictionary<string, Action<string").Append(isFlags ? "?" : "").Append(">> ").Append(dictName).Append(@" = new() {
            { ""--help"", DisplayHelp }, { ""-h"", DisplayHelp },
");

        groupOrCmd.Match(
            group => appendGroupOptions(group),
            cmd => {
                appendAllOptions(isFlags ? cmd.Flags : cmd.Options, "");
                appendGroupOptions(cmd.ParentGroup);
            }
        );

        sb
        .Append("\t\t};")
        .AppendLine();
    }

    static void AddHasParamsField(StringBuilder sb, Command? cmd)
         => sb.Append(@"
        internal static readonly bool _hasParams = ").Append((cmd?.HasParams ?? false) ? "true" : "false").Append(";")
         .AppendLine();

    static void AddPosArgActions(StringBuilder sb, Group group) {
        sb.Append(@"
        internal static readonly Action<string>[] _posArgActions = ");

        var defaultCmd = group.DefaultCommand;
        if (defaultCmd is not null)
            sb.Append(defaultCmd.ID).Append("CmdDesc._posArgActions");
        else
            sb.Append("Array.Empty<Action<string>>()");

        sb.Append(';')
        .AppendLine();
    }

    static void AddPosArgActions(StringBuilder sb, Command cmd) {
        foreach (var arg in cmd.Arguments) {
            if (arg.IsParams)
                continue; // cf above

            sb
            .Append("\t\tprivate static ")
            .Append(arg.Type.FullName + (arg.Type.IsNullable ? "?" : ""))
            .Append(' ')
            .Append(arg.BackingSymbol.Name);

            if (arg.DefaultValueExpr is not null) {
                sb
                .Append(" = ")
                .Append(arg.DefaultValueExpr);
            }

            sb.AppendLine(";");
        }

        sb.Append(@"
        internal static readonly Action<string>[] _posArgActions = ");

        if (cmd.Arguments.Count == 0) {
            sb
            .Append("Array.Empty<Action<string>>();")
            .AppendLine();
            return;
        }

        sb
        .Append("new Action<string>[] {")
        .AppendLine();

        foreach (var arg in cmd.Arguments) {
            if (arg.IsParams)
                continue; // could be break since params is always the last parameter

            sb
            .Append("\t\t\tstatic __arg => ")
            .Append(arg.BackingSymbol.Name)
            .Append(" = ")
            .Append(
                GetValidatingExpression(
                    GetParsingExpression(arg.Parser, null, null),
                    arg.Name,
                    arg.Validator
                )
            )
            .Append(',')
            .AppendLine();
        }

        sb
        .Append("\t\t};")
        .AppendLine();
    }

    static void AddInvokeCmdField(StringBuilder sb, Group group) {
        sb.Append(@"
        internal static readonly Func<int> _invokeCmd = ");

        if (group.DefaultCommand is not null) {
            sb.Append(group.DefaultCommand.ID).Append("CmdDesc._invokeCmd");
        } else {
            sb.Append("DisplayHelp");
        }

        sb.Append(';')
        .AppendLine();
    }

    static void AddInvokeCmdField(StringBuilder sb, Command cmd) {
        sb.Append(@"
        internal static readonly Func<int> _invokeCmd = ");

        var isVoid = cmd.BackingMethod.ReturnsVoid;
        var methodParams = cmd.BackingMethod.Parameters;

        // if _func is already Func<int>
        if (!isVoid && methodParams.Length == 0) {
            sb.Append("_func");
        } else {
            // lambda attributes are only supported since C#10
            if (_langVersion >= LanguageVersion.CSharp10)
                sb.Append("[System.Diagnostics.StackTraceHidden]");

            sb.Append("() => "); // can't be static because of _params

            if (isVoid)
                sb.Append("{ ");

            sb.Append("_func(");

            var defArgName = new string[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++) {
                defArgName[i]
                    = methodParams[i].IsParams
                        ? "_params.ToArray()"
                        : SymbolUtils.GetSafeName(methodParams[i].Name) + "!";
            }

            sb.Append(String.Join(", ", defArgName));

            sb.Append(')');

            if (isVoid)
                sb.Append("; return 0; }");
        }

        sb
        .Append(";")
        .AppendLine();
    }

    static void AddHelpTextLine(StringBuilder sb, InvokableBase groupOrCmd) {
        var helpTextSb = new StringBuilder();

        HelpGenerator.AddHelpText(helpTextSb, groupOrCmd);

        sb.Append(@"
        internal const string _helpText = ").Append(SyntaxFactory.Literal(helpTextSb.ToString())).Append(';')
        .AppendLine();
    }

    static StringBuilder AppendOptionFunction(this StringBuilder sb, Option opt, InvokableBase groupOrCmd) {
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

    static string GetParsingExpression(ParserInfo parser, string? argName, string? defaultValueExpr) {
        if (parser == ParserInfo.AsBool) {
            var name = ParserInfo.AsBool.FullName;
            if (defaultValueExpr is null)
                return name + "(__arg)";
            else
                return name + "(__arg, !" + defaultValueExpr + ")";
        }

        string expr = "";

        if (parser.TargetType == CommonTypes.BOOLMinInfo) {
            expr = "__arg is null ? true : ";
        }

        expr += parser switch {
            ParserInfo.Identity => "__arg" + (argName is null ? "" : " ?? " + argName),
            ParserInfo.DirectMethod dm => "ThrowIfParseError<" + parser.TargetType.FullName + ">(" + dm.FullName + ", __arg ?? \"\")",
            ParserInfo.Constructor ctor => "new " + ctor.TargetType.FullName + "(__arg ?? \"\")",
            ParserInfo.BoolOutMethod bom => "ThrowIfTryParseNotTrue<" + parser.TargetType.FullName + ">(" + bom.FullName + ", __arg ?? \"\")",
            _ => throw new Exception(parser.GetType().Name + " is not a supported ParserInfo type."),
        };

        if (parser.TargetType.IsNullable && defaultValueExpr is not null)
            expr = '(' + expr + " ?? " + defaultValueExpr + ')';

        return expr;
    }

    static string GetValidatingExpression(string argExpr, string argName, ValidatorInfo? validator) {
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

    static StringBuilder AppendDictEntry(this StringBuilder sb, string key, string value)
        => sb.Append("\t\t\t{ \"").Append(key).Append("\", ").Append(value).Append(" },");

    static StringBuilder AppendOptDictionaryLine(this StringBuilder sb, string longName, char shortName, string methodName) {
        sb.AppendDictEntry("--" + longName, methodName);

        if (shortName is not '\0')
            sb.AppendLine().AppendDictEntry("-" + shortName, methodName);

        return sb.AppendLine();
    }

    internal static void UseLanguageVersion(LanguageVersion langVersion)
        => _langVersion = langVersion;
}