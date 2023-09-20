#nullable disable
using static StarKid.Tests.Options.OptionTest; // i'm lazy i don't care

namespace StarKid.Generated;

internal static partial class StarKidProgram
{
    static partial void ResetCmdDescs() {
        OptionTestCmdDesc.Reset();
        OptionTest_DummyCmdDesc.Reset();
        OptionTest_Dummy2CmdDesc.Reset();
    }

    public static partial class OptionTestCmdDesc {
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

            FooOption = default;
            hasFooOptionActionBeenTriggered = false;

            ParsedStringOption = "blank1";
            hasParsedStringOptionActionBeenTriggered = false;

            ManualLibOption = default;
            hasManualLibOptionActionBeenTriggered = false;

            ManualEnumOption = default;
            hasManualEnumOptionActionBeenTriggered = false;

            ManualFooOption = default;
            hasManualFooOptionActionBeenTriggered = false;

            NullableStructOption = default;
            hasNullableStructOptionActionBeenTriggered = false;

            // ThrowingOption = "this shouldn't be set, remember?";
            hasThrowingOptionActionBeenTriggered = false;
        }
    }

    public static partial class OptionTest_DummyCmdDesc {
        public static void Reset() {}
    }

    public static partial class OptionTest_Dummy2CmdDesc {
        public static void Reset() {
            defaultOpt = 5;
            hasdefaultOptActionBeenTriggered = false;

            Dummy2State = new();
        }
    }
}