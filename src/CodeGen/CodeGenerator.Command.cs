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
            sb.AppendOptionFunction(opt, cmd);
        }

        AddOptionDictionary(sb, cmd, isFlags: false);

        sb.AppendLine();

        foreach (var flag in cmd.Flags) {
            sb.AppendOptionFunction(flag, cmd);
        }

        AddOptionDictionary(sb, cmd, isFlags: true);

        sb.AppendLine();

        sb.Append(@"
        // needed to simplify recline's codegen
        internal static readonly Dictionary<string, CmdID> _subs = new();");

        sb.AppendLine();
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
}