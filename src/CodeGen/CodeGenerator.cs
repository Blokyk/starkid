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

    void AddOptionLookup(StringBuilder sb, InvokableBase groupOrCmd, bool isFlags) {
        string funcName = isFlags ? "TryExecFlagAction" : "TryExecOptionAction";

        sb.Append(@"
        internal static bool ").Append(funcName).Append("(string optName, string? arg, bool onlyAllowGlobal) {");

        // if we didn't find the option here, it might be global, so we ask the parent group
        var defaultReturn
            = groupOrCmd.ParentGroup is null
            ? "false"
            : groupOrCmd.ParentGroup.ID + "CmdDesc." + funcName + "(optName, arg, true)";

        IEnumerable<Option> opts = isFlags ? groupOrCmd.Flags : groupOrCmd.Options;
        // if there's no options, just return false
        if (!opts.Any()) {
            sb.Append(" return ").Append(defaultReturn).AppendLine("; }");
            return;
        }

        if (!isFlags) {
            sb.Append("""
            void updateArg() {
                if (arg is null && !TryGetNextArgFromArgv(out arg))
                    ExitWithError("Option {0} needs an argument", 1, optName);
            }
""");
        }

        sb.Append(@"
            if (_displayHelp)
                return true;

            try {
                switch (optName) {");

        foreach (var opt in opts) {
            sb.Append(@"
                case ""--").Append(opt.Name).AppendLine("\":");
            if (opt.Alias != '\0') {
                sb.Append(@"
                case ""-").Append(opt.Alias).AppendLine("\":");
            }

            if (!opt.IsGlobal) {
                // add a guard against using this option globally
                //
                // it's fine to exit early because there can't be a
                // parent global option with the same name anyway
                sb.Append(@"
                    if (onlyAllowGlobal) return false;");
            }

            if (opt is not Flag) {
                sb.Append(@"
                    updateArg();");
            }

            sb.Append(@"
                    ").Append(opt.BackingSymbol.Name).AppendLine(@"Action(arg!);
                    return true;");
        }

        sb.Append(@"
                    default:
                        return ").Append(defaultReturn).Append(';');
        sb.Append("""
                }
            } catch (Exception e) {
                if (_displayHelp)
                    return true;

                if (arg is null) {
                    // arg is only null when it's a flag (although we could get a flag with a non-null arg)
                    ExitWithError(
                        "Using the '" + optName + "' flag isn't valid in this context: {0}",
                        e.Message
                    );
                } else {
                    ExitWithError(
                        "Expression '{0}' is not a valid value for option '" + optName + "': {1}",
                        arg, e.Message
                    );
                }

                return false;
            }
        }
""");
    }

    void AddHasParamsField(StringBuilder sb, Command? cmd)
         => sb.Append(@"
        internal const bool _hasParams = ").Append((cmd?.HasParams ?? false) ? "true" : "false").Append(';')
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

    void AddActivateFunc(StringBuilder sb) =>
        sb.Append(@"
        internal static void Activate() {
            ReclineProgram._prevCmdName = _currCmdName;
            ReclineProgram._tryExecOption = TryExecOptionAction;
            ReclineProgram._tryExecFlag = TryExecFlagAction;
            ReclineProgram._tryUpdateCmd = TryUpdateCommand;
            ReclineProgram._hasParams = _hasParams;
            ReclineProgram._posArgActions = _posArgActions;
            ReclineProgram._invokeCmd = _invokeCmd;
            ReclineProgram._helpString = _helpText;
            ReclineProgram._currCmdName = _name;
        }");
}