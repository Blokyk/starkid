using Recline.Generator.Model;

using static Recline.Generator.Resources;

namespace Recline.Generator;

internal class CmdDescBuilder
{
    public string CliClassName { get; }
    public ImmutableArray<string> RequiredUsings { get; }
    public string AppName { get; }
    public string? Description { get; }

    readonly Dictionary<Command, ImmutableArray<Command>.Builder> domToSubTable;

    Command RootCmd { get; }
    ImmutableArray<Argument> RootArgs { get; }
    ImmutableArray<Option> RootOptsAndSws { get; }

    private record CmdInfo(Command cmd, ImmutableArray<Option> opts, ImmutableArray<Argument> posArgs);

    readonly ImmutableArray<CmdInfo>.Builder _allCmdInfo
        = ImmutableArray.CreateBuilder<CmdInfo>();

    /// <summary>
    /// Whether the root command can actually be invoked or not
    /// </summary>
    /// <value></value>
    bool IsRootCmdReal { get; }

    public int HelpExitCode { get; set; } = 0;

    private readonly StringBuilder sb;

    public CmdDescBuilder(
        string appName,
        string cliClassName,
        ImmutableArray<string> usings,
        (Command cmd, ImmutableArray<Argument> args)? cmdAndArgs,
        ImmutableArray<Option> optsAndSws,
        string? description
    ) {
        AppName = appName;
        CliClassName = cliClassName;
        RequiredUsings = usings;
        sb = new StringBuilder();
        Description = description;

        IsRootCmdReal = cmdAndArgs.HasValue;

        if (cmdAndArgs.HasValue) {
            RootArgs = cmdAndArgs.Value.args;
            RootCmd = cmdAndArgs.Value.cmd;
        } else {
            RootArgs = ImmutableArray<Argument>.Empty;
            RootCmd = new Command(true, appName, Description, ImmutableArray<Option>.Empty, RootArgs);
        }

        RootOptsAndSws = optsAndSws;

        domToSubTable = new() {
            { RootCmd, ImmutableArray.CreateBuilder<Command>() }
        };

        AddRoot();
    }

    void AddRootHelpText() => AddHelpText(
            RootCmd,
            domToSubTable[RootCmd].ToImmutable(),
            RootOptsAndSws,
            RootArgs
        );

    public void AddHelpText(Command cmd, ImmutableArray<Command> subs, ImmutableArray<Option> opts, ImmutableArray<Argument> posArgs) {
        var help = new CmdHelp(
            cmd.ParentCmd?.Name ?? (cmd.IsRoot ? null : RootCmd.Name),
            cmd.Name,
            cmd.Description,
            ImmutableArray.CreateRange(opts, o => o.Desc),
            ImmutableArray.CreateRange(posArgs, a => a.Desc),
            ImmutableArray.CreateRange(subs, s => s.GetDesc()),
            IsRootCmd: cmd.IsRoot,
            HasParams: cmd.HasParams
        );

        var helpStr = help.ToString();

        sb.Append('\n').Append(GetClassDeclarationLine(cmd)).Append(@"
        public override string Name => """).Append(cmd.Name).Append(@""";
        internal override string HelpString => _helpString;
        private static readonly string _helpString = ").Append(SyntaxFactory.Literal(helpStr)).Append(@";

        private static void DisplayHelp(string? val) {
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit(").Append(HelpExitCode).Append(@");
        }
    }
");
    }

