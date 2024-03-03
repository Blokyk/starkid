# StarKid

A C# source generator to create command-line apps from a simple, code-based description (now with 80% less boilerplate!).

[[ Quick Start ]](#tldr-docs-) | [[ Tutorial ]](docs/Your-first-app-with-StarKid.md) | [[ Overview ]](docs/StarKid-overview.md) | [[ Docs ]](docs/)

## What can it actually do?

StarKid allows you to write code that matches perfectly your command line interface's structure, by using nesting to represent your CLI's hierarchy of verbs and subcommands.

It also includes a bunch of mechanisms to make parsing and validating options/args easier, so you never have to string-type anything, you can simply use the appropriate type for everything instead of writing walls of `if-then-throw`s at the start of every method.

## Getting started 🚀

StarKid is just like most other source generators and requires no additional dependency. You can install it just like a normal package, with:

```shell
dotnet add package Blokyk.StarKid
```

That's it! For a quick tutorial, check out [Your first app with StarKid](docs/Your-first-app-with-StarKid.md), or have a look through the TL;DR docs below. If you want to deep-dive directly in, you can check out the [overview](docs/StarKid-overview.md) and the rest of the [docs folder](docs/)

## TL;DR docs 📖

```csharp
using Blokyk.StarKid;

[CommandGroup("my-app")]
public static class MyApp {
    [Option("verbose", 'v')]
    public bool ShouldBeVerbose { get; set; }

    [Command("greet")]
    // ./my-app greet <name> [--greeting greetingPhrase]
    public void Greet(string name, [Option("greeting", 'g')] string greetingPhrase = "Hello, ") {
        if (ShouldBeVerbose)
            Console.Error.WriteLine($"Running command 'greet' with name '{name}' and phrase '{greetingPhrase}'");

        Console.WriteLine(greetingPhrase + name + "!")
    }
}
```

- Commands are represented by methods marked with `[Command]`, *groups* of commands by (static) classes marked with `[CommandGroup]`.

- By nesting classes inside others, you can have a command group "inside" another group, which acts as a subgroup to its parent.

- The outmost `[CommandGroup]` class is the root group, which basically defines the CLI; it is only special in that there can't be multiple root groups (since you can't have multiple CLIs inside a single assembly).

- Command options are declared by adding `[Option]` to a parameter. Any other parameter will be treated as a mandatory argument.

- An `[Option]` whose type is `bool` will be treated as a flag.

- Using `[Option]` on a field or property (with a set accessor) declares that option for the whole group.

- Just like you can't have methods outside of classes, you can't have `[Command]`s outside of `[CommandGroup]`s.

For more information, check out [Your first app with StarKid](docs/Your-first-app-with-StarKid.md) and [StarKid overview](docs/StarKid-overview.md).

## ...why tho?

While rewriting [lotus](https://github.com/lotuslang/lotus)'s command line interface to use `System.CommandLine`, I found myself looking at [System.CommandLine.DragonFruit](https://github.com/dotnet/command-line-api/blob/main/docs/DragonFruit-overview.md), a generator that makes writing basic CLIs absolutely effortless.

However, after looking at its implementation, I was slightly disappointed to find that it was basically just a redirect to reflection-based parsing. Don't get me wrong, I heavily respect the people behind that project, both for the simplicity of DragonFruit and the flexibility of `System.CommandLine`[^1], but it did feel like there was a gap to be filled there.

[^1]: It also tries to follow POSIX standards and conventions which is absolutely impressive in its right.

I wanted to try my hand at writing a source generator, as well as having a slightly less complex/obscure mechanism for lotus's CLI. So I started writing, not expecting to actually do anything useful, let alone have an actual generator. And yet here we are.

One thing to note is that this generator does not simply replace your declaration with calls to the `System.CommandLine` library, it *is* a completely separate thing. Unfortunately, right now a few tools in the dotnet environment rely on `System.CLI`'s niceties, which means for now some features you might be used to will be unavailable; for example, you won't get compatibility with [dotnet-suggest](https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md), or any kind of "debug-mode" like in `System.CLI`. As I've stated above, my initial goal was simply to get a reflection- and overhead-free alternative to that library, and given how tightly integrated dotnet-suggest is with it, I can't make any promises as to whether this will be supported in any future versions. However, it is a tool I use a lot given my usage of the terminal, so I'll definitely try to investigate it in my free time.

For now, I don't actually plan on maintaining this any further than I need to for my personal projects, but I'm completely open to hearing about any issues or requests you might have, so feel free to create an issue/PR and I'll try to take a look at it!

## Disclaimer ⚠️

You probably shouldn't actually use this for anything serious. While I've put a lot of effort into it, it is still pretty brittle in some cases; and although StarKid's design has gone through multiple iterations, it is fairly opinionated, goes against some of C#'s coding conventions, and can quickly lead to unmaintainable code if not planned carefully. I personally encourage partial classes split into files and folders reflecting your CLI's structure.

In addition, it is not fit for every case or app in the world: it heavily discourages "one root command, a thousand options"-kind of CLI. If you're trying to write a GCC-style CLI with a thousand different flags, this is probably not the library for you.