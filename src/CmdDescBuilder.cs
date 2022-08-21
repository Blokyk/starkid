using Recline.Generator.Model;

using static Recline.Generator.Resources;

namespace Recline.Generator;

internal class CmdDescBuilder
{
    public string CliClassName { get; }
    public string[] RequiredUsings { get; }
    public string AppName { get; }
    public string? Description { get; }

    Dictionary<Command, List<Command>> domToSubTable;

    Command RootCmd { get; }
    Argument[] RootArgs { get; }
    Option[] RootOptsAndSws { get; }

    List<(Command cmd, Option[] opts, Argument[] posArgs)> _allCmdInfo = new();

    bool HasRealRootCmd { get; }

    public int HelpExitCode { get; set; } = 0;

    private StringBuilder sb;

    public CmdDescBuilder(
        string appName,
        string cliClassName,
        string[] usings,
        (Command cmd, Argument[] args)? cmdAndArgs,
        Option[] optsAndSws,
        string? description
    ) {
        AppName = appName;
        CliClassName = cliClassName;
        RequiredUsings = usings;
        sb = new StringBuilder();
        Description = description;

        HasRealRootCmd = cmdAndArgs.HasValue;

        if (cmdAndArgs.HasValue) {
            RootArgs = cmdAndArgs.Value.args;
            RootCmd = cmdAndArgs.Value.cmd;
        } else {
            RootArgs = Array.Empty<Argument>();
            RootCmd = new Command(true, appName, Description, Array.Empty<Option>(), RootArgs);
        }

        RootOptsAndSws = optsAndSws;

        domToSubTable = new() {
            { RootCmd, new List<Command>() }
        };

        AddRoot();
    }

    void AddRootHelpText() {
        AddHelpText(
            RootCmd,
            domToSubTable[RootCmd].ToArray(),
            RootOptsAndSws,
            RootArgs,
            true
        );
    }

