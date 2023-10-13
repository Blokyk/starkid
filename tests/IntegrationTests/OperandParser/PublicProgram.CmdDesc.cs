#nullable disable
using static StarKid.Tests.OperandParser.Main; // i'm lazy i don't care

namespace StarKid.Generated;

internal static partial class StarKidProgram
{
    static partial void ResetCmdDescs() {
        MainCmdDesc.Reset();
        Main_SumCmdDesc.Reset();
    }

    public static partial class MainCmdDesc {
        public static void Reset() {}
    }

    public static partial class Main_SumCmdDesc {
        public static void Reset() {
            @a = 0;
            @b = 0;
        }
    }
}