    void AddRoot() {
        sb.Append(@"
    private abstract partial class CmdDesc {
        private static readonly Lazy<CmdDesc> _lazyRoot = new(static () => new ").Append(RootCmd.ClassPrefix).AppendLine(@"CmdDesc(), false);
        internal static CmdDesc root => _lazyRoot.Value;");

        AddOptsAndFlags(RootOptsAndSws, isRoot: true);

        sb.Append(@"
    }");

        if (IsRootCmdReal)
            AddCmd(RootCmd, RootOptsAndSws, RootArgs);
    }

    void AddOptsAndFlags(ImmutableArray<Option> optsAndFlags, bool isRoot = false) {
        var opts = ImmutableArray.CreateBuilder<Option>(optsAndFlags.Length / 2);
        var flags = ImmutableArray.CreateBuilder<Option>(optsAndFlags.Length / 2);

        foreach (var thing in optsAndFlags) {
            if (thing.IsFlag)
                flags.Add(thing);
            else
                opts.Add(thing);
        }

        #region Flags

        sb.Append(@"
        private static Dictionary<string, Action<string?>> _flags = new() {
            { ""--help"", DisplayHelp }, { ""-h"", DisplayHelp },
");

        foreach (var sw in flags) {
            sb
                .Append("\t\t\t")
                .AppendLine(
                    GetOptDictLine(
                        sw.Desc.LongName,
                        sw.Desc.Alias,
                        "set_" + sw.BackingSymbol.Name
                    )
                )
                ;
        }

        sb
            .AppendLine("\t\t};")
            .AppendLine()
            ;

        foreach (var flag in flags) {
            var argExpr = GetParsingExpression(flag.Parser, flag.BackingSymbol.Name, flag.DefaultValueExpr);
            var validExpr = GetValidatingExpression(argExpr, flag.Desc.LongName, flag.Validator);

            string expr
                = (!isRoot ? "" : CliClassName + ".")
                + SymbolUtils.GetSafeName(flag.BackingSymbol.Name) + " = " + validExpr;

            sb
                .Append("\t\t")
                .AppendLine(
                    GetOptFuncLine(
                        "set_" + flag.BackingSymbol.Name,
                        expr
                    )
                )
                ;
        }

        if (!isRoot) {
            foreach (var sw in flags) {
                sb
                    .Append(@"
        private static bool ")
                    .Append(SymbolUtils.GetSafeName(sw.BackingSymbol.Name))
                    ;

                if (sw.DefaultValueExpr is not null) {
                    sb
                        .Append(" = ")
                        .Append(sw.DefaultValueExpr)
                        ;
                }

                sb
                    .AppendLine(";")
                    ;
            }
        }

        #endregion
        #region Options

        sb.AppendLine(@"
        private static Dictionary<string, Action<string>> _options = new() {");

        foreach (var opt in opts) {
            sb
                .Append("\t\t\t")
                .AppendLine(
                    GetOptDictLine(
                        opt.Desc.LongName,
                        opt.Desc.Alias,
                        opt.BackingSymbol.Name + "Action"
                    )
                )
                ;
        }

        sb
            .AppendLine("\t\t};")
            .AppendLine()
            ;

        foreach (var opt in opts) {
            var argExpr = GetParsingExpression(opt.Parser, opt.BackingSymbol.Name, opt.DefaultValueExpr);
            var validExpr = GetValidatingExpression(argExpr, opt.Desc.LongName, opt.Validator);

            string expr
                = (!isRoot ? "" : CliClassName + ".")
                + SymbolUtils.GetSafeName(opt.BackingSymbol.Name) + " = " + validExpr;

            sb
                .Append("\t\t")
                .AppendLine(
                    GetOptFuncLine(
                        opt.BackingSymbol.Name + "Action",
                        expr
                    )
                )
                ;
        }

        if (!isRoot) {
            foreach (var opt in opts) {
                sb
                    .Append("\t\tprivate static ")
                    .Append(opt.Type.FullName)
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

    static string GetParsingExpression(ParserInfo parser, string? argName, string? defaultValueExpr) {
        if (parser == ParserInfo.AsBool) {
            var name = ParserInfo.AsBool.FullName;
            if (defaultValueExpr is null)
                return name + "(__arg)";
            else
                return name + "(__arg, " + defaultValueExpr + ")";
        }

        string expr = "";

        if (parser.TargetType == CommonTypes.BOOLMinInfo) {
            expr = "__arg is null ? true : ";
        }

        expr += parser switch {
            ParserInfo.Identity => "__arg" + (argName is null ? "" : " ?? " + argName),
            ParserInfo.DirectMethod dm => "ThrowIfParseError<" + parser.TargetType.FullName + ">(" + dm.FullName + ", __arg ?? \"\")",
            ParserInfo.Constructor ctor => "new " + ctor.TargetType.FullName + "(__arg ?? \"\")",
            ParserInfo.BoolOutMethod bom => "ThrowIfTryParseNotTrue<" + parser.TargetType.FullName + ">(" + bom.FullName + ", __arg ?? \"\")",
            _ => throw new Exception(parser.GetType().Name + " is not a supported ParserInfo type."),
        };

        if (parser.TargetType.IsNullable && defaultValueExpr is not null)
            expr = '(' + expr + " ?? " + defaultValueExpr + ')';

        return expr;
    }

    static string GetValidatingExpression(string argExpr, string argName, ValidatorInfo? validator) {
        if (validator is null)
            return argExpr;

        return validator switch {
            ValidatorInfo.Method validatorMethod
                =>  "ThrowIfNotValid(" +
                        $"{argExpr}, " +
                        $"{validatorMethod.FullName}, " +
                        $"\"{argName}\", " +
                        $"{(validator.Message is null ? "null" : SyntaxFactory.Literal(validator.Message))}, " +
                        $"\"{validatorMethod.MethodInfo.Name}\"" +
                    ")",
            _ => throw new Exception(validator.GetType().Name + " is not a supported ValidatorInfo type."),
        };
    }

    void AddArgs(ImmutableArray<Argument> posArgs) {
        sb.Append(@"
        protected override Action<string>[] _posArgs => ");

        if (posArgs.Length == 0) {
            sb.AppendLine("Array.Empty<Action<string>>();");
            return;
        }

        sb.AppendLine("new Action<string>[] {");

        foreach (var arg in posArgs) {
            if (arg.IsParams)
                continue; // could be break since params is always the last parameter

            sb
                .Append("\t\t\tstatic __arg => ")
                .Append(arg.BackingSymbol.Name)
                .Append(" = ")
                .Append(
                    GetValidatingExpression(
                        GetParsingExpression(arg.Parser, null, arg.DefaultValueExpr),
                        arg.Desc.Name,
                        arg.Validator
                    )
                )
                .Append(",");
        }

        sb.AppendLine(@"
        };").AppendLine();

        foreach (var arg in posArgs) {
            if (arg.IsParams)
                continue; // cf above

            sb
                .Append("\t\tprivate static ")
                .Append(arg.Type.FullName)
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

    void AppendFunc(MinimalMethodInfo method) {
        sb.Append(@"
        private static ");

        var isVoid = method.ReturnsVoid;
        var methodParams = method.Parameters;

        if (isVoid) {
            sb.Append("Action");

            if (methodParams.Length != 0)
                sb.Append('<');
        } else {
            sb.Append("Func<");
        }

        sb.Append(String.Join(", ", methodParams.Select(p => p.Type.FullName)));

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

    void AddFuncAndInvoke(MinimalMethodInfo method) {
        var isVoid = method.ReturnsVoid;
        var methodParams = method.Parameters;

        AppendFunc(method);

        sb.Append(@"
        internal override Func<int> Invoke => ");

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
                defArgName[i]
                    = methodParams[i].IsParams
                        ? "_params.ToArray()"
                        : SymbolUtils.GetSafeName(methodParams[i].Name);
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
    public void AddCmd(Command cmd, ImmutableArray<Option> optsAndSws, ImmutableArray<Argument> posArgs) {
        if (!cmd.IsRoot)
            _allCmdInfo.Add(new(cmd, optsAndSws, posArgs));

        if (!cmd.IsTopLevel) {
            if (!domToSubTable.TryGetValue(cmd.ParentCmd, out var list)) {
                list = ImmutableArray.CreateBuilder<Command>();
                domToSubTable.Add(cmd.ParentCmd, list);
            }

            list.Add(cmd);
        } else if (!cmd.IsRoot) {
            domToSubTable[RootCmd].Add(cmd);
        }

        sb
        .AppendLine()
        .Append(GetClassDeclarationLine(cmd))
        .Append(cmd.HasParams ? @"
        protected override bool HasParams => true;" : "").Append(@"

        internal ").Append(cmd.ClassPrefix).Append(@"CmdDesc() : base(_flags, _options) {}

        protected ").Append(cmd.ClassPrefix).Append(@"CmdDesc(
            Dictionary<string, Action<string?>> flags,
            Dictionary<string, Action<string>> options
        )
            : base(UpdateWith(_flags, flags), UpdateWith(_options, options))
        {}
");

        AddOptsAndFlags(optsAndSws, cmd.IsRoot);
        AddArgs(posArgs);
        if (!cmd.IsRoot) // if this is not root
            AddFuncAndInvoke(cmd.BackingSymbol);

        sb.AppendLine(@"
    }
");
    }

    public override string ToString() {
        foreach (var kv in domToSubTable) {
            var dom = kv.Key;
            var subs = kv.Value;

                sb
                .AppendLine(GetClassDeclarationLine(dom))
                .AppendLine(@"
        internal override Dictionary<string, Func<CmdDesc>> SubCmds => _subs;
        private static Dictionary<string, Func<CmdDesc>> _subs = new() {")
                ;

            foreach (var sub in subs) {
                sb.Append("\t\t\t").AppendLine(GetDictLine(sub.Name, "static () => new " + sub.ClassPrefix + "CmdDesc()"));
            }

            sb
                .AppendLine("\t\t};")
                .AppendLine("\t}");
        }

        foreach (var info in _allCmdInfo) {
            if (!domToSubTable.TryGetValue(info.cmd, out var subList)) {
                subList = ImmutableArray.CreateBuilder<Command>();
                domToSubTable.Add(info.cmd, subList);
            }

            if (info.cmd != RootCmd)
                AddHelpText(info.cmd, subList.ToImmutable(), info.opts, info.posArgs);
        }

        AddRootHelpText();

        var usingsStr = "";

        foreach (var u in RequiredUsings)
            usingsStr += "using " + u + ";";

        return $@"
{GenFileHeader}
{usingsStr}

internal static partial class {ProgClassName} {{
" + sb.ToString() + "}";
    }

    static string GetOptFuncLine(string methodName, string expr)
        => $"private static void {methodName}(string? __arg) => {expr};";

    static string GetDictLine(string key, string value)
        => $@"{{ ""{key}"", {value} }},";

    static string GetOptDictLine(string longName, char shortName, string methodName) {
        var str = GetDictLine("--" + longName, methodName);

        if (shortName is not '\0')
            str += ' ' + GetDictLine("-" + shortName, methodName);

        return str;
    }

    public static string GetClassDeclarationLine(Command cmd)
        => $@"
#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class {cmd.ClassPrefix}CmdDesc : {cmd.ParentCmd?.ClassPrefix ?? ""}CmdDesc {{";
}