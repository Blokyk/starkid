using StarKid;

[CommandGroup("door-cli")]
/// <summary>Simulates a house door</summary>
public static class Door
{
    private const string ValidKey = "5ecr3t_keii";

    [Option("log")]
    /// <summary>Log every interaction with the door</summary>
    public static bool ShouldLogActivity { get; set; }

    [Command("open")]
    /// <summary>Tries to open the door with the given key</summary>
    /// <param name="key">A string representing the key to the door</param>
    public static void Open(string key) {
        if (key != ValidKey)
            Console.Error.WriteLine("Invalid key!");
        else if (ShouldLogActivity)
            Console.WriteLine("Door is now opened");
    }

    [Command("close")]
    /// <summary>tries to close the dor with the given key</summary>
    /// <param name="key">A string representing the key to the door</param>
    public static void Close(string key, [Option("turns")] int turns = 1) {
        if (key != ValidKey)
            Console.Error.WriteLine("Invalid key!");
        else if (ShouldLogActivity)
            Console.WriteLine($"Closing door with {turns} turns");
    }

    [Command("knock")]
    /// <summary>Knocks on the door, shouting a name if provided</summary>
    /// <param name="name">An optional name to shout when knocking</param>
    /// <param ame="angry">Knock *angrily* on the door</param>
    public static void Knock(string? name = null, [Option("angry")] bool angry = false) {
        if (angry)
            Console.WriteLine($"IT'S {name}, OPEN THE GODDAMN DOOR!");
        else
            Console.WriteLine($"Hey, it's {name}!");
    }
}