    public void AddHelpText(Command cmd, Command[] subs, Option[] opts, Argument[] posArgs, bool isRoot = false) {
        var help = new CmdHelp(
            cmd.ParentCmd?.GetNameWithParent() ?? (isRoot ? null : RootCmd.Name),
            cmd.Name,
            cmd.Description,
            opts.Select(o => o.Desc).ToArray(),
            posArgs.Select(a => a.Desc).ToArray(),
            subs.Select(s => s.WithArgsDesc).ToArray(),
            IsDirectCmd: !isRoot,
            HasParams: cmd.HasParams
        );

        var helpStr = help.ToString();

        sb.Append($@"
{GetClassDeclarationLine(cmd, isRoot)}
        public override string Name => ""{cmd.Name}"";
        internal override string HelpString => _helpString;
        private static readonly string _helpString = {SyntaxFactory.Literal(helpStr).ToString()};

        private static void DisplayHelp(string? val) {{
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit({HelpExitCode});
        }}
    }}
");
    }

    void AddRoot() {
        sb.AppendLine($@"
    private abstract partial class CmdDesc {{
        private static readonly Lazy<CmdDesc> _lazyRoot = new(static () => new {AppName}CmdDesc(), false);
        internal static CmdDesc root => _lazyRoot.Value;

");

        AddOptsAndSwitches(RootOptsAndSws, isRoot: true);

        sb.AppendLine($@"
    }}");

        if (HasRealRootCmd)
            AddCmd(RootCmd, RootOptsAndSws, RootArgs, true);

    }

    //TODO: change "switch" vocab to "flag"
    void AddOptsAndSwitches(Option[] optsAndSws, bool isRoot = false) {
        var opts = new List<Option>(optsAndSws.Length);
        var sws = new List<Option>(optsAndSws.Length);

        foreach (var thing in optsAndSws) {
            if (thing.Type == Utils.BOOLMinInfo)
                sws.Add(thing);
            else
                opts.Add(thing);
        }

        #region Switches

        sb.AppendLine($@"private static Dictionary<string, Action<string?>> _switches = new() {{");

        sb.AppendLine($@"
            {{ ""--help"", DisplayHelp }},
            {{ ""-h"", DisplayHelp }},
");

        foreach (var sw in sws) {
            sb.AppendLine(GetOptDictLine(sw.Desc.LongName, sw.Desc.Alias, "set_" + sw.BackingSymbol.Name));
        }

        sb.AppendLine("};");

        foreach (var sw in sws) {
            string expr = "";

            if (isRoot)
                expr = CliClassName + ".";

            var argExpr = GetParsingExpression(sw.Type, sw.DefaultValueExpr);

            if (sw is MethodOption methodOpt) {
                expr = methodOpt.BackingSymbol.ToString() + '(' + argExpr + " ?? \"\")";

                if (methodOpt.NeedsAutoHandling) {
                    expr = "ThrowIfNotValid(" + expr + ")";
                }
            } else {
                expr += Utils.GetSafeName(sw.BackingSymbol.Name) + " = " + argExpr;
            }

            sb.AppendLine(
                GetOptFuncLine(
                    "set_" + sw.BackingSymbol.Name,
                    expr
                )
            );
        }

        if (!isRoot) {
            foreach (var sw in sws) {
                if (sw is MethodOption)
                    continue;

                sb
                    .Append("private static bool ")
                    .Append(Utils.GetSafeName(sw.BackingSymbol.Name));

                if (sw.DefaultValueExpr is not null) {
                    sb
                        .Append(" = ")
                        .Append(sw.DefaultValueExpr);
                }

                sb
                    .AppendLine(";")
                    ;
            }
        }

        #endregion
        #region Options

        sb.AppendLine($@"private static Dictionary<string, Action<string>> _options = new() {{");

        foreach (var opt in opts) {
            sb.AppendLine(GetOptDictLine(opt.Desc.LongName, opt.Desc.Alias, opt.BackingSymbol.Name + "Action"));
        }

        sb.AppendLine("};");

        foreach (var opt in opts) {
            string expr = "";

            if (opt is MethodOption methodOpt) {
                string argExpr = "";

                if (!methodOpt.IsSwitch) {
                    argExpr = "__arg";

                    if (!methodOpt.BackingSymbol.Parameters[0].IsNullable) {
                        argExpr += "?? \"\"";
                    }
                }

                expr = methodOpt.BackingSymbol.ToString() + '(' + argExpr + ')';

                if (methodOpt.NeedsAutoHandling) {
                    expr = "ThrowIfNotValid(" + expr + ")";
                }
            } else {
                expr = (isRoot ? CliClassName  + "." : "") + Utils.GetSafeName(opt.BackingSymbol.Name) + " = " + GetParsingExpression(opt.Type, opt.DefaultValueExpr);
            }

            sb.AppendLine(
                GetOptFuncLine(
                    opt.BackingSymbol.Name + "Action",
                    expr
                )
            );
        }

        if (!isRoot) {
            foreach (var opt in opts) {
                if (opt is MethodOption)
                    continue;

                sb
                    .Append("private static ")
                    .Append(opt.Type.Name)
                    .Append(' ')
                    .Append(opt.BackingSymbol.Name);

                if (opt.DefaultValueExpr is not null) {
                    sb
                        .Append(" = ")
                        .Append(opt.DefaultValueExpr);
                }

                sb
                    .AppendLine(";")
                    ;
            }
        }

        #endregion
    }

    string GetParsingExpression(MinimalTypeInfo type, string? defaultValExpr) {
        if (type == Utils.BOOLMinInfo) {
            return "AsBool(__arg, " + (defaultValExpr is not null ? "!" + defaultValExpr : "true") + ")";
        } else {
            return "Parse<" + type.FullName + ">(__arg ?? \"\")" + (defaultValExpr is not null ? "??" + defaultValExpr : "");
        }
    }

    void AddArgs(Argument[] posArgs, bool isRoot = true) {
        sb.Append($@"protected override Action<string>[] _posArgs => ");

        if (posArgs.Length == 0) {
            sb.AppendLine("Array.Empty<Action<string>>();");
            return;
        }

        sb.AppendLine("new Action<string>[] {");

        foreach (var arg in posArgs) {
            if (arg.IsParams)
                continue; // could be break since params is always the last parameter

            sb
                .Append("static __arg => ")
                .Append(arg.BackingSymbol.Name)
                .Append(" = ")
                .Append(GetParsingExpression(arg.Type, arg.DefaultValueExpr))
                .Append(",");
        }

        sb.AppendLine("};");


        foreach (var arg in posArgs) {
            if (arg.IsParams)
                continue; // cf above

            sb
                .Append("private static ")
                .Append(arg.Type.Name)
                .Append(' ')
                .Append(arg.BackingSymbol.Name);

            if (arg.DefaultValueExpr is not null) {
                sb
                    .Append(" = ")
                    .Append(arg.DefaultValueExpr);
            }

            sb
                .AppendLine(";")
                ;
        }
    }

    void AppendFunc(MinimalMethodInfo method, bool isRoot = false) {
        sb.Append("private static ");

        var isVoid = method.ReturnVoid;
        var methodParams = method.Parameters;

        if (isVoid) {
            sb.Append("Action");

            if (methodParams.Length != 0)
                sb.Append('<');
        } else {
            sb.Append("Func<");
        }

        sb.Append(String.Join(", ", methodParams.Select(p => p.Type.Name)));

        if (isVoid) {
            if (methodParams.Length != 0)
                sb.Append('>');

        } else {
            if (methodParams.Length != 0)
                sb.Append(", ");

            sb.Append("int>");
        }

        sb
            .Append(" _func = ")
            .Append(method.ToString())
            .AppendLine(";");
    }

    void AddFuncAndInvoke(MinimalMethodInfo method, Option[] optsAndSws, Argument[] posArgs, bool isRoot = false) {
        var isVoid = method.ReturnVoid;
        var methodParams = method.Parameters;

        AppendFunc(method, isRoot);

        sb.Append("internal override Func<int> Invoke => ");

        // if _func is already Func<int>
        if (!isVoid && methodParams.Length == 0) {
            sb.Append("_func");
        } else {
            sb.Append("() => "); // can't be static because of _params

            if (isVoid)
                sb.Append("{ ");

            sb.Append("_func(");

            var defArgName = new string[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++) {
                if (methodParams[i].IsParams)
                    defArgName[i] = "_params.ToArray()";
                else
                    defArgName[i] = Utils.GetSafeName(methodParams[i].Name);
            }

            sb.Append(String.Join(", ", defArgName));

            sb.Append(')');

            if (isVoid)
                sb.Append("; return 0; }");
        }

        sb.AppendLine(";");
    }

    // NOTE: We keep isRoot because we want to be able to force that behavior from AddRoot()
    // even when the rootCmd isn't synthetic
    public void AddCmd(Command cmd, Option[] optsAndSws, Argument[] posArgs, bool isRoot = false) {
        if (!isRoot)
            _allCmdInfo.Add((cmd, optsAndSws, posArgs));

        if (cmd.ParentSymbolName is not null) {
            if (!domToSubTable.TryGetValue(cmd.ParentCmd!, out var list))
                domToSubTable[cmd.ParentCmd!] = new() { cmd };
            else
                list.Add(cmd);
        } else if (!isRoot) {
            domToSubTable[RootCmd].Add(cmd);
        }

        sb.Append($@"
{GetClassDeclarationLine(cmd, isRoot)}
        {(cmd.HasParams ? "protected override bool HasParams => true;" : "")}

        internal {cmd.Name}CmdDesc() : base(_switches, _options) {{}}

        protected {cmd.Name}CmdDesc(
            Dictionary<string, Action<string?>> switches,
            Dictionary<string, Action<string>> options
        )
            : base(_switches.UpdatedWith(switches), _options.UpdatedWith(options))
        {{}}
");

        AddOptsAndSwitches(optsAndSws, isRoot);
        AddArgs(posArgs, isRoot);
        if (cmd.BackingSymbol is not null) // if this is not root
            AddFuncAndInvoke(cmd.BackingSymbol, optsAndSws, posArgs, isRoot);

        sb.AppendLine($@"
    }}
");
    }

    public override string ToString() {
        foreach (var kv in domToSubTable) {
            var dom = kv.Key;
            var subs = kv.Value;

                sb
                .AppendLine(GetClassDeclarationLine(dom, isRoot: dom == RootCmd))
                .AppendLine($@"
        internal override Dictionary<string, Func<CmdDesc>> SubCmds => _subs;
        private static Dictionary<string, Func<CmdDesc>> _subs = new() {{
")
                ;

            foreach (var sub in subs) {
                sb.AppendLine(GetDictLine(sub.Name, "static () => new " + sub.Name + "CmdDesc()"));
            }

            sb
                .AppendLine("};")
                .AppendLine("}");
        }

        foreach (var info in _allCmdInfo) {
            if (!domToSubTable.TryGetValue(info.cmd, out var subList)) {
                subList = new();
            }

            if (info.cmd != RootCmd)
                AddHelpText(info.cmd, subList.ToArray(), info.opts, info.posArgs);
        }

        AddRootHelpText();

        var usingsStr = "";

        foreach (var u in RequiredUsings)
            usingsStr += "using " + u + ";";

        return $@"
{GenFileHeader}
{usingsStr}
static partial class {ProgClassName} {{
" + sb.ToString() + "}";
    }

    static string GetOptFuncLine(string methodName, string expr) {
        return $@"private static void {methodName}(string? __arg) => {expr};";
    }

    static string GetDictLine(string key, string value)
        => $@"{{ ""{key}"", {value} }},";

    static string GetOptDictLine(string longName, char shortName, string methodName) {
        var str = GetDictLine("--" + longName, methodName);

        if (shortName is not '\0')
            str += GetDictLine("-" + shortName, methodName);

        return str;
    }

    public static string GetClassDeclarationLine(Command cmd, bool isRoot)
        => $@"
#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class {cmd.Name}CmdDesc : {cmd.ParentSymbolName ?? ""}CmdDesc {{
";
}