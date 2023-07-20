#pragma warning disable RCS1197 // Optimize StringBuilder call

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

        AddOptionDictionary(sb, group, isFlags: false);

        foreach (var flag in group.Flags) {
            AddOptionFunction(sb, flag, group);
        }

        AddOptionDictionary(sb, group, isFlags: true);

        sb.AppendLine();

        AddSubsDictionary(sb, group);

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

        sb.Append(@"
    internal const string _name = """).Append(group.Name).Append("\";")
        .AppendLine();

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
        static IEnumerable<string> GetCmdIDs(Group group) {
            yield return group.ID;

            foreach (var cmd in group.Commands)
                yield return cmd.ID;

            foreach (var sub in group.SubGroups) {
                foreach (var id in GetCmdIDs(sub)) {
                    yield return id;
                }
            }
        }

        sb.Append(
            $$"""
        {{Resources.GenFileHeader}}

        internal static partial class {{Resources.ProgClassName}}
        {
            private enum CmdID {

        """
        );

        var cmdIDs = GetCmdIDs(rootGroup);

        foreach (var id in cmdIDs)
            sb.Append("\t\t").Append(id).Append(',').AppendLine();

        sb
        .Append("\t}")  // enum CmdID
        .AppendLine()
        .AppendLine();

        sb.AppendLine(
            """
            private static bool TryUpdateCommand(CmdID subCmdID) {
                if (!_displayHelp && ArgCount != 0) {
                    ExitWithError("Can't invoke sub-command '{0}' with arguments for the '" + _prevCmdName + "' command", _currCmdName);
                    return false;
                }

                _prevCmdName = _currCmdName;

                switch (subCmdID) {
        """
        );

        foreach (var id in cmdIDs) {
            sb.Append("\t\t\tcase CmdID.").Append(id).AppendLine(":");
            sb.Append("\t\t\t\t_options = ").Append(id).AppendLine("CmdDesc._options;");
            sb.Append("\t\t\t\t_flags = ").Append(id).AppendLine("CmdDesc._flags;");
            sb.Append("\t\t\t\t_subs = ").Append(id).AppendLine("CmdDesc._subs;");
            sb.Append("\t\t\t\t_hasParams = ").Append(id).AppendLine("CmdDesc._hasParams;");
            sb.Append("\t\t\t\t_posArgActions = ").Append(id).AppendLine("CmdDesc._posArgActions;");
            sb.Append("\t\t\t\t_invokeCmd = ").Append(id).AppendLine("CmdDesc._invokeCmd;");
            sb.Append("\t\t\t\t_helpString = ").Append(id).AppendLine("CmdDesc._helpText;");
            sb.Append("\t\t\t\t_currCmdName = ").Append(id).AppendLine("CmdDesc._name;");
            sb.Append("\t\t\t\tbreak;");
            sb.AppendLine();
        }

        sb.AppendLine(
            """
                    default:
                        throw new ArgumentException($"Recline internal error: CmdID '{subCmdID}' is unknown!", nameof(subCmdID));
                }

                return true;
            }
        """
        );
    }

    void AddRootFooter(StringBuilder sb, Group _)
        => sb.AppendLine("}"); // class Program

    void AddSubsDictionary(StringBuilder sb, Group group) {
        sb.Append(@"
        internal static readonly Dictionary<string, CmdID> _subs = new() {");

        if (group.SubGroups.Count == 0 && group.Commands.Count == 0) {
            sb.AppendLine(" };");
            return;
        }

        sb.AppendLine();

        foreach (var sub in group.SubGroups)
            sb.AppendDictEntry(sub.Name, "CmdID." + sub.ID).AppendLine();

        foreach (var cmd in group.Commands)
            sb.AppendDictEntry(cmd.Name, "CmdID." + cmd.ID).AppendLine();

        sb
        .Append("\t\t};")
        .AppendLine();
    }
}