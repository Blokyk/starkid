namespace Recline.Generator;

internal static class Resources {
    public static int MAX_LINE_LENGTH = 80;

    public const string CmdGroupAttribName = nameof(Recline.CommandGroupAttribute);
    public const string CmdAttribName = nameof(Recline.CommandAttribute);
    public const string OptAttribName = nameof(Recline.OptionAttribute);
    public const string ParseWithAttribName = nameof(Recline.ParseWithAttribute);
    public const string ValidateWithAttribName = nameof(Recline.ValidateWithAttribute);

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
    public const string ProgClassStr = $$"""
{{GenFileHeader}}
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal static partial class {{ProgClassName}}
{
#pragma warning disable CS8618
    static ReclineProgram() {
        TryUpdateCommand((CmdID)0);
    }
#pragma warning restore CS8618

    private static string _currCmdName;
    private static string _prevCmdName = "";

    private static Action<string>[] _posArgActions = Array.Empty<Action<string>>();
    private static List<string> _params = new();

    public static bool _hasParams;
    private static string _helpString;
    private static Func<int> _invokeCmd;

    private static Dictionary<string, Action<string?>> _flags;
    private static Dictionary<string, Action<string>> _options;
    private static Dictionary<string, CmdID> _subs;

    [System.Diagnostics.StackTraceHidden]
    static int Main(string[] args) {
        var argCount = 0;

        var onlyArgs = false; // set to true if we get '--'

        for (int i = 0; i < args.Length; i++) {
            ref var rawArg = ref args[i];

            var eqIdx = rawArg.IndexOf('=');

            string arg;
            string? value = null;

            if (eqIdx < 0 || eqIdx == rawArg.Length - 1) {
                arg = rawArg;
            } else {
                arg = rawArg.Substring(0, eqIdx);
                value = rawArg.Substring(eqIdx + 1); // eqIdx + 1 ignores the '='
            }

            if (!onlyArgs) {
                if (_flags.TryGetValue(arg, out var actFlag)) {
                    if (!TryDoAction(actFlag, arg, value, rawArg))
                        return 1;
                    continue;
                }

                if (_options.TryGetValue(arg, out var actOpt)) {
                    if (value is null) {
                        if (!TryGetNext(ref i, args, out value)) {
                            PrintHelpString("Option {0} needs an argument", arg);
                            return 1;
                        }

                        rawArg += " " + value;
                    }

                    if (!TryDoAction(actOpt!, arg, value, rawArg))
                        return 1;
                    continue;
                }

                if (_subs.TryGetValue(arg, out var subCmdID)) {
                    if (!TryUpdateCommand(subCmdID)) {
                        return 1;
                    }

                    continue;
                }

                if (arg[0] == '-') {
                    if (arg.Length == 2 && arg[1] == '-') {
                        onlyArgs = true;
                        continue;
                    }

                    PrintHelpString("Couldn't understand '{0}' in this context", rawArg);
                    return 1;
                }
            }

            if (!TryAddPosArg(arg)) {
                PrintHelpString("Couldn't understand '{0}' in this context", rawArg);
                return 1;
            }
        }

        if (ArgSlotsLeft > 0) {
            PrintHelpString("Expected at least {0} arguments, but only got " + argCount, (ArgSlotsLeft + argCount).ToString());
            return 1;
        }

        return _invokeCmd();
    }

    [System.Diagnostics.StackTraceHidden]
    static bool TryDoAction(Action<string?> act, string arg1, string? arg2, string rawArg) {
        try {
            act(arg2);
            return true;
        }
        catch (Exception e) {
            PrintHelpString(
                "Expression '{0}' is not valid in this context: \x1b[1m\"" + e.Message + "\"",
                rawArg
            );
            return false;
        }
    }

    [System.Diagnostics.StackTraceHidden]
    static bool TryGetNext(ref int i, string[] args, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? nextArg) {
        if (args.Length == (i + 1)) {
            nextArg = null;
            return false;
        }

        nextArg = args[++i];
        return true;
    }

    [System.Diagnostics.StackTraceHidden]
    static string InRed(string msg)
        => "\x1b[31m" + msg + "\x1b[0m";
    [System.Diagnostics.StackTraceHidden]
    static string FormatError(string reason, object obj1)
        => InRed(string.Format(reason, "\x1b[1m" + obj1.ToString() + "\x1b[22m"));

    [System.Diagnostics.StackTraceHidden]
    public static void PrintHelpString(string reason) {
        Console.Error.WriteLine(InRed(reason));
        DisplayHelp();
    }

    [System.Diagnostics.StackTraceHidden]
    public static void PrintHelpString(string reason, string argName)
        => PrintHelpString(FormatError(reason, argName));

    [System.Diagnostics.StackTraceHidden]
    private static bool AsBool(string? val) => AsBool(val, true);

    [System.Diagnostics.StackTraceHidden]
    private static bool AsBool(string? val, bool defaultVal) {
        if (val is null)
            return defaultVal;

        if (val is "true" or "True")
            return true;

        if (val is "false" or "False")
            return false;

        throw new FormatException("Couldn't understand '" + val + "' as a boolean value");
    }

    [System.Diagnostics.StackTraceHidden]
    private static T ThrowIfNotValid<T>(T val, Func<T, string?> isValid, string argName, string? message, [CallerArgumentExpression("isValid")] string funcName = "") {
        var errorMessage = isValid(val);

        if (!String.IsNullOrEmpty(errorMessage))
            throw new Exception(errorMessage);

        return val;
    }

    [System.Diagnostics.StackTraceHidden]
    private static T ThrowIfNotValid<T>(T val, Func<T, Exception?> isValid, string argName, string? message, [CallerArgumentExpression("isValid")] string funcName = "") {
        var e = isValid(val);

        if (e is not null)
            throw e;

        return val;
    }

    [System.Diagnostics.StackTraceHidden]
    private static T ThrowIfNotValid<T>(T val, Func<T, bool> isValid, string argName, string? message, [CallerArgumentExpression("isValid")] string funcExpr = "") {
        if (!isValid(val))
            throw new Exception(message ?? "'" + funcExpr + "' returned false");

        return val;
    }

    private delegate bool BoolOut<T>(string arg, out T t);

    [System.Diagnostics.StackTraceHidden]
    private static T ThrowIfTryParseNotTrue<T>(BoolOut<T> tryParse, string arg) {
        if (!tryParse(arg, out var val))
            throw new FormatException("Couldn't parse '" + (arg ?? "") + "' as an argument of type '" + GetFriendlyNameOf(typeof(T)) + "'");

        return val;
    }

    [System.Diagnostics.StackTraceHidden]
    private static T ThrowIfParseError<T>(Func<string, T> parse, string arg) {
        try {
            return parse(arg);
        } catch (Exception e) {
            var msg = ": " + e.Message;

            throw new FormatException("Couldn't parse '" + (arg ?? "") + "' as an argument of type '" + GetFriendlyNameOf(typeof(T)) + "'" + msg);
        }
    }

    [System.Diagnostics.StackTraceHidden]
    private static string GetFriendlyNameOf(Type t) {
        if (!t.IsGenericType) {
            return t.Name switch {
                "Char"   => "char",
                "Byte"   => "byte",
                "Int16"  => "short",
                "UInt16" => "ushort",
                "Int32"  => "int",
                "UInt32" => "uint",
                "Int64"  => "long",
                "UInt64" => "ulong",
                "String" => "string",
                _        => t.Name
            };
        }

        if (t.Name == typeof(Nullable<>).Name && t.IsConstructedGenericType) {
            return GetFriendlyNameOf(t.GenericTypeArguments[0]) + "?";
        }

        if (t.IsArray) {
            return GetFriendlyNameOf(t.GetElementType()!) + "[]";
        }

        var baseName = t.Name[..^(t.GenericTypeArguments.Length < 10 ? 2 : 3)];

        return baseName + "<" + string.Join(',', t.GenericTypeArguments.Select(u => GetFriendlyNameOf(u))) + ">";
    }

    private static int posArgIdx = 0;
    internal static int ArgCount => posArgIdx + _params.Count;
    internal static int ArgSlotsLeft => _posArgActions.Length - posArgIdx;

    [System.Diagnostics.StackTraceHidden]
    internal static bool TryAddPosArg(string arg) {
        if (ArgSlotsLeft > 0) {
            _posArgActions[posArgIdx++](arg);
            return true;
        } else {
            if (!_hasParams)
                return false;

            _params.Add(arg);
            return true;
        }
    }

    [System.Diagnostics.StackTraceHidden]
    internal static void DisplayHelp(string? _) => DisplayHelp();

    [System.Diagnostics.StackTraceHidden]
    internal static int DisplayHelp() {
        Console.Error.WriteLine(_helpString);
        Environment.Exit(1);
        return 1;
    }
}
""";
}