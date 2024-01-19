namespace StarKid.Tests.Options;

[CommandGroup("test")]
public static partial class Main {
    public static object GetState()
        => new {
            SimpleSwitch,
            SwitchProp,
            TrueSwitch,
            ParsedSwitch,
            GlobalSwitch,

            StringOption,
            IntOption,
            AutoLibOption,
            EnumOption,
            AutoUserOption,
            RepeatableStringOption,
            RepeatableAutoOption,
            AutoParsedNullableStructOption,

            ParsedStringOption,
            ManualLibOption,
            ManualEnumOption,
            ManualFooOption,
            ManualParsedNullableStructOption,
            DirectParsedNullableStructOption,
            RepeatManualItemOption,
            ManualArrayOption,

            RepeatManualItemValidatorOption,
            RepeatItemArrayValidatorOption,

            ValidatorWithMessageOption,

            ThrowingOption
        };

    [Command("dummy")] public static void Dummy() { }

    [Option("throwing-setter")] public static string ThrowingOption {
        get => null!;
        set => throw new InvalidOperationException("Faulty setter!");
    }

    [Option("global-switch", IsGlobal = true)] public static bool GlobalSwitch { get; set; }

    // todo: global repeatable option

    internal static object Dummy2State = new();
    [Command("dummy2")] public static void Dummy2(
        [Option("cmd-opt-with-default")] int defaultOpt = 5
    ) {
        Dummy2State = new { defaultOpt };
    }
}