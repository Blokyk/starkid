## Your first app with Recline

*The finished version of this example can be found [here](samples/Doer/)*

> todo: little introduction explaining the app we're gonna be building
> todo: note about `dotnet run --` vs `./my-app`

> todo: rewrite this paragraph to fit the tone of this document
As stated above, (runnable) commands correspond to methods marked
with a special attribute, `[Command]`. However, just like normal
methods in C#, they can't just exist outside of a class! Since
Recline is mostly built for apps with multiple (sub-)commands in
mind, [we have chosen to follow that pattern](./Recline-overview.md#one-command-apps-are-impossible-to-create).
Thus, you'll first need to create a class with the `[CommandGroup]`
attribute (which we'll revisit later), and put your methods in that.

```csharp
[CommandGroup("my-app")]
public static class MyApp {
    [Command("announce")]
    public static void Announce() {
        Console.WriteLine("Here comes... FABIEN!");
    }
}
```

Building and running this code gives the following little CLI:
```shell
> dotnet build && cd bin/Debug/net* # move to the folder with the executable
> ./my-app -h # or whatever you named your executable
Usage:
  my-app announce

Options:
  -h, --help  Display this help message

Subcommands:
  announce
> ./my-app announcer
Here comes... FABIEN!
```

As a command, it has to return either `void` or `int` (just like a
classic `Main()` method), and [must be internal/public](#classesmethodsfields-must-be-internal)
as well as [static](#classesmethodsfields-must-be-static).

What if we wanted to shout-out someone other than Fabien though?
Well, just like when you want to generalize a normal method, you just
add a parameter:

```csharp
[Command("announce")]
public static void Announce(string name) {
    Console.WriteLine($"Here comes... {name.ToUpper()}!");
}
```

And here's our little program working!
```shell
> ./my-app announce Janet
Here comes... JANET!
```

...as well as not working, that's important too!
```shell
> ./my-app announce
Expected at least 1 arguments, but only got 0
Usage:
  my-app announce <name>

Arguments:
  name

Options:
  -h, --help  Display this help message
```

(For more information on errors and help text, see the section on
[help text generation](./Recline-overview.md#help-text-generation).)

Okay that's pretty cool, but all that screaming might become a bit
tiring... How about we add