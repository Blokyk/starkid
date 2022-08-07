namespace CLIGen.Generator;

internal static class Ressources {
    public const int MAX_LINE_LENGTH = 80;

    public const string CLIAttribName = nameof(CLIGen.CLIAttribute);
    public const string CmdAttribName = nameof(CLIGen.CommandAttribute);
    public const string DescAttribName = nameof(CLIGen.DescriptionAttribute);
    public const string OptAttribName = nameof(CLIGen.OptionAttribute);
    public const string SubCmdAttribName = nameof(CLIGen.SubCommandAttribute);

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

    public static string GetMainErrorStmts(string msg) => $@"
Console.Error.WriteLine(GetHelpString({msg}, currCmdDesc));
return 1;
";

    public const string GenNamespace = "CLIGen.Generated";

    public const string GenFileHeader = $@"
{UsingsList}

namespace {GenNamespace};
";

    public const string ProgClassName = "Program";
    public const string ProgClassStr = $@"{GenFileHeader}
static partial class Program {{
    static int Main(string[] args) {{
        Console.WriteLine(""!test!"");
        return 0;
        /*
        var currCmdDesc = new __CmdDesc();

        var onlyArgs = false;

        for (int i = 0; i < args.Length; i++) {{
            var rawArg = args[i];
            var parts = rawArg.Split(new[] {{ '=' }}, 2);

            var arg = parts[0];
            var value = parts.ElementAtOrDefault(1);

            if (!onlyArgs) {{

            if (currCmdDesc.Switches.TryGetValue(arg, out var actSwitch)) {{
                if (!TryDoAction(actSwitch, arg, value!, rawArg, currCmdDesc))
                    return 1;
                continue;
            }}

            if (currCmdDesc.Options.TryGetValue(arg, out var actOpt)) {{
                if (value is null) {{
                    if (!TryGetNext(ref i, args, out value)) {{
                        Console.Error.WriteLine(GetHelpString(""Option "" + arg + "" needs an argument"", currCmdDesc));
                        return 1;
                    }}
                }}

                if (!TryDoAction(actOpt, arg, value, rawArg, currCmdDesc))
                    return 1;
                continue;
            }}

            if (currCmdDesc.SubCmds.TryGetValue(arg, out var getSubCmdDesc)) {{
                currCmdDesc = getSubCmdDesc();
                continue;
            }}

            if (arg[0] == '-') {{
                if (arg[1] == '-') {{
                    onlyArgs = true;
                    continue;
                }}

                Console.Error.WriteLine(GetHelpString(""Couldn't understand '"" + rawArg + ""' in this context"", currCmdDesc));
                return 1;
            }}
            }}

            if (!currCmdDesc.TryAddPosArg(arg)) {{
                Console.Error.WriteLine(GetHelpString(""Couldn't understand '"" + rawArg + ""' in this context"", currCmdDesc));
                return 1;
            }}
        }}

        return currCmdDesc.Invoke();*/
    }}

    static bool TryDoAction(Action<string, string> act, string arg1, string arg2, string rawArg, __CmdDesc desc) {{
        try {{
            act(arg1, arg2);
            return true;
        }}
        catch (FormatException) {{
            Console.Error.WriteLine(GetHelpString(""Couldn't understand '"" + rawArg + ""' in this context"", desc));
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

    static string GetHelpString(string reason, __CmdDesc desc) => reason + desc.helpString;
}}
";

    public const string UtilsClassName = "CLIGenUtils";
    public const string UtilsClassStr = $@"{GenFileHeader}

internal static class {UtilsClassName} {{
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

    internal static T Parse<T>(string str) => default(T)!;

    internal static bool AsBool(string? val, bool defaultVal) => val is null ? defaultVal : Boolean.Parse(val);
}}
";
}