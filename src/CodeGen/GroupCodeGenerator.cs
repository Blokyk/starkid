#pragma warning disable RCS1197 // Optimize StringBuilder call

using Recline.Generator.Model;

namespace Recline.Generator;

internal static partial class CodeGenerator
{
    internal static class GroupCodeGenerator
    {
        public static void AddSourceCode(StringBuilder sb, Group group, bool isRoot) {
            if (isRoot)
                AddRootHeader(sb, group);

            sb.Append(@"
#pragma warning disable CS8618
#pragma warning disable CS8625
    private static class ").Append(group.ID).Append("CmdDesc {")
            .AppendLine();

            foreach (var opt in group.Options) {
                sb.AppendOptionFunction(opt, group);
            }

            AddOptionDictionary(sb, group, isFlags: false);

            foreach (var flag in group.Flags) {
                sb.AppendOptionFunction(flag, group);
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

            if (!isRoot)
                sb.Append('\t');

            sb.Append("\t}");

            foreach (var sub in group.SubGroups) {
                AddSourceCode(sb, sub, false);
            }

            foreach (var cmd in group.Commands) {
                CommandCodeGenerator.AddSourceCode(sb, cmd);
            }

            if (isRoot)
                AddRootFooter(sb, group);
        }

        static void AddRootHeader(StringBuilder sb, Group rootGroup) {
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
            .AppendLine();

            sb.AppendLine(
                """
                private static bool TryUpdateCommand(CmdID subCmdID) {
                    if (ArgCount != 0) {
                        PrintHelpString("Can't invoke sub-command '{0}' with arguments for the '" + _prevCmdName + "' command", _currCmdName);
                        DisplayHelp();
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

        static void AddRootFooter(StringBuilder sb, Group rootGroup)
            => sb.Append("}"); // class Program

        static void AddSubsDictionary(StringBuilder sb, Group group) {
            sb.Append(@"
        internal static readonly Dictionary<string, CmdID> _subs = new() {")
            .AppendLine();

            foreach (var sub in group.SubGroups)
                sb.AppendDictEntry(sub.Name, "CmdID." + sub.ID);

            foreach (var cmd in group.Commands)
                sb.AppendDictEntry(cmd.Name, "CmdID." + cmd.ID);

            sb
            .Append("\t\t};") // _subs = new { }
            .AppendLine();
        }
    }
}