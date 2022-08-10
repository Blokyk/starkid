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
    void AddBaseRoot()
        => sb.AppendLine($@"
    private abstract partial class CmdDesc {{
        private static readonly Lazy<CmdDesc> _lazyRoot = new(static () => new {AppName}CmdDesc(), false);
        internal static CmdDesc root => _lazyRoot.Value;
    }}");

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
        //TODO: this

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
            cmd.ParentSymbolName,
            cmd.Name,
            cmd.Description,
            opts.Select(o => o.Desc).ToArray(),
            posArgs.Select(a => a.Desc).ToArray(),
            subs.Select(s => s.WithArgsDesc).ToArray(),
            IsDirectCmd: isRoot
        );

        var helpSb = new StringBuilder();

        help.AppendTo(helpSb);

        sb.Append($@"
{GetClassDeclarationLine(cmd, isRoot)}
        internal override string HelpString => _helpString;
        private static readonly string _helpString = {SyntaxFactory.Literal(helpSb.ToString()).ToString()};

        private static void DisplayHelp(string origFlag, string? val) {{
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit(0);
        }}
    }}
");
    }

    void AddRoot() {
        sb.AppendLine($@"
    private abstract partial class CmdDesc {{
");

        AddOptsAndSwitches(RootOptsAndSws, isRoot: true);

        sb.AppendLine($@"
    }}");

        if (HasRealRootCmd)
            AddCmd(RootCmd, RootOptsAndSws, RootArgs, true);

    }

    void AddOptsAndSwitches(Option[] optsAndSws, bool isRoot = false) {
        var opts = new List<Option>(optsAndSws.Length);
        var sws = new List<Option>(optsAndSws.Length);

        foreach (var thing in optsAndSws) {
            if (Utils.Equals(thing.Type, Utils.BOOL))
                sws.Add(thing);
            else
                opts.Add(thing);
        }

        #region Switches

        sb.AppendLine($@"private static Dictionary<string, Action<string, string?>> _switches = new() {{");

        sb.AppendLine($@"
            {{ ""--help"", DisplayHelp }},
            {{ ""-h"", DisplayHelp }},
");

        foreach (var sw in sws) {
            sb.AppendLine(GetOptDictLine(sw.Desc.LongName, sw.Desc.Alias, "set_" + sw.Desc.Name));
        }

        sb.AppendLine("};");

        foreach (var sw in sws) {
            string expr = "";

            if (isRoot)
                expr = CliClassName + ".";

            var argExpr = "AsBool(arg, ";

            if (sw.DefaultValue is null)
                argExpr += "true";
            else
                argExpr += "!" + sw.DefaultValue.ToString();

            argExpr += ")";

            if (sw is MethodOption methodOpt) {
                expr += methodOpt.BackingSymbol.Name + '(' + argExpr + " ?? \"\")";

                if (methodOpt.NeedsAutoHandling) {
                    expr = "ThrowIfNotValid(" + expr + ")";
                }
            } else {
                expr += sw.BackingSymbol.Name + " = " + argExpr;
            }

            sb.AppendLine(
                GetOptFuncLine(
                    "set_" + sw.Desc.Name,
                    expr
                )
            );
        }

        if (!isRoot) {
            foreach (var sw in sws) {
                sb
                    .Append("private static ")
                    .Append(sw.Type.Name)
                    .Append(' ')
                    .Append(sw.BackingSymbol.Name);

                if (sw.DefaultValue is not null) {
                    sb
                        .Append(" = ")
                        .Append(sw.DefaultValue.ToString());
                }

                sb
                    .AppendLine(";")
                    ;
            }
        }

        #endregion
        #region Options

        sb.AppendLine($@"private static Dictionary<string, Action<string, string>> _options = new() {{");

        foreach (var opt in opts) {
            sb.AppendLine(GetOptDictLine(opt.Desc.LongName, opt.Desc.Alias, opt.Desc.Name + "Action"));
        }

        sb.AppendLine("};");

        foreach (var opt in opts) {
            string expr = "";

            if (isRoot)
                expr = CliClassName + ".";

            if (opt is MethodOption methodOpt) {
                string argExpr = "arg";

                if (methodOpt.BackingSymbol.Parameters[0].NullableAnnotation != NullableAnnotation.Annotated) {
                    argExpr += "?? \"\"";
                }

                expr += methodOpt.BackingSymbol.Name + '(' + argExpr + ')';

                if (methodOpt.NeedsAutoHandling) {
                    expr = "ThrowIfNotValid(" + expr + ")";
                }
            } else {
                expr += opt.BackingSymbol.Name + " = Parse<" + opt.Type.Name + ">(arg ?? \"\")";
            }

            sb.AppendLine(
                GetOptFuncLine(
                    opt.Desc.Name + "Action",
                    expr
                )
            );
        }

        if (!isRoot) {
            foreach (var opt in opts) {
                sb
                    .Append("private static ")
                    .Append(opt.Type.GetNameWithNull())
                    .Append(' ')
                    .Append(opt.BackingSymbol.Name);

                if (opt.DefaultValue is not null) {
                    sb
                        .Append(" = ")
                        .Append(opt.DefaultValue.ToString());
                }

                sb
                    .AppendLine(";")
                    ;
            }
        }

        #endregion
    }

    void AddArgs(Argument[] posArgs, bool isRoot = true) {
        sb.Append($@"protected override Action<string>[] _posArgs => ");

        if (posArgs.Length == 0)
            sb.Append("Array.Empty<Action<string>>();//");
        else
            sb.AppendLine("new Action<string>[] {");

        foreach (var arg in posArgs) {
            sb
                .Append("static arg => ")
                .Append(arg.BackingSymbol.Name)
                .Append(" = Parse<")
                .Append(arg.Type.Name)
                .AppendLine(">(arg),");
        }

        sb.AppendLine("};");

        foreach (var arg in posArgs) {
            sb
                .Append("private static ")
                .Append(arg.Type.GetNameWithNull())
                .Append(' ')
                .Append(arg.BackingSymbol.Name);

            if (arg.DefaultValue is not null) {
                sb
                    .Append(" = ")
                    .Append(arg.DefaultValue.ToString());
            }

            sb
                .AppendLine(";")
                ;
        }
    }

    void AppendFunc(IMethodSymbol method, bool isRoot = false) {
        sb.Append("private static ");

        var isVoid = method.ReturnsVoid;
        var methodParams = method.Parameters;

        if (isVoid) {
            sb.Append("Action");

            if (methodParams.Length != 0)
                sb.Append('<');
        } else {
            sb.Append("Func<");
        }

        sb.Append(String.Join(", ", methodParams.Select(p => p.Type.GetNameWithNull())));

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
            .Append(method.GetFullName())
            .AppendLine(";");
    }

    void AddFuncAndInvoke(IMethodSymbol method, Option[] optsAndSws, Argument[] posArgs, bool isRoot = false) {
        var isVoid = method.ReturnsVoid;
        var methodParams = method.Parameters;

        AppendFunc(method, isRoot);

        sb.Append("internal override Func<int> Invoke => ");

        // if _func is already Func<int>
        if (!isVoid && methodParams.Length == 0) {
            sb.Append("_func");
        } else {
            sb.Append("static () => ");

            if (isVoid)
                sb.Append("{ ");

            sb.Append("_func(");

            var defArgName = new string[methodParams.Length];

            int currPosArgIdx = 0;

            for (int i = 0; i < methodParams.Length; i++) {
                var paramOpt = optsAndSws.FirstOrDefault(
                    opt => Utils.Equals(opt.BackingSymbol, methodParams[i])
                );

                if (paramOpt is not null) {
                    // the opt generator uses backing symbol for field names
                    defArgName[i] = paramOpt.BackingSymbol.Name;
                } else {
                    defArgName[i] = posArgs[currPosArgIdx++].BackingSymbol.Name;
                }
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
        } else {
            domToSubTable[RootCmd].Add(cmd);
        }

        sb.Append($@"
{GetClassDeclarationLine(cmd, isRoot)}
        internal {cmd.Name}CmdDesc() : base(_switches, _options) {{}}

        protected {cmd.Name}CmdDesc(
            Dictionary<string, Action<string, string?>> switches,
            Dictionary<string, Action<string, string>> options
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
        AddBaseRoot();

        foreach (var kv in domToSubTable) {
            var dom = kv.Key;
            var subs = kv.Value;

                sb
                .AppendLine(GetClassDeclarationLine(dom, isRoot: false))
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
        return $@"private static void {methodName}(string origFlag, string? arg) => {expr};";
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