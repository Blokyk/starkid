using System.IO;
using System.Diagnostics;

namespace StarKid.Generator;

public sealed class HelpTextBuilder
{
    private readonly struct TextInfo {
        public static readonly TextInfo Empty = new();

        public readonly string text;
        public readonly string[] lines;
        public readonly int maxLineLength;
        public bool HasNewlines => lines.Length > 1;

        public TextInfo() {
            text = "";
            lines = Array.Empty<string>();
        }

        public TextInfo(string s) {
            text = s;
            lines = text.Split('\n');
            maxLineLength = 0;

            foreach (var line in lines) {
                if (line.Length > maxLineLength)
                    maxLineLength = line.Length;
            }
        }
    }

    private readonly string _padding;
    private readonly int _padSize;

    private readonly int _maxTotalSize;
    private int _nameMaxSize = 0;

    private readonly Dictionary<string, TextInfo> _args = new();
    private readonly Dictionary<string, TextInfo> _opts = new();
    private readonly Dictionary<string, TextInfo> _subs = new();

    public HelpTextBuilder(int spacing, int maxTotalSize) {
        _padSize = spacing;
        _padding = new(' ', spacing);
        _maxTotalSize = maxTotalSize;
    }

    private void AddDescription(Dictionary<string, TextInfo> dict, string name, string? desc) {
        var info
            = desc is not null
            ? new TextInfo(desc)
            : TextInfo.Empty;
        dict.Add(name, info);

        if (name.Length >= _nameMaxSize && name.Length < 30) {
            _nameMaxSize = name.Length;
        }
    }

    public void AddArgumentDescription(string name, string? description)
        => AddDescription(_args, name, description);
    public void AddOptionDescription(string name, string? description)
        => AddDescription(_opts, name, description);
    public void AddSubcommandDescription(string name, string? description)
        => AddDescription(_subs, name, description);

    public override string ToString() {
        var sb = new StringBuilder();
        WriteTo(sb);
        return sb.ToString();
    }

    public void WriteTo(StringBuilder sb) {
        if (_args.Count > 0) {
            sb.AppendLine();
            sb.AppendLine("Arguments:");
            AddAllDescriptions(sb, _args);
        }

        if (_opts.Count > 0) {
            sb.AppendLine();
            sb.AppendLine("Options:");
            AddAllDescriptions(sb, _opts);
        }

        if (_subs.Count > 0) {
            sb.AppendLine();
            sb.AppendLine("Subcommands:");
            AddAllDescriptions(sb, _subs);
        }
    }

    private void AddAllDescriptions(StringBuilder sb, Dictionary<string, TextInfo> descriptions) {
        int nameColumnSize = _nameMaxSize + (2 * _padSize);
        //                                  ^^^^^^^^^^^^^^
        //                         there's padding before AND after

        foreach (var (name, desc) in descriptions) {
            sb.Append(_padding);

            sb.Append(name);

            if (desc.lines.Length == 0) {
                sb.AppendLine();
                continue;
            }

            // if there isn't enough space for the name, skip a
            // line first (and add the necessary padding) before
            // printing the description
            if (_nameMaxSize - name.Length < 0) {
                sb.AppendLine();
                sb.Append(' ', nameColumnSize - _padSize);
                //                            ^^^^^^^^^^
                //                we call sb.Append(_padding) just after
            } else {
                // not nameColumnSize because we already have the padding before,
                // and we'll add the padding after
                sb.Append(' ', _nameMaxSize - name.Length);
            }

            // padding between names and description
            sb.Append(_padding);

            for (int i = 0; i < desc.lines.Length; i++) {
                // we don't want to add a line before the first line
                if (i != 0) {
                    sb
                        .AppendLine()
                        .Append(' ', nameColumnSize);
                }

                var line = desc.lines[i];

                if (line.Length <= _maxTotalSize - nameColumnSize) {
                    // not AppendLine because the next iteration will call it (and add padding)
                    sb.Append(line);
                    continue;
                }

                int charsLeft = _maxTotalSize - nameColumnSize;

                foreach (var word in line.Split(' ')) {
                    if (word.Length + 1 > charsLeft) {
                        sb
                            .AppendLine()
                            .Append(' ', nameColumnSize);
                        charsLeft = _maxTotalSize - nameColumnSize;
                    }

                    sb.Append(word).Append(' ');
                    charsLeft -= word.Length + 1;
                }
            }

            sb.AppendLine();
        }
    }
}