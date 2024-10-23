using StarKid;

[CommandGroup("bc", DefaultCmdName = "#")]
static class App
{
    [Option("interactive", 'i')] internal static bool forceInteractive;
    [Option("mathlib", 'l')] internal static bool hasStdLib;
    [Option("quiet", 'q')] internal static bool dontPrintHeader;
    [Option("standard", 's')] internal static bool dontUseExtensions;
    [Option("warn", 'w')] internal static bool warnExtensions;

    [Option("version", 'v')]
    internal static bool Version {
        get => default;
        set {
            PrintVersion();
            Environment.Exit(1);
        }
    }

    [Command("#")]
    internal static void Main(params FileInfo[] files) {
        if (!dontPrintHeader) {
            PrintVersion();
            Console.WriteLine("This is free software with ABSOLUTELY NO WARRANTY.");
            Console.WriteLine("For details type `warranty'.");
        }

        /* nah too lazy to implement everything */

        foreach (var file in files)
            Console.WriteLine($"Reading file '{file}'");

        Console.WriteLine("<?> = 42");
    }

    static void PrintVersion() {
        Console.WriteLine("bc 1.07.1");
        Console.WriteLine("Copyright 1991-1994, 1997, 1998, 2000, 2004, 2006, 2008, 2012-2017 Free Software Foundation, Inc.");
    }
}