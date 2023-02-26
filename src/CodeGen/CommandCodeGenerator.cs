#pragma warning disable RCS1197 // Optimize StringBuilder call

using Recline.Generator.Model;

namespace Recline.Generator;

internal static partial class CodeGenerator
{
    internal static class CommandCodeGenerator
    {
        public static void AddSourceCode(StringBuilder sb, Command cmd) {
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

        static void AddCommandFunc(StringBuilder sb, MinimalMethodInfo method) {
            sb.Append(@"
        internal static ");

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

            sb.Append(" _func = ").Append(method.ToString()).Append(';')
            .AppendLine();
        }
    }
}