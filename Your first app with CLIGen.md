## Your first app with CLIGen

*The finished version of this example can be found [here](samples/DragonFruit/)*

Let's start by taking the example CLI used by the DragonFruit samples :

> **Note**: Again, this is code for DragonFruit, **NOT** CLIGen

```csharp
// Code for System.CommandLine.DragonFruit
static void Main(int intOption = 42, bool boolOption = false, FileInfo fileOption = null)
{
    Console.WriteLine($"The value of intOption is: {intOption}");
    Console.WriteLine($"The value of boolOption is: {boolOption}");
    Console.WriteLine($"The value of fileOption is: {fileOption?.FullName ?? "null"}");
}
```

> If you've never heard of/used DragonFruit before, this creates an app that parses three different *options* (i.e. `--intOption`), which each have a default value, and are automatically parsed/converted from the raw argument string into the right type.

So, how would you write this app with CLIGen ? First, let's start by declaring a class to contain our CLI :

```csharp
[CLI(cmdName: "myapp")] // be sure to write using CLIGen !
static class MyCLI { /* */ }
```

Next, we declare each option. Here, this is done by adding fields and marking them with `[Option]`. For `intOption` that look like that :

```csharp
    [Option("intOption")]
    public static int intOption = 42;
```

> **Note**: Option members need to be marked public or internal for the generator to be able to see them

If we build and run our app now, we'll get a `No command provided` error with a help text. So... **what went wrong ?**

The problem is, we never specified any functionality. CLIGen doesn't assume your app has a "default" command, so it just errors out.

So, let's write a function to print the value of our fields :

```csharp
    public static void Dump() {
        Console.WriteLine($"The value of intOption is: {intOption}");
        Console.WriteLine($"The value of boolOption is: {boolOption}");
        Console.WriteLine($"The value of fileOption is: {fileOption?.FullName ?? "null"}");
    }
```

The last thing we have to do, is change the `[CLI]` attribute at the point of our class to indicate the "entry point", i.e. the method that will be executed by default when no command is specified :

```csharp
[CLI(cmdName: "myapp", EntryPoint = nameof(Dump)]
static class MyCLI { /* */ }
```

Build and run and... success ! 

You can now use `--boolOption` and the likes to change the value of each option. Congrats ! You made your first CLI app, with a help text, error handling, and even basic type conversion, and all that in only ~15 lines !

> **Note**: If you use `dotnet run` to launch your app, you'll need to insert `--` before specifying your options/arguments, so that the dotnet cli doesn't capture them first. For example :
> 
> ```shell
> $ dotnet run -- -h
> ```
> 
> Will print *your app's* help text, while
> 
> ```shell
> $ dotnet run -h
> ```
> 
> Will invoke print `dotnet run`'s help message

### What now ?

This is about as far as you can go with DragonFruit, but CLIGen can do a lot more !

For example, you can change the name of the *option* without changing the name of the field/property, and you can add an alias. Here's how it could look :

```csharp
    [Option("int", 'i')] public static int intOption = 42;
```

Now, you can use `--int` and `-i` instead of `--intOption`. You could even change the name of the argument in the help text by using `ArgName =`.

For more info, you can start by reading the [docs](docs/), or you could have a look around in the [samples](samples/) folder for more examples
