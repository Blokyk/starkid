#pragma warning disable RCS1197 // Optimize StringBuilder call

using System.Linq;

using Recline.Generator.Model;

namespace Recline.Generator;

internal sealed partial class CodeGenerator
{
    void AddSourceCode(StringBuilder sb, Group group, bool isRoot) {
        if (isRoot)
            AddRootHeader(sb, group);

        sb.Append(@"
#pragma warning disable CS8618
#pragma warning disable CS8625
private static class ").Append(group.ID).Append("CmdDesc {")
        .AppendLine();

        foreach (var opt in group.Options) {
            AddOptionFunction(sb, opt, group);
        }

        AddOptionLookup(sb, group, isFlags: false);

        foreach (var flag in group.Flags) {
            AddOptionFunction(sb, flag, group);
        }

        AddOptionLookup(sb, group, isFlags: true);

        sb.AppendLine();

        AddSubsLookup(sb, group);

        sb.AppendLine();

        AddActivateFunc(sb);

        sb.AppendLine();

        // --- On the subject of options in default commands ---
        //
        // After a bit of reflection and experimentation, I've
        //    decided it best not to handle default command
        //   options at all, which means that, although they
        //  are allowed in the command definition, they won't
        //     be "available" unless you specify the actual
        //       command name instead of defaulting to it

        var defaultCmd = group.DefaultCommand;

        AddHasParamsField(sb, defaultCmd);

        sb.AppendLine();

        AddPosArgActions(sb, group);

        sb.AppendLine();

        AddInvokeCmdField(sb, group);

        sb.AppendLine();

        AddHelpTextLine(sb, group);

        sb.AppendLine();

        AddCommandName(sb, group);

        sb.AppendLine().Append("\t}").AppendLine();

        foreach (var sub in group.SubGroups) {
            AddSourceCode(sb, sub, false);
        }

        foreach (var cmd in group.Commands) {
            AddSourceCode(sb, cmd);
        }

        if (isRoot)
            AddRootFooter(sb, group);
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

    void AddRootHeader(StringBuilder sb, Group rootGroup) {
        sb.AppendLine(Resources.GenFileHeader);

        sb.Append("internal static partial class ").Append(Resources.ProgClassName).Append(@"
{
#pragma warning disable CS8618
    static ReclineProgram() {
        ").Append(rootGroup.ID).AppendLine(@"CmdDesc.Activate();
    }
#pragma warning restore CS8618");
    }

    void AddRootFooter(StringBuilder sb, Group _)
        => sb.AppendLine("}"); // class Program

    void AddSubsLookup(StringBuilder sb, Group group) {
        sb.Append(@"
        internal static bool TryUpdateCommand(string cmdName) {");

        if (group.SubGroups.Count == 0 && group.Commands.Count == 0) {
            sb.AppendLine(" return false; }");
            return;
        }

        sb.Append("""

            if (!_displayHelp && ArgCount != 0) {
                ExitWithError("Can't invoke sub-command '{0}' with arguments for the '" + _prevCmdName + "' command", _currCmdName);
                return false;
            }

            switch (cmdName) {
""");
        foreach (var sub in group.SubGroups.Concat((IEnumerable<InvokableBase>)group.Commands)) {
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

    void AddCommandName(StringBuilder sb, Group group)
        => sb.Append(@"
        internal const string _name = """).Append(group.Name).Append("\";").AppendLine();
}