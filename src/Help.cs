namespace Recline.Generator.Model;

public record CmdHelp(
    string? ParentCmd,
    string CmdName,
    string? Description,
    OptDesc[] CmdOpts,
    Desc[] PosArgs,
    WithArgsDesc[] SubCmds,
    bool IsDirectCmd = true,
    bool HasParams = false
) {
    public StringBuilder AppendTo(StringBuilder sb) {
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

            if (ParentCmd is not null) {
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

            if (allCmdsStr.Length > 40 || sb.Length + allCmdsStr.Length > Ressources.MAX_LINE_LENGTH) {
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

        AppendDescs(sb, "Options", CmdOpts.Concat(new[] { new SwitchDesc("help", 'h', "Print this help message") } ).Cast<Desc>().ToArray())
            .AppendLine();
        AppendDescs(sb, "Arguments", PosArgs)
            .AppendLine();
        AppendDescs(sb, "Commands", SubCmds.Cast<Desc>().ToArray());

        return sb;
    }

    private static readonly char[] splitWithSpaceArray = new[] { ' ' };

    private static StringBuilder AppendDescs(StringBuilder sb, string sectionName, Desc[] descArr) {
        if (descArr.Length == 0)
            return sb;

        sb
            .AppendLine(sectionName + ':');

        var prefixStrings = GetPrefixStrings(descArr);

        var maxPrefixLength = prefixStrings.Max(s => s.Length);

        for (int i = 0; i < descArr.Length; i++) {
            ref var opt = ref descArr[i];
            ref var pre = ref prefixStrings[i];

            sb.Append(pre);

            if (opt.Description is null || opt.Description.Length == 0) {
                sb.AppendLine();
                continue;
            }

            var maxIndentLength = maxPrefixLength + 2;
            var maxIndentStr = new string(' ', maxIndentLength);

            var indentStr = new string(' ', maxIndentLength - pre.Length);

            // if there's not enough space for the description, skip a line
            // and indent before inserting it
            if (pre.Length + opt.Description.Length > Ressources.MAX_LINE_LENGTH) {
                sb
                    //.AppendLine()
                    .Append(indentStr);
            } else {
                sb.Append(indentStr);
            }

            var charsLeft = Ressources.MAX_LINE_LENGTH - maxIndentLength;

            if (opt.Description.Length <= charsLeft) {
                sb.AppendLine(opt.Description);
                continue;
            }

            var descWords = opt.Description.Split(splitWithSpaceArray);

            int currDescLineLength = 0;

            for (int j = 0; j < descWords.Length; j++) {
                ref var word = ref descWords[j];

                if (currDescLineLength + word.Length + 1 + maxIndentLength > Ressources.MAX_LINE_LENGTH) {
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

    private static string[] GetPrefixStrings(Desc[] opts) {
        var prefixStrings = new string[opts.Length];

        for (int i = 0; i < opts.Length; i++) {
            ref var desc = ref opts[i];

            var str = "  ";

            if (desc is OptDesc opt1) {
                if (opt1.Alias == '\0') {
                    str += "   ";
                } else {
                    str += "-" + opt1.Alias + ",";
                }

                str += " --" + opt1.LongName;
            } else {
                str += desc.Name;
            }

            if (desc is WithArgsDesc { ArgNames.Length: > 0 } withArgs) {
                if (withArgs is not SwitchDesc) {
                    str += " <" + String.Join("> <", withArgs.ArgNames) + ">";
                }
            }

            prefixStrings[i] = str;
        }

        return prefixStrings;
    }
}