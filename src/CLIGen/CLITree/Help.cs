namespace CLIGen.Generator.Model;

public record CmdHelp(
    string? RootCmd,
    string CmdName,
    string? Description,
    OptDesc[] CmdOpts,
    Desc[] PosArgs,
    Desc[] SubCmds,
    bool IsDirectCmd = true
) : ICLINode {
    public StringBuilder AppendTo(StringBuilder sb) {

        if (MainGenerator._cachedHelpText.TryGetValue(this, out var str)) {
            return sb.Append(str);
        }

        if (Description is not null) {
            sb
                .AppendLine("Description:")
                .Append("  ")
                .AppendLine(Description)
                .AppendLine();
        }

#region usage

        sb
            .AppendLine("Usage:")
            .Append("  ");

        if (RootCmd is not null) {
            sb.Append(RootCmd).Append(' ');
        }

        sb
            .Append(CmdName)
            .Append(' ');

        if (CmdOpts.Length != 0)
            sb.Append("[options] ");

        foreach (var arg in PosArgs) {
            sb
                .Append('<')
                .Append(arg.Name)
                .Append("> ");
        }

        if (SubCmds.Length != 0) {
            if (!IsDirectCmd) {
                sb.Append('<');
            } else {
                sb.Append('[');
            }

            var allCmdsStr = String.Join(" | ", SubCmds.Select(cmd => cmd.Name));

            if (allCmdsStr.Length > 40 || sb.Length + allCmdsStr.Length > Ressources.MAX_LINE_LENGTH) {
                sb.Append("command");
            } else {
                sb.Append(allCmdsStr);
            }


            if (!IsDirectCmd) {
                sb.Append("> ");
            } else {
                sb.Append("] ");
            }
        }
#endregion

        sb
            .AppendLine()
            .AppendLine();

        AppendDescs(sb, "Options", CmdOpts.Cast<OptDesc>().ToArray());
        AppendDescs(sb, "Arguments", PosArgs);
        AppendDescs(sb, "Commands", SubCmds);


        return sb;
    }

    private static char[] splitWithSpaceArray = new[] { ' ' };

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

            if (opt.Description is null || opt.Description.Length == 0)
                return sb.AppendLine();

            var indentLength = maxPrefixLength + 2;
            var indentStr = new string(' ', indentLength);

            // if there's not enough space for the description, skip a line
            // and indent before inserting it
            if (pre.Length + opt.Description.Length > Ressources.MAX_LINE_LENGTH) {
                sb
                    .AppendLine()
                    .Append(indentStr);
            } else {
                sb.Append("  ");
            }

            var charsLeft = Ressources.MAX_LINE_LENGTH - indentLength;

            if (opt.Description.Length <= charsLeft) {
                sb.AppendLine(opt.Description);
                continue;
            }

            var descWords = opt.Description.Split(splitWithSpaceArray, 2);

            int currDescLineLength = 0;

            for (int j = 0; j < descWords.Length; j++) {
                ref var word = ref descWords[j];
                sb.Append(word).Append(' ');
                currDescLineLength += word.Length + 1;

                if (currDescLineLength + indentLength > Ressources.MAX_LINE_LENGTH) {
                    sb
                        .AppendLine()
                        .Append(indentStr);

                    currDescLineLength = 0;
                }
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
                    str += "<" + String.Join("> <", withArgs.ArgNames) + ">";
                }
            }

            prefixStrings[i] = str;
        }

        return prefixStrings;
    }
}