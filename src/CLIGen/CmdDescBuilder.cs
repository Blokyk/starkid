using System.Collections.Immutable;
using System.Collections.Concurrent;

using CLIGen;
using CLIGen.Generator;
using CLIGen.Generator.Model;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Diagnostics;

using static CLIGen.Generator.Ressources;

namespace CLIGen.Generator;

public class CmdDescBuilder
{
    public static string GetBaseDescFile(string appName)
        => $@"
{GenFileHeader}
static partial class {ProgClassName} {{
    private abstract partial class CmdDesc {{
        internal static CmdDesc root = new {appName}CmdDesc();
    }}
}}";

    public string CliClassName { get; }

    public Dictionary<Command, List<string>> rootToSubTable = new();

    private StringBuilder sb;

    public CmdDescBuilder(string cliClassName) {
        CliClassName = cliClassName;
        sb = new StringBuilder();
    }

    public static string GenerateHelpFile(Command cmd, Option[] optsAndSws, Desc[] posArgs) {
        return "";
    }

    public static string GetClassDeclarationLine(Command cmd)
        => $@"
    private partial class {cmd.Name}CmdDesc : {cmd.ParentCmdName ?? ""}CmdDesc {{
";

    public void AddCmd(Command cmd, Option[] optsAndSws, Desc[] posArgs) {
        var sb = new StringBuilder();

        if (cmd.ParentCmdName is not null) {
            if (!rootToSubTable.TryGetValue(cmd.ParentCmd!, out var list))
                rootToSubTable[cmd.ParentCmd!] = new() { cmd.Name };
            else
                list.Add(cmd.Name);
        }

        sb.Append($@"
    private partial class {cmd.Name}CmdDesc : {cmd.ParentCmdName ?? ""}CmdDesc {{

        internal {cmd.Name}CmdDesc() : base(_switches, _options) {{}}

        protected {cmd.Name}CmdDesc(
            Dictionary<string, Action<string, string?>> switches,
            Dictionary<string, Action<string, string>> options
        )
            : base(_switches.UpdatedWith(switches), _options.UpdatedWith(options))
        {{}}
");

        var sws = new List<Option>();
        var opts = new List<Option>();

        foreach (var opt in optsAndSws) {
            if (Utils.Equals(opt.Type, Utils.BOOL))
                sws.Add(opt);
            else
                opts.Add(opt);
        }

        /*** Switches ***/

        sb.AppendLine($@"private static Dictionary<string, Action<string, string?>> _switches = new() {{");

        foreach (var sw in sws) {
            sb.AppendLine(GetOptDictLine(sw.Desc.LongName, sw.Desc.Alias, "set" + sw.Desc.Name));
        }

        sb.AppendLine("};");

        foreach(var sw in sws) {
            sb.AppendLine(
                GetOptFuncLine(
                    "set" + sw.Desc.Name,
                    CliClassName + "." + sw.BackingSymbol?.Name
                    + " = "
                    + "AsBool(arg, " + (sw.DefaultValue is null ? "true" : sw.DefaultValue) + ")"
                )
            );
        }

        /*** Options ***/

        sb.AppendLine($@"private static Dictionary<string, Action<string, string?>> _options = new() {{");

        foreach (var opt in opts) {
            sb.AppendLine(GetOptDictLine(opt.Desc.LongName, opt.Desc.Alias, opt.Desc.Name + "Action"));
        }

        sb.AppendLine("};");

        foreach (var opt in opts) {
            string expr;

            if (opt is MethodOption methodOpt) {
                expr = CliClassName + "." + methodOpt.BackingSymbol.Name + "(arg)";

                if (methodOpt.NeedsAutoHandling) {
                    expr = "ThrowIfNotValid(" + expr + ")";
                }
            } else {
                expr = CliClassName + "." + opt.BackingSymbol.Name + " = Parse<" + opt.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">(arg)";
            }

            sb.AppendLine(
                GetOptFuncLine(
                    opt.Desc.Name + "Action",
                    expr
                )
            );
        }

        /*** Func ***/

        /*** Footer ***/

        sb.AppendLine($@"
    }}
");
    }

    public override string ToString() {
        foreach (var kv in rootToSubTable) {
            var root = kv.Key;
            var subs = kv.Value;

            sb
                .AppendLine(GetClassDeclarationLine(root))
                .AppendLine($@"
        internal override Dictionary<string, Func<CmdDesc>> SubCmds => _subs;
        private static Dictionary<string, Func<CmdDesc>> _subs = new() {{
")
                ;

            foreach (var sub in subs) {
                sb.AppendLine(GetDictLine(sub, "static () => new " + sub + "CmdDesc"));
            }

            sb.AppendLine("}");
        }

        return $@"
{GenFileHeader}
static partial class {ProgClassName} {{
" + sb.ToString() + "}";
    }

    static string GetOptFuncLine(string methodName, string expr) {
        return $@"private static void {methodName}(string origFlag, string arg) => {expr};";
    }

    static string GetDictLine(string key, string value)
        => $@"{{ ""{key}"", {value} }},";

    static string GetOptDictLine(string longName, char shortName, string methodName) {
        var str = GetDictLine("--" + longName, methodName);

        if (shortName is not '\0')
            str += GetDictLine("-" + shortName, methodName);

        return str;
    }
}