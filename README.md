# Recline

A C# source generator to create command-line apps from a simple, code-based description (now with 80% less boilerplate!).

## What can it actually do?

Recline allows you to write code that matches perfectly your command line interface's structure, by using nesting to represent your CLI's hierarchy of verbs and subcommands.

It also includes a bunch of mechanisms to make parsing and validating options/args easier, so you never have string-type anything, you can simply use the appropriate type for everything instead of writing walls of `if-then-throw`s at the start of every method.

## Getting started 🚀

Recline is just like most other source generators and requires no additional dependency. You can install it just like a normal package, with:

```shell
dotnet add package Blokyk.Recline
```

That's it! To start writing your CLI, check out [Your first app with Recline](docs/../Your%20first%20app%20with%20Recline.md), or have a quick look through the TL;DR docs below.

## TL;DR docs 📖

- Commands are represented by methods marked with `[Command]`, *groups* of commands by (static) classes marked with `[CommandGroup]`.

- Just like you can nest classes inside others, you can have a command group "inside" another group, which acts as a subgroup to its parent.

- The outmost `[CommandGroup]` class is the root group, which basically defines the CLI; it is only special in that there can't be multiple root groups (since you can't have multiple CLIs inside a single assembly).

- Just like you can't have methods outside of classes, you can't have `[Command]`s outside of `[CommandGroup]`s.

- Command options are declared by adding `[Option]` to a parameter. Any other parameter will be treated as an argument.

- Using `[Option]` on a field or property (with a set accessor) declares that option for the whole group.

- An `[Option]` whose type is `bool` will be treated as a flag.

For more information, check out [Your first app with Recline](docs/Your-first-app-with-Recline.md) and [Recline overview](docs/Recline-overview.md).

## Known bugs & missing features 🐛

- You cannot declare a custom `--help` or `-h` option.

- Help texts are currently entirely generated at compile-time, which means it won't adapt to the size of the terminal; instead it uses a default column size of 80 characters.

- Errors and warnings from Recline do not show up as red squiggles, and instead are only available from the `Build output` window in VS, or in the output of `dotnet build`.

## ...why tho?

While rewriting [lotus](https://github.com/lotuslang/lotus)'s command line interface to use `System.CommandLine`, I found myself looking at [System.CommandLine.DragonFruit](https://github.com/dotnet/command-line-api/blob/main/docs/DragonFruit-overview.md), a generator that makes writing basic CLIs absolutely effortless.

However, after looking at its implementation, I was slightly disappointed to find that it was basically just a redirect to reflection-based parsing. Don't get me wrong, I heavily respect the people behind that project, both for the simplicity of DragonFruit and the flexibility of `System.CommandLine`[^1], but it did feel like there was a gap to be filled there.

[^1] It also tries its best to follow POSIX standards and conventions which is absolutely impressive in its right.

I wanted to try my hand at writing a source generator, as well as having a slightly less complex/obscure mechanism for lotus's CLI. So I started writing, not expecting to actually do anything useful, let alone have an actual generator. And yet here we are.

One thing to note is that this generator does not simply replace your declaration with calls to the `System.CommandLine` library, which does have a few caveats. For example, you won't get compatibility with [dotnet-suggest](https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md), or any kind of "debug-mode" like in that library. However, as I've stated above, my initial goal was simply to get a reflection- and overhead-free alternative to `System.CommandLine`, which was a bit overpowered for me.

## Disclaimer ⚠️

You probably shouldn't actually use this for anything serious. It has basically no tests and can be pretty brittle in some cases. It also does not follow e.g. POSIX conventions and is pretty opinionated in some cases. It is also not fit for every case or app in the world; in fact, it heavily discourages "one root command, a thousand options" kind of CLI. In short: you ain't gonna be writing a `gcc` wrapper with this Recline.

For now, I don't actually plan on maintaining this any further than I need to for my personal projects, but I'm completely open to hearing about any issues or requests you might have, so feel free to create an issue/PR and I'll try to take a look at it!