namespace Recline.Generator.Model;

public record CmdHelp(
    string? ParentCmd,
    string CmdName,
    string? Description,
    ImmutableArray<OptDesc> CmdOpts,
    ImmutableArray<Desc> PosArgs,
    ImmutableArray<WithArgsDesc> SubCmds,
    bool IsDirectCmd = true,
    bool HasParams = false
) {
    public override string ToString() {
        var isRoot = ParentCmd is null;

        var sb = new StringBuilder();

        if (Description is not null) {
            sb
                .AppendLine("Description:")
                .Append("  ")
                .AppendLine(Description)
                .AppendLine();
        }

#region usage

        sb
            .AppendLine("Usage:");

        void appendNameAndOpts() {
            sb.Append("  ");

            if (!isRoot) {
                sb.Append(ParentCmd).Append(' ');
            }

            sb.Append(CmdName);

            if (CmdOpts.Length != 0)
                sb.Append(" [options]");
        }

        appendNameAndOpts();

        // if it can be used directly, first print one without sub cmds,
        // then print a new line for the subcmds help to use
        if (IsDirectCmd) {
            foreach (var arg in PosArgs) {
                sb
                    .Append(" <")
                    .Append(arg.Name)
                    .Append('>');
            }

            if (HasParams)
                sb.Append("...");

            sb.Append("\n  ");
        }

        if (SubCmds.Length != 0) {
            sb.AppendLine();
            appendNameAndOpts();

            if (!IsDirectCmd) {
                sb.Append(" <");
            } else {
                sb.Append(" [");
            }

            var allCmdsStr = String.Join(" | ", SubCmds.Select(cmd => cmd.Name));

            if (allCmdsStr.Length > 40 || sb.Length + allCmdsStr.Length > Resources.MAX_LINE_LENGTH) {
                sb.Append("command");
            } else {
                sb.Append(allCmdsStr);
            }

            if (!IsDirectCmd) {
                sb.Append('>');
            } else {
                sb.Append(']');
            }
        }
#endregion

        sb
            .AppendLine()
            .AppendLine();

        var cmdOptsBuilder = ImmutableArray.CreateBuilder<Desc>();
        cmdOptsBuilder.AddRange(CmdOpts);

        bool hasCustomHelp = false;
        bool hasHAlias = false;

        foreach (var opt in CmdOpts) {
            if (opt.Alias == 'h')
                hasHAlias = true;

            if (opt.LongName == "help") {
                hasCustomHelp = true;
                break;
            }
        }

        if (!hasCustomHelp) {
            if (hasHAlias)
                cmdOptsBuilder.Add(_helpFlagNoAlias);
            else
                cmdOptsBuilder.Add(_fullHelpFlag);
        }

        AppendDescs(sb, "Options", cmdOptsBuilder.ToImmutable())
            .AppendLine();
        AppendDescs(sb, "Arguments", PosArgs)
            .AppendLine();
        AppendDescs(sb, "Commands", ImmutableArray<Desc>.CastUp(SubCmds));

        return sb.ToString();
    }

    private static readonly FlagDesc _fullHelpFlag = new("help", 'h', "Print this help message");
    private static readonly FlagDesc _helpFlagNoAlias = new("help", '\0', "Print this help message");
    private static readonly char[] splitWithSpaceArray = new[] { ' ' };

    private static StringBuilder AppendDescs(StringBuilder sb, string sectionName, ImmutableArray<Desc> descArr) {
        if (descArr.Length == 0)
            return sb;

        sb
            .Append(sectionName).Append(':').AppendLine();

        var prefixStrings = GetPrefixStrings(descArr);

        var maxPrefixLength = prefixStrings.Max(s => s.Length);

        for (int i = 0; i < descArr.Length; i++) {
            var opt = descArr[i];
            var pre = prefixStrings[i];

            sb.Append(pre);

            if (String.IsNullOrEmpty(opt.Description)) {
                sb.AppendLine();
                continue;
            }

            var maxIndentLength = maxPrefixLength + 2;
            var maxIndentStr = new string(' ', maxIndentLength);

            var indentStr = new string(' ', maxIndentLength - pre.Length);

            // if there's not enough space for the description, skip a line
            // and indent before inserting it
            if (pre.Length + opt.Description!.Length > Resources.MAX_LINE_LENGTH) {
                sb
                    .Append(indentStr);
            } else {
                sb.Append(indentStr);
            }

            var charsLeft = Resources.MAX_LINE_LENGTH - maxIndentLength;

            if (opt.Description.Length <= charsLeft) {
                sb.AppendLine(opt.Description);
                continue;
            }

            var descWords = opt.Description.Split(splitWithSpaceArray);

            int currDescLineLength = 0;

            for (int j = 0; j < descWords.Length; j++) {
                ref var word = ref descWords[j];

                if (currDescLineLength + word.Length + 1 + maxIndentLength > Resources.MAX_LINE_LENGTH) {
                    sb
                        .AppendLine()
                        .Append(maxIndentStr);

                    currDescLineLength = 0;
                }

                sb.Append(word).Append(' ');
                currDescLineLength += word.Length + 1;
            }

            sb.AppendLine();
        }

        return sb;
    }

    private static ImmutableArray<string> GetPrefixStrings(ImmutableArray<Desc> opts) {
        var prefixStrings = ImmutableArray.CreateBuilder<string>(opts.Length);

        foreach (var desc in opts) {
            var str = "  ";

            if (desc is OptDesc opt) {
                if (opt.Alias == '\0') {
                    str += "   ";
                } else {
                    str += "-" + opt.Alias + ",";
                }

                str += " --" + opt.LongName;
            } else {
                str += desc.Name;
            }

            if (desc is WithArgsDesc { ArgNames.Length: > 0 } withArgs) {
                if (withArgs is not FlagDesc) {
                    str += " <" + String.Join("> <", withArgs.ArgNames) + ">";
                }
            }

            prefixStrings.Add(str);
        }

        return prefixStrings.MoveToImmutable();
    }
}