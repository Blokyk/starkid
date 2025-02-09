using StarKid;

namespace StarKid.Tests.ProgramTests;

[CommandGroup("test")]
public static class Main {
    [Command("dummy")] public static void Dummy() {}
}