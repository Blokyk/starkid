namespace Recline.Generator;

internal static class Resources {
    public const int MAX_LINE_LENGTH = 80;

    //TODO: add list of parsable types for options

    public const string CLIAttribName = nameof(Recline.CLIAttribute);
    public const string CmdAttribName = nameof(Recline.CommandAttribute);
    public const string DescAttribName = nameof(Recline.DescriptionAttribute);
    public const string OptAttribName = nameof(Recline.OptionAttribute);
    public const string SubCmdAttribName = nameof(Recline.SubCommandAttribute);

    public const string UsingsList = $@"
#nullable enable
using System;
using System.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using {GenNamespace};
";

    public const string GenNamespace = "Recline.Generated";
    public const string ClassPrefix = "Recline";

    public const string GenFileHeader = $@"
{UsingsList}

namespace {GenNamespace};
";

    public const string ProgClassName = ClassPrefix + "Program";
    public const string ProgClassStr = $@"{GenFileHeader}
internal static partial class {ProgClassName} {{
    private static CmdDesc currCmdDesc = CmdDesc.root;
    private static string prevCmdName = CmdDesc.root.Name;

    static int Main(string[] args) {{
        var argCount = 0;

        var onlyArgs = false; // set to true if we get '--'

        for (int i = 0; i < args.Length; i++) {{
            var rawArg = args[i];
            var parts = rawArg.Split(new[] {{ '=' }}, 2);

            var arg = parts[0];
            var value = parts.ElementAtOrDefault(1);

            if (!onlyArgs) {{
                if (currCmdDesc.flags.TryGetValue(arg, out var actFlag)) {{
                    if (!TryDoAction(actFlag, arg, value!, rawArg, currCmdDesc))
                        return 1;
                    continue;
                }}

                if (currCmdDesc.Options.TryGetValue(arg, out var actOpt)) {{
                    if (value is null) {{
                        if (!TryGetNext(ref i, args, out value)) {{
                            Console.Error.WriteLine(GetHelpString(""Option {{0}} needs an argument"", arg, currCmdDesc));
                            return 1;
                        }}

                        rawArg += "" "" + value;
                    }}

                    if (!TryDoAction(actOpt, arg, value, rawArg, currCmdDesc))
                        return 1;
                    continue;
                }}

                if (currCmdDesc.SubCmds.TryGetValue(arg, out var getSubCmdDesc)) {{
                    var oldArgCount = currCmdDesc.PosArgCount;

                    prevCmdName = currCmdDesc.Name;
                    currCmdDesc = getSubCmdDesc();

                    if (oldArgCount != 0) {{
                        Console.Error.WriteLine(GetHelpString(""Can't invoke sub-command '{{0}}' with arguments for parent command '"" + prevCmdName + ""'"", currCmdDesc.Name));
                        return 1;
                    }}

                    continue;
                }}

                if (arg[0] == '-') {{
                    if (arg[1] == '-') {{
                        onlyArgs = true;
                        continue;
                    }}

                    Console.Error.WriteLine(GetHelpString(""Couldn't understand '{{0}}' in this context"", rawArg, currCmdDesc));
                    return 1;
                }}
            }}

            if (!currCmdDesc.TryAddPosArg(arg)) {{
                Console.Error.WriteLine(GetHelpString(""Couldn't understand '{{0}}' in this context"", rawArg, currCmdDesc));
                return 1;
            }}
        }}

        if (currCmdDesc.ArgSlotsLeft > 0) {{
            Console.Error.WriteLine(GetHelpString(""Expected at least {{0}} arguments, but only got "" + argCount, (currCmdDesc.ArgSlotsLeft + argCount).ToString(), currCmdDesc));
            return 1;
        }}

        return currCmdDesc.Invoke();
    }}

    static bool TryDoAction(Action<string> act, string arg1, string arg2, string rawArg, CmdDesc desc) {{
        try {{
            act(arg2);
            return true;
        }}
        catch (FormatException e) {{
            Console.Error.WriteLine(
                GetHelpString(
                    ""Expression '{{0}}' is not valid in this context""
                    + (String.IsNullOrEmpty(e.Message) ? """" : ("": \x1b[1m'"" + e.Message + ""'"")),
                    rawArg,
                    desc
                )
            );
            return false;
        }}
    }}

    static bool TryGetNext(ref int i, string[] args, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? nextArg) {{
        if (args.Length == (i + 1)) {{
            nextArg = null;
            return false;
        }}

        nextArg = args[++i];
        return true;
    }}

    static string InRed(string msg)
        => ""\x1b[31m"" + msg + ""\x1b[0m\n"";
    static string FormatError(string reason, object obj1)
        => InRed(string.Format(reason, ""\x1b[1m"" + obj1.ToString() + ""\x1b[22m""));

    static string GetHelpString(string reason, CmdDesc desc)
        => InRed(reason) + desc.HelpString;

    static string GetHelpString(string reason)
        => GetHelpString(reason, currCmdDesc);

    static string GetHelpString(string reason, string argName, CmdDesc desc)
        => GetHelpString(FormatError(reason, argName), desc);

    static string GetHelpString(string reason, string argName)
        => GetHelpString(reason, argName, currCmdDesc);

    internal static void UpdateWith<TKey, TValue>(
        this Dictionary<TKey, TValue> dic1,
        Dictionary<TKey, TValue> dic2
    ) where TKey : notnull
    {{
        foreach (var kvPair in dic2) {{
            if (!dic1.TryAdd(kvPair.Key, kvPair.Value)) {{
                dic1[kvPair.Key] = kvPair.Value;
            }}
        }}
    }}

    internal static Dictionary<TKey, TValue> UpdatedWith<TKey, TValue>(
        this Dictionary<TKey, TValue> dic1,
        Dictionary<TKey, TValue> dic2
    ) where TKey : notnull
    {{
        var newDic = new Dictionary<TKey, TValue>(dic2);

        foreach (var kvPair in dic1) {{
            newDic.TryAdd(kvPair.Key, kvPair.Value);
        }}

        return newDic;
    }}

    internal static T Parse<T>(string? str) => default(T)!;

    internal static bool AsBool(string? val, bool defaultFlag) {{
        if (val is null)
            return defaultFlag;

        if (val is ""true"")
            return true;

        if (val is ""false"")
            return false;

        throw new FormatException(""Couldn't understand '"" + val + ""' as a boolean value"");
    }}

    internal static void ThrowIfNotValid(bool val, [CallerArgumentExpression(""val"")] string expr = """") {{
        if (!val)
            throw new Exception(""Expression '"" + expr + ""' returned false"");
    }}

    internal static void ThrowIfNotValid(int val, [CallerArgumentExpression(""val"")] string expr = """") {{
        if (val != 0)
            throw new Exception(""Expression '"" + expr + ""' returned a non-zero value"");
    }}

    internal static void ThrowIfNotValid(string? val) {{
        if (String.IsNullOrEmpty(val))
            throw new Exception(val);
    }}

    internal static void ThrowIfNotValid(Exception? e) {{
        if (e is not null)
            throw e;
    }}

    private abstract partial class CmdDesc {{
        public abstract string Name {{ get; }}
        private static Dictionary<string, Func<CmdDesc>> _subs = new() {{}};

        private static readonly Func<int> _func = static () => {{
            Console.Error.WriteLine(GetHelpString(""No command provided"", CmdDesc.root));
            return 1;
        }};

        protected virtual Action<string>[] _posArgs {{ get; }} = Array.Empty<Action<string>>();

        internal Dictionary<string, Action<string?>> flags => _flags;
        internal Dictionary<string, Action<string>> Options => _options;
        internal virtual Dictionary<string, Func<CmdDesc>> SubCmds => _subs;
        internal virtual Func<int> Invoke => _func;
        internal virtual string HelpString {{ get; }}

#nullable disable
        internal CmdDesc() {{}}

        protected CmdDesc(
#nullable restore
            Dictionary<string, Action<string?>> flags,
            Dictionary<string, Action<string>> options
        ) {{
            _flags.UpdateWith(flags);
            _options.UpdateWith(options);
        }}

        private int posArgIdx = 0;
        public int PosArgCount => posArgIdx + _params.Count;
        protected virtual bool HasParams {{ get; }} = false;

        protected List<string> _params = new();

        internal int ArgSlotsLeft => _posArgs.Length - posArgIdx;

        internal bool TryAddPosArg(string arg) {{
            if (posArgIdx < _posArgs.Length) {{
                _posArgs[posArgIdx++](arg);
                return true;
            }} else {{
                if (!HasParams)
                    return false;

                _params.Add(arg);
                return true;
            }}
        }}

        private static void DisplayHelp(string? val) {{
            Console.Error.WriteLine(root.HelpString);
            System.Environment.Exit(0);
        }}
    }}
}}
";
}