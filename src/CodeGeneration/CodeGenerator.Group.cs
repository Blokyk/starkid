#pragma warning disable RCS1197 // Optimize StringBuilder call

using StarKid.Generator.CommandModel;

namespace StarKid.Generator.CodeGeneration;

internal sealed partial class CodeGenerator
{
    void AddSourceCode(StringBuilder sb, Group group) {
        sb.Append(@"
#pragma warning disable CS8618
#pragma warning disable CS8625
    [StackTraceHidden]
    static partial class ").Append(group.ID).Append("CmdDesc {")
        .AppendLine();

        // --- On the subject of options in default commands ---
        //
        // After a bit of reflection and experimentation, I've
        //    decided it best not to handle default command
        //   options at all, which means that, although they
        //  are allowed in the command definition, they won't
        //     be "available" unless you specify the actual
        //       command name instead of defaulting to it

        foreach (var opt in group.Options) {
            AddOptionFieldAndSetter(sb, opt, group);
        }

        AddOptionLookup(sb, group, isFlags: false);

        foreach (var flag in group.Flags) {
            AddOptionFieldAndSetter(sb, flag, group);
        }

        AddOptionLookup(sb, group, isFlags: true);

        sb.AppendLine();

        AddSubsLookup(sb, group);

        sb.AppendLine();

        AddFlushBuildersFunc(sb, group);

        sb.AppendLine();

        AddActivateFunc(sb);

        sb.AppendLine();

        AddParamsFields(sb, group);

        sb.AppendLine();

        AddPosArgActions(sb, group);

        sb.AppendLine();

        AddInvokeCmdField(sb, group);

        sb.AppendLine();

        AddCommandName(sb, group);

        sb.AppendLine().Append("\t}").AppendLine();

        foreach (var sub in group.SubGroups) {
            AddSourceCode(sb, sub);
        }

        foreach (var cmd in group.Commands) {
            AddSourceCode(sb, cmd);
        }
    }

    void AddInvokeCmdField(StringBuilder sb, Group group) {
        sb.Append(@"
        internal static readonly Func<int> _invokeCmd = ");

        if (group.DefaultCommand is not null) {
            sb.Append(group.DefaultCommand.ID).Append("CmdDesc._invokeCmd");
        } else {
            sb.Append("StarKidProgram.NonInvokableGroupAction");
        }

        sb.Append(';')
        .AppendLine();
    }

    void AddSubsLookup(StringBuilder sb, Group group) {
        sb.Append(@"
        internal static bool TryUpdateCommand(string cmdName) {");

        if (group.SubGroups.Count == 0 && group.Commands.Count == 0) {
            sb.AppendLine(" return false; }");
            return;
        }

        sb.Append("""

            if (ArgCount != 0)
                return false;

            switch (cmdName) {
""");

        var nonHiddenCmds = group.Commands.Where(cmd => !cmd.IsHiddenCommand);
        foreach (var sub in group.SubGroups.Concat(nonHiddenCmds.Cast<InvokableBase>())) {
            sb.Append(@"
                case """).Append(sub.Name).Append(@""":
                    ").Append(sub.ID).Append(@"CmdDesc.Activate();
                    return true;");
        }

        sb.AppendLine(@"
                default:
                    return false;
            }
        }");
    }

    void AddParamsFields(StringBuilder sb, Group group) {
        if (group.DefaultCommand is null) {
            sb.Append(@"
        internal static readonly Action<string> _addParams = DefaultParamsAdd;
        internal const bool _hasParams = false;").AppendLine();
            return;
        }

        AddParamsFields(sb, group.DefaultCommand);
    }

    void AddPosArgActions(StringBuilder sb, Group group) {
        var defaultCmd = group.DefaultCommand;

        if (defaultCmd is null) {
            sb.Append(@"
        internal const int _requiredArgCount = 0;
        internal static readonly Action<string>[] _posArgActions = Array.Empty<Action<string>>();").AppendLine();
            return;
        }

        sb.Append(@"
        internal const int _requiredArgCount = ").Append(defaultCmd.ID).Append(@"CmdDesc._requiredArgCount;
        internal static readonly Action<string>[] _posArgActions = ").Append(defaultCmd.ID).Append("CmdDesc._posArgActions;").AppendLine();
    }

    void AddCommandName(StringBuilder sb, Group group)
        => sb.Append(@"
        internal const string __name = """).Append(group.Name).Append("\";").AppendLine();
}