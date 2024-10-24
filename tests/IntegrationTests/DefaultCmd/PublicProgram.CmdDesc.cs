#nullable disable
using StarKid.Tests.DefaultCmd;

namespace StarKid.Generated;

internal static partial class StarKidProgram
{
    static partial void ResetCmdDescs() {
        AppCmdDesc.Reset();
        App_WithHiddenCmdDesc.Reset();
        App_WithHidden_MainCmdDesc.Reset();
        App_WithVisibleCmdDesc.Reset();
        App_WithVisible_FooCmdDesc.Reset();
        App_WithVisible_BarCmdDesc.Reset();
    }

    public static partial class AppCmdDesc {
        public static void Reset() {}
    }

    public static partial class App_WithHiddenCmdDesc {
        public static void Reset() {
            App.WithHidden.SomeFlag = default;
            hasSomeFlagActionBeenTriggered = false;
        }
    }

    public static partial class App_WithHidden_MainCmdDesc {
        public static void Reset() {
            _params.Clear();
        }
    }

    public static partial class App_WithVisibleCmdDesc {
        public static void Reset() {}
    }

    public static partial class App_WithVisible_FooCmdDesc {
        public static void Reset() {}
    }

    public static partial class App_WithVisible_BarCmdDesc {
        public static void Reset() {
            flag = default;
            hasflagActionBeenTriggered = false;
        }
    }
}