# Your first app with StarKid

*The finished version of this example can be found [here](samples/door-cli/)*

In this tutorial, we will be building a simple command-line app,
`door-cli`, which simulates a house door. It has 3 subcommands:
- `open`, which takes a mandatory `key` argument
- `close`, which takes a `key`argument as well as a `-t/--turns <n>`
  option to customize the number of key turns.
- `knock`, which takes an optional `name` argument, and has an
  `--angry` switch

We'll also add a global `--log` option to... log activity.

If this was an API, we might write something like this:

```csharp
public class Door
{
    public bool ShouldLogActivity { get; set; }

    public int Open(string key) { ... }
    public void Close(string key, int turns = 1) { ... }
    public void Knock(string? name = null, bool angry = false) { ... }
}
```

And in fact, this is pretty close to what our final code will look
like! One of the biggest difference is that we need to make our class
`static`, since, as a CLI, the concept of "instantiation" doesn't
really make sense, for the same reasons you have to mark a
traditional `Main()` method static. Besides that, all we have left to
do is using StarKid's attributes to mark commands and options.

```csharp
[CommandGroup("door-cli")]
public static class Door
{
    [Option("log")]
    public static bool ShouldLogActivity { get; set; }

    [Command("open")]
    public static void Open(string key) { ... }

    [Command("close")]
    public static void Close(string key, [Option] int turns = 1) { ... }

    [Command("knock")]
    public static void Knock(string? name = null, [Option] bool angry = false) { ... }
}
```

You might notice that the attribute we put a `Door` is slightly
different than the ones on methods. This is because the former
doesn't have any inherent behavior: it's not "invokable" like methods
are; instead it's meant to group together related commands a.k.a.
methods.

**This is one of StarKid's core concepts, so it's worth repeating:
*static classes are groups of related methods/commands; only methods
can be executed.***

And... that's about it! After adding some actual code to the `Open`,
`Close`, and `Knock`, you should be able to build and run your app!

> [!IMPORTANT]
> To run your app with `dotnet run`, you'll need to add
> `--` before you type your arguments/commands/options, to make sure
> that the dotnet CLI doesn't misinterpret any of them. For example:
> ```shell
> $ dotnet run -- open 5ecr3t_keii
> ```
> If you're using an executable directly, there is obviously no need
> to do this (and in fact, StarKid follows POSIX's recommendation
> to use `--` as a delimiter between options/commands and normal
> arguments, which is why you need to do this for `dotnet run`)
> ```shell
> $ ./door-cli open 5ecr3t_keii
> ```

Finally, StarKid automatically generates a `--help`/`-h` flag for
*every* group and command, but right now it's not super... helpful:
```shell
$ ./door-cli knock --help
Usage:
  door-cli [--log] knock [name] [--angry]

Arguments:
  name

Options:
  --angry
```

To remedy that, we should add some documentation to our code:
```csharp
[CommandGroup("door-cli")]
/// <summary>Simulates a house door</summary>
public static class Door
{
    [Option("log")]
	/// <summary>Log every interaction with the door</summary>
    public static bool ShouldLogActivity { get; set; }

    [Command("open")]
	/// <summary>Tries to open the door with the given key</summary>
	/// <param name="key">A string representing the key to the door</param>
    public static void Open(string key) { ... }

    [Command("close")]
	/// <summary>tries to close the dor with the given key</summary>
	/// <param name="key">A string representing the key to the door</param>
    public static void Close(string key, [Option("turns")] int turns = 1) { ... }

    [Command("knock")]
	/// <summary>Knocks on the door, shouting a name if provided</summary>
	/// <param name="name">An optional name to shout when knocking</param>
	/// <param ame="angry">Knock *angrily* on the door</param>
    public static void Knock(string? name = null, [Option("angry")] bool angry = false) { ... }
}
```

And voilà! Now, our help option actually lives up to its name:
```shell
$ ./door-cli knock -h
Usage:
  door-cli [--log] knock [name] [--angry]

Description:
  Knocks on the door, shouting a name if provided

Arguments:
  name       An optional name to shout when knocking

Options:
  --angry    Knock *angrily* on the door
```
```shell
$ ./door-cli -h
Usage:
  door-cli [--log] open <key>
  door-cli [--log] close <key> [-t=<n> | --turns=<n>]
  door-cli [--log] knock [name] [--angry]

Description:
  Simulates a house door

Options:
  --log    Log every interaction with the door
```
