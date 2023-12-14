#nullable disable
using static StarKid.Tests.Options.Main; // i'm lazy i don't care

namespace StarKid.Generated;

internal static partial class StarKidProgram
{
    static partial void ResetCmdDescs() {
        MainCmdDesc.Reset();
        Main_DummyCmdDesc.Reset();
        Main_Dummy2CmdDesc.Reset();
    }

    public static partial class MainCmdDesc {
        public static void Reset() {
            // switch
            SimpleSwitch = false;
            hasSimpleSwitchActionBeenTriggered = false;

            SwitchProp = false;
            hasSwitchPropActionBeenTriggered = false;

            TrueSwitch = true;
            hasTrueSwitchActionBeenTriggered = false;

            ParsedSwitch = false;
            hasParsedSwitchActionBeenTriggered = false;

            GlobalSwitch = false;
            hasGlobalSwitchActionBeenTriggered = false;

            // options
            StringOption = "blank0";
            hasStringOptionActionBeenTriggered = false;

            IntOption = default;
            hasIntOptionActionBeenTriggered = false;

            AutoLibOption = default;
            hasAutoLibOptionActionBeenTriggered = false;

            EnumOption = FileMode.Open;
            hasEnumOptionActionBeenTriggered = false;

            AutoUserOption = default;
            hasAutoUserOptionActionBeenTriggered = false;

            ParsedStringOption = "blank1";
            hasParsedStringOptionActionBeenTriggered = false;

            ManualLibOption = default;
            hasManualLibOptionActionBeenTriggered = false;

            ManualEnumOption = default;
            hasManualEnumOptionActionBeenTriggered = false;

            ManualFooOption = default;
            hasManualFooOptionActionBeenTriggered = false;

            AutoParsedNullableStructOption = default;
            hasAutoParsedNullableStructOptionActionBeenTriggered = false;

            ManualParsedNullableStructOption = default;
            hasManualParsedNullableStructOptionActionBeenTriggered = false;

            DirectParsedNullableStructOption = default;
            hasDirectParsedNullableStructOptionActionBeenTriggered = false;

            // ThrowingOption = "this shouldn't be set, remember?";
            hasThrowingOptionActionBeenTriggered = false;
        }
    }

    public static partial class Main_DummyCmdDesc {
        public static void Reset() {}
    }

    public static partial class Main_Dummy2CmdDesc {
        public static void Reset() {
            defaultOpt = 5;
            hasdefaultOptActionBeenTriggered = false;

            Dummy2State = new();
        }
    }
}