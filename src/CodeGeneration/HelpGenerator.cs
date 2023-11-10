#pragma warning disable RCS1197 // Optimize StringBuilder call
using System.Collections.ObjectModel;

using StarKid.Generator.CommandModel;

namespace StarKid.Generator.CodeGeneration;

internal sealed partial class HelpGenerator(StarKidConfig config)
{
    private const string padding = "  ";
    private const int padSize = 2;
    private readonly int _maxLineLength = config.ColumnLength;

    public void AddHelpText(StringBuilder sb, InvokableBase groupOrCmd) {
        AddDescription(sb, groupOrCmd);
        AddUsage(sb, groupOrCmd);

        // fixme(#1): if DefaultCommand is hidden, then add its opts+args in usage and in the help text

        // todo: if there's no description at all in a certain category, then we might be able
        // to shorten it (especially args and subcmds, don't know about options tho)

        var builder = new HelpTextBuilder(padSize, _maxLineLength);

        if (groupOrCmd is Command cmd) {
            foreach (var arg in cmd.Arguments)
                builder.AddArgumentDescription(FormatArgName(arg.Name), arg.Description);
        }

        builder.AddOptionDescription(
            "-h, --help",
            "Display this help message"
        );

        foreach (var opt in groupOrCmd.OptionsAndFlags) {
            var aliasStr
                = opt.Alias == '\0'
                ? "    "
                : "-" + opt.Alias + ", ";

            var argStr
                = opt is not Flag
                ? " <" + FormatArgName(opt.ArgName) + ">"
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
                    sub.Description?.ShortDesc ?? sub.Description?.Description
                );
            }

            foreach (var subcmd in group.Commands) {
                if (subcmd.IsHiddenCommand)
                    continue;

                string argStr
                    = subcmd.Arguments.Count switch {
                        0 => "",
                        1 => argStr = " <" + FormatArgName(subcmd.Arguments[0].Name) + ">",
                        _ => " <args>",
                    };

                builder.AddSubcommandDescription(
                    subcmd.Name + argStr,
                    subcmd.Description?.ShortDesc ?? subcmd.Description?.Description
                );
            }
        }

        builder.WriteTo(sb);

        // we *definitely* wrote something to sb at this point,
        // so no need to check the length
        if (sb[^1] == '\n')
            sb.Length--;

        AddNotes(sb, groupOrCmd);
    }

    void AddUsage(StringBuilder sb, InvokableBase groupOrCmd) {
        sb.AppendLine("Usage:");

        if (groupOrCmd is not Group group) { // if it's a command, there's nothing else to do
            AddShortLineHelp(sb, groupOrCmd);
            sb.AppendLine();
            return;
        }

        // only add usage if this group has any option OR it's invokable (has a default command)
        if (group.DefaultCommand is not null || group.OptionsAndFlags.Any()) {
            AddShortLineHelp(sb, group);
            sb.AppendLine();
            return;
        }

        // if it'd be too long to display, just return, the help text will do the rest
        if (group.SubGroups.Count + group.Commands.Count > 5) {
            sb.AppendLine("...\n");
            return;
        }

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

    void AddShortLineHelp(StringBuilder sb, InvokableBase groupOrCmd) {
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

    void AddShortArgumentList(StringBuilder sb, ReadOnlyCollection<Argument> args) {
        if (args.Count == 0)
            return;

        foreach (var arg in args) {
            sb.Append('<');
            sb.Append(FormatArgName(arg.Name));

            if (arg.IsParams)
                sb.Append("...");

            sb.Append("> ");
        }

        sb.Length--; // remove the last space
    }

    void AddShortOptionList(StringBuilder sb, IEnumerable<Option> opts, int count) {
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
                    .Append(FormatArgName(opt.ArgName))
                    .Append('>');
            }

            sb.Append("] ");
        }

        sb.Length--; // remove the last space
    }

    int GetDisplayLength(IEnumerable<Option> opts) {
        int length = 0;

        foreach (var opt in opts) {
            if (opt.Alias != '\0')
                length += 5;

            length += opt.Name.Length + 5;

            if (opt is not Flag)
                length += FormatArgName(opt.ArgName).Length + 3;
        }

        return length - 1; // minus the last space
    }

    void AddNameWithParentsBefore(StringBuilder sb, InvokableBase groupOrCmd) {
        if (groupOrCmd.ParentGroup is not null) {
            AddNameWithParentsBefore(sb, groupOrCmd.ParentGroup);
            sb.Append(' ');
        }

        sb.Append(groupOrCmd.Name);
    }

    void AddDescription(StringBuilder sb, InvokableBase groupOrCmd) {
        var desc = groupOrCmd.Description?.Description ?? groupOrCmd.Description?.ShortDesc;

        if (desc is null)
            return;

        // not AppendLine cause that's handled in the loop
        sb.Append("Description:");
        AppendAllLines(sb, desc, padding);
        sb.AppendLine();
        sb.AppendLine();
    }

    void AddNotes(StringBuilder sb, InvokableBase groupOrCmd) {
        var notes = groupOrCmd.Description?.Remarks!;

        if (String.IsNullOrEmpty(notes))
            return;

        if (notes[0] != '\n')
            sb.AppendLine();

        AppendAllLines(sb, notes, padding: "");

        if (notes[^1] != '\n')
            sb.AppendLine();
    }

    void AppendAllLines(StringBuilder sb, string s, string padding) {
        var padSize = padding.Length;
        var maxPaddedLineLength = _maxLineLength - padSize;
        foreach (var line in s.Split('\n')) {
            sb
                .AppendLine()
                .Append(padding);

            if (line.Length < maxPaddedLineLength) {
                sb.Append(line);
                continue;
            }

            int charsLeft = maxPaddedLineLength;

            foreach (var word in line.Split(' ')) {
                if (word.Length > charsLeft) {
                    sb
                        .AppendLine()
                        .Append(padding);
                    charsLeft = maxPaddedLineLength;
                }

                sb.Append(word).Append(' ');
                charsLeft -= word.Length + 1;
            }
        }
    }

    private readonly Cache<string, string> _formattedNameCache
        = new(StringComparer.InvariantCulture, s => NameFormatter.Format(s, config.ArgNameCasing));
    private string FormatArgName(string s) => _formattedNameCache.GetValue(s);
}