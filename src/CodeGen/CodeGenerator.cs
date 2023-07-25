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
        internal static readonly Dictionary<string, Action<string").Append(isFlags ? "?" : "").Append(">> ").Append(dictName).Append(" = new() {")
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
            .Append(" @")
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
                continue; // nit: could be break since params is always the last parameter

            sb
            .Append("\t\t\tstatic __arg => @")
            .Append(arg.BackingSymbol.Name)
            .Append(" = ")
            .Append(
                CodegenHelpers.GetValidatingExpression(
                    CodegenHelpers.GetParsingExpression(arg.Parser, null, null),
                    arg.Name,
                    arg.Validators
                )
            )
            .Append(',')
            .AppendLine();
        }

        sb
        .Append("\t\t};")
        .AppendLine();
    }

    public void AddOptionFunction(StringBuilder sb, Option opt, InvokableBase groupOrCmd) {
        if (groupOrCmd is not Group) {
            sb
            .Append("\t\tprivate static ")
            .Append(opt.Type.FullName)
            .Append(" @")
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

        var argExpr = CodegenHelpers.GetParsingExpression(opt.Parser, opt.BackingSymbol.Name, opt.DefaultValueExpr);
        var validExpr = CodegenHelpers.GetValidatingExpression(argExpr, opt.Name, opt.Validators);

        var fieldPrefix
            = groupOrCmd is Group group
            ? "@" + group.FullClassName + ".@"
            : "@";

        string expr
            = fieldPrefix + opt.BackingSymbol.Name + " = " + validExpr;

        var actionName = opt.BackingSymbol.Name + "Action";
        var argType = opt is Flag ? "string?" : "string";

        if (!_config.AllowRepeatingOptions) {
            sb
            .Append(@"
        private static bool has").Append(actionName).AppendLine("BeenTriggered;");
        }

        // internal static void {optName}Action(string[?] __arg) {
        //     if (has{optName}ActionBeenTriggered)
        //         ThrowOptionAlreadySpecified("{optName}");
        //     has{optName}ActionBeenTriggered = true;
        //     Validate(Parse(__arg));
        // }

        sb
            .Append(@"
        internal static void ").Append(actionName).Append('(').Append(argType).Append(" __arg) {");

        if (!_config.AllowRepeatingOptions) {
            sb
                .Append(@"
            if (has").Append(actionName).Append("BeenTriggered)")
                .Append(@"
                ThrowOptionAlreadySpecified(""--").Append(opt.Name).Append("\");")
                .Append(@"
            has").Append(actionName).Append("BeenTriggered = true;");
        }

        sb
            .Append(@"
            ").Append(expr).AppendLine(@";
        }");
    }
}