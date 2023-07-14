#pragma warning disable RCS1197 // Optimize StringBuilder call

using Recline.Generator.Model;

namespace Recline.Generator;

internal sealed partial class CodeGenerator
{
    private readonly ReclineConfig _config;

    private readonly HelpGenerator _helpGenerator;

    public CodeGenerator(ReclineConfig config) {
        _config = config;
        _helpGenerator = new(config);
    }

    public static string ToSourceCode(Group rootGroup, ReclineConfig config) {
        var generator = new CodeGenerator(config);
        var sb = new StringBuilder();
        generator.AddSourceCode(sb, rootGroup, true);
        return sb.ToString();
    }

    void AddOptionDictionary(StringBuilder sb, InvokableBase groupOrCmd, bool isFlags) {
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
            IEnumerable<Option> groupOpts = isFlags ? group.Flags : group.Options;
            appendAllOptions(groupOpts.Where(opt => opt.IsGlobal), group.ID + "CmdDesc.");

            if (group.ParentGroup is not null)
                appendGroupOptions(group.ParentGroup);
        }

        string dictName = isFlags ? "_flags" : "_options";

        sb.Append(@"
        internal static readonly Dictionary<string, Action<string").Append(isFlags ? "?" : "").Append(">> ").Append(dictName).Append(@" = new() {
            { ""--help"", DisplayHelp }, { ""-h"", DisplayHelp },")
        .AppendLine();

        appendAllOptions(isFlags ? groupOrCmd.Flags : groupOrCmd.Options, "");
        if (groupOrCmd.ParentGroup is not null)
            appendGroupOptions(groupOrCmd.ParentGroup);

        sb
        .Append("\t\t};")
        .AppendLine();
    }

    void AddHasParamsField(StringBuilder sb, Command? cmd)
         => sb.Append(@"
        internal static readonly bool _hasParams = ").Append((cmd?.HasParams ?? false) ? "true" : "false").Append(';')
         .AppendLine();

    void AddPosArgActions(StringBuilder sb, Group group) {
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

    void AddPosArgActions(StringBuilder sb, Command cmd) {
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
                CodegenHelpers.GetValidatingExpression(
                    CodegenHelpers.GetParsingExpression(arg.Parser, null, null),
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

    void AddInvokeCmdField(StringBuilder sb, Group group) {
        sb.Append(@"
        internal static readonly Func<int> _invokeCmd = ");

        if (group.DefaultCommand is not null) {
            sb.Append(group.DefaultCommand.ID).Append("CmdDesc._invokeCmd");
        } else {
            sb.Append("ReclineProgram.NonInvokableGroupAction");
        }

        sb.Append(';')
        .AppendLine();
    }

    void AddInvokeCmdField(StringBuilder sb, Command cmd) {
        sb.Append(@"
        internal static readonly Func<int> _invokeCmd = ");

        var isVoid = cmd.BackingMethod.ReturnsVoid;
        var methodParams = cmd.BackingMethod.Parameters;

        // if _func is already Func<int>
        if (!isVoid && methodParams.Length == 0) {
            sb.Append("_func");
        } else {
            // lambda attributes are only supported since C#10
            if (_config.LanguageVersion >= LanguageVersion.CSharp10)
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
        .Append(';')
        .AppendLine();
    }
}