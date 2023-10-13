using StarKid;

namespace StarKid.Tests.OperandParser;

[CommandGroup("test")]
public static class Main {
    public static object GetState() => new {};

    [Command("sum")]
    public static void Sum(int a, int b) => Console.WriteLine(a + b);
}