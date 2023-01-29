#pragma warning disable RCS1197 // Optimize StringBuilder call

using Recline.Generator.Model;
using System.Collections.ObjectModel;

namespace Recline.Generator;

internal static partial class CodeGenerator
{
    internal static class HelpGenerator
    {
        private const string padding = "  ";
        private const int padSize = 2;
        private static int MaxLineLength => Resources.MAX_LINE_LENGTH;

        public static void AddHelpText(StringBuilder sb, InvokableBase groupOrCmd) {
            AddDescription(sb, groupOrCmd);
            AddUsage(sb, groupOrCmd);

            // fixme: if DefaultCommand is hidden, then add its opts+args in usage and in the help text

            sb.AppendLine();

            var builder = new HelpTextBuilder(padSize, MaxLineLength);

            if (groupOrCmd is Command cmd) {
                foreach (var arg in cmd.Arguments)
                    builder.AddArgumentDescription(arg.Name, arg.Description);
            }

            foreach (var opt in groupOrCmd.OptionsAndFlags) {
                var aliasStr
                    = opt.Alias == '\0'
                    ? "    "
                    : "-" + opt.Alias + ", ";

                var argStr
                    = opt is not Flag
                    ? " <" + opt.ArgName + ">"
                    : "";

                builder.AddOptionDescription(
                    aliasStr + "--" + opt.Name + argStr,
                    opt.Description
                );
            }

            if (groupOrCmd is Group group) {
                foreach (var sub in group.SubGroups) {
                    builder.AddSubcommandDescription(
                        sub.Name + " [cmds] [options]",
                        sub.Description
                    );
                }

                foreach (var subcmd in group.Commands) {
                    string argStr
                        = subcmd.Arguments.Count switch {
                            0 => "",
                            1 => argStr = " <" + subcmd.Arguments[0].Name + ">",
                            _ => " <args>",
                        };

                    builder.AddSubcommandDescription(
                        subcmd.Name + argStr,
                        subcmd.Description
                    );
                }
            }

            builder.WriteTo(sb);
        }

        static void AddUsage(StringBuilder sb, InvokableBase groupOrCmd) {
            sb.AppendLine("Usage:");
            AddShortLineHelp(sb, groupOrCmd);

            if (groupOrCmd is not Group group)
                return;

            if (group.SubGroups.Count + group.Commands.Count > 5)
                return;

            sb.AppendLine();

            foreach (var subGroup in group.SubGroups) {
                AddShortLineHelp(sb, subGroup);
                sb.AppendLine();
            }

            foreach (var subCmd in group.Commands) {
                if (subCmd.IsHiddenCommand)
                    continue;

                AddShortLineHelp(sb, subCmd);
                sb.AppendLine();
            }
        }

        static void AddShortLineHelp(StringBuilder sb, InvokableBase groupOrCmd) {
            sb.Append(padding);
            AddNameWithParentsBefore(sb, groupOrCmd);

            var optsAndFlagsCount = groupOrCmd.Options.Count + groupOrCmd.Flags.Count;
            if (optsAndFlagsCount != 0) {
                sb.Append(' ');
                AddShortOptionList(sb, groupOrCmd.OptionsAndFlags, optsAndFlagsCount);
            }

            if (groupOrCmd is Command cmd) {
                sb.Append(' ');
                AddShortArgumentList(sb, cmd.Arguments);
            }
        }

        static void AddShortArgumentList(StringBuilder sb, ReadOnlyCollection<Argument> args) {
            if (args.Count == 0)
                return;

            foreach (var arg in args) {
                sb.Append('<');
                sb.Append(arg.Name);

                if (arg.IsParams)
                    sb.Append("...");

                sb.Append("> ");
            }

            sb.Length--; // remove the last space
        }

        static void AddShortOptionList(StringBuilder sb, IEnumerable<Option> opts, int count) {
            if (count == 0)
                return;

            if (GetDisplayLength(opts) > 30) {
                sb.Append("[options]");
                return;
            }

            foreach (var opt in opts) {
                sb.Append('[');

                if (opt.Alias != '\0') {
                    sb
                        .Append('-')
                        .Append(opt.Alias)
                        .Append(" | ");
                }

                sb
                    .Append("--")
                    .Append(opt.Name);

                if (opt is not Flag) {
                    sb
                        .Append(" <")
                        .Append(opt.ArgName)
                        .Append('>');
                }

                sb.Append("] ");
            }

            sb.Length--; // remove the last space
        }

        static int GetDisplayLength(IEnumerable<Option> opts) {
            int length = 0;

            foreach (var opt in opts) {
                if (opt.Alias != '\0')
                    length += 5;

                length += opt.Name.Length + 5;

                if (opt is not Flag)
                    length += opt.ArgName.Length + 3;
            }

            return length - 1; // minus the last space
        }

        static void AddNameWithParentsBefore(StringBuilder sb, InvokableBase groupOrCmd) {
            if (groupOrCmd.ParentGroup is not null) {
                AddNameWithParentsBefore(sb, groupOrCmd.ParentGroup);
                sb.Append(' ');
            }

            sb.Append(groupOrCmd.Name);
        }

        static void AddDescription(StringBuilder sb, InvokableBase groupOrCmd) {
            var desc = groupOrCmd.Description;

            if (desc is null)
                return;

            // not AppendLine cause that's handled in the loop
            sb.Append("Description:");

            foreach (var line in desc.Split('\n')) {
                sb
                    .AppendLine()
                    .Append(padding);

                if (line.Length < MaxLineLength - padSize) {
                    sb.Append(line);
                    continue;
                }

                int charsLeft = MaxLineLength - padSize;

                foreach (var word in line.Split(' ')) {
                    if (word.Length > charsLeft) {
                        sb
                            .AppendLine()
                            .Append(padding);
                        charsLeft = MaxLineLength - padSize;
                    }

                    sb.Append(word);
                }
            }
        }
    }
}