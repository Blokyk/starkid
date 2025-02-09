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

            AutoSwitch = default;
            hasAutoSwitchActionBeenTriggered = false;

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

            RepeatableStringOption = Array.Empty<string>();
            RepeatableStringOptionBuilder = new();

            RepeatableAutoOption = Array.Empty<int>();
            RepeatableAutoOptionBuilder = new();

            RepeatManualItemOption = Array.Empty<string>();
            RepeatManualItemOptionBuilder = new();

            ManualArrayOption = default;
            hasManualArrayOptionActionBeenTriggered = false;

            GenericParserOption = default;
            hasGenericParserOptionActionBeenTriggered = false;

            RepeatManualItemValidatorOption = Array.Empty<string>();
            RepeatManualItemValidatorOptionBuilder = new();

            RepeatItemArrayValidatorOption = Array.Empty<int>();
            RepeatItemArrayValidatorOptionBuilder = new();

            GenericValidatorOption = default;
            hasGenericValidatorOptionActionBeenTriggered = false;

            GenericValidatorWithGenericParserOption = default;
            hasGenericValidatorWithGenericParserOptionActionBeenTriggered = false;

            ValidatorWithMessageOption = default;

            // ThrowingOption = "this shouldn't be set, remember?";
            hasThrowingOptionActionBeenTriggered = false;
        }
    }

    public static partial class Main_DummyCmdDesc {
        public static void Reset() {}
    }

    public static partial class Main_Dummy2CmdDesc {
        public static void Reset() {
            missingArg = default;
            hasmissingArgActionBeenTriggered = false;

            flagShouldntHaveNonBoolArg = default;
            hasflagShouldntHaveNonBoolArgActionBeenTriggered = false;

            defaultOpt = 5;
            hasdefaultOptActionBeenTriggered = false;

            Dummy2State = new();
        }
    }
}