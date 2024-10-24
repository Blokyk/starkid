using StarKid;

namespace StarKid.Tests.DefaultCmd;

[CommandGroup("test")]
public static partial class App {
    public static object GetState() => new {
        WithHidden.SomeFlag
    };

    [CommandGroup("with-hidden", DefaultCmdName = "#")]
    public static partial class WithHidden {
        [Option("some")] public static bool SomeFlag { get; set; }

        [Command("#")]
        internal static void Main(params FileInfo[] files) {
            Console.WriteLine(String.Join(", ", files.Select(f => f.ToString())));
        }
    }

    [CommandGroup("with-visible", DefaultCmdName = "bar")]
    public static partial class WithVisible {
        [Command("foo")]
        internal static void Foo(string stuff) {
            Console.WriteLine("foo: " + stuff);
        }

        [Command("bar")]
        internal static void Bar([Option("flag")] bool flag, int a) {
            Console.WriteLine($"bar({flag}): " + a);
        }
    }

}