#pragma warning disable RCS1197 // Optimize StringBuilder call

using Recline.Generator.Model;

namespace Recline.Generator;

internal sealed partial class CodeGenerator
{
    void AddSourceCode(StringBuilder sb, Command cmd) {
        sb.Append(@"
#pragma warning disable CS8618
#pragma warning disable CS8625
private static class ").Append(cmd.ID).Append("CmdDesc {")
        .AppendLine();

        foreach (var opt in cmd.Options) {
            AddOptionFunction(sb, opt, cmd);
        }

        AddOptionLookup(sb, cmd, isFlags: false);

        sb.AppendLine();

        foreach (var flag in cmd.Flags) {
            AddOptionFunction(sb, flag, cmd);
        }

        AddOptionLookup(sb, cmd, isFlags: true);

        sb.AppendLine();

        sb.Append(@"
        internal static bool TryUpdateCommand(string _) => false;");

        AddActivateFunc(sb);

        sb.AppendLine();

        AddHasParamsField(sb, cmd);

        sb.AppendLine();

        AddPosArgActions(sb, cmd);

        sb.AppendLine();

        AddCommandFunc(sb, cmd.BackingMethod);

        AddInvokeCmdField(sb, cmd);

        sb.AppendLine();

        AddHelpTextLine(sb, cmd);

        sb.Append(@"
        internal const string _name = """).Append(cmd.Name).Append("\";")
        .AppendLine();

        sb.Append("\t}").AppendLine();
    }

    void AddCommandFunc(StringBuilder sb, MinimalMethodInfo method) {
        var typeName
            = method.ReturnsVoid
            ? "void"
            : method.ReturnType.FullName;

        sb.Append(@"
        private delegate ").Append(typeName).Append(" __funcT(");

        sb.Append(String.Join(", ", method.Parameters.Select((p, i) => p.Type.FullName + " param" + i)));

        sb.Append(");\n");

        sb.Append(@"
        private static __funcT _func = ").Append(method.ToString()).Append(';')
        .AppendLine();
    }

    void AddInvokeCmdField(StringBuilder sb, Command cmd) {
        sb.Append(@"
        internal static readonly Func<int> _invokeCmd = ");

        var isVoid = cmd.BackingMethod.ReturnsVoid;
        var methodParams = cmd.BackingMethod.Parameters;

        // if _func is already Func<int>
        if (!isVoid && methodParams.Length == 0) {
            sb.Append("_func");
        } else {
            // lambda attributes are only supported since C#10
            if (_config.LanguageVersion >= LanguageVersion.CSharp10)
                sb.Append("[System.Diagnostics.StackTraceHidden]");

            sb.Append("() => "); // can't be static because of _params

            if (isVoid)
                sb.Append("{ ");

            sb.Append("_func(");

            var defArgName = new string[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++) {
                defArgName[i]
                    = methodParams[i].IsParams
                        ? "_params.ToArray()"
                        : "@" + methodParams[i].Name + "!";
            }

            sb.Append(String.Join(", ", defArgName));

            sb.Append(')');

            if (isVoid)
                sb.Append("; return 0; }");
        }

        sb
        .Append(';')
        .AppendLine();
    }
}