namespace StarKid.Tests {
    static partial class Utils { public static object GetHostState() => new(); }
}

namespace StarKid.Generated {
    static partial class StarKidProgram {
        public static string MainHelpText => MainCmdDesc._helpText;
        public static string DummyHelpText => Main_DummyCmdDesc._helpText;
    }
}