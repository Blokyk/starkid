# Recline

A C# source generator to create command-line apps given a simple, code-based description

---

**Disclaimer**: Obviously, don't actually use this for anything serious, it has zero tests and could probably break if you look at it the wrong way. This is, at best, in pre-alpha; more realistically it's barely a prototype.

I don't actually plan on maintaining this any further than I need to for lotus, but feel free to open issues/PR and I'll be sure to take a look at them !

## Getting started

// todo

## TL;DR docs

- Use `[CLI]` on a static class to mark it as the descriptor, set the `EntryPoint` property to the name of the method you want to invoke by default; if not set, the app will just error out when no command is supplied

- Use `[Option(longName, alias)]` on a static field or property (with a set accessor) to declare a global new command-line option. If it's a bool, it will be treated as a flag; otherwise, the argument will be converted to the target type (when possible)

- You can use `[Option]` on method parameters to declare command-specific options.

- Use `[Command(cmdName)]` on a method to link it to a command with the given name. Any parameter not marked `[Option]` will be treated as a positional argument

- Use `[SubCommand(cmdName, parentName)]` on a method to declare it as a sub command of `parentName`. Note that `parentName` is the name of the **method** that you want to sub-command, **not the command**.

- You can use `[Option]` on a method with a single `string` parameter. This can be used to validate arguments

- Use `[Description(text)]` to add, well, a description to an option, a [sub]command, or even your app

## Known bugs & missing features

- Associating method to switches is only possible for top-level options (design flaw)

- The limitations around certain elements could be lifted in some cases (e.g. static class for CLI)

- Options declared in the "Entry Point" method parameter list are currently not supported, even tho they could be implemented just like for normal commands. The bug is that I'm lazy

- You cannot declare a `--help` or `-h` option

- Method-backed options are called immediately, even if there's still other stuff (like options) to parse. This could be changed by adding a "processing queue" which could be dequeued on `CmdDesc.Invoke()`. However, the semantics of this, however, are a bit weird, since which options might be set/processed will depend on a) whether they were method-backed or not, b) if they were, did they come before or after, and c) if they came after, what should we do ? In my opinion, this could be resolved by transitioning method-backed options to instead use the [ParseWith] attribute, and having void-method-backed flags be put in a processing queue, or potentially preventing a user from specifying two different void-backed flags (although that might considerably reduce their usefulness)

### ...why tho?

After looking at the implementation of [System.CommandLine.DragonFruit](https://github.com/dotnet/command-line-api/blob/main/docs/DragonFruit-overview.md), I was slightly disappointed to find that it was basically just a redirect to reflection-based parsing. Don't get me wrong, I heavily respect the people behind that project, both for the simplicity of DragonFruit and the flexibility of System.CommandLine, but it did feel like there was a gap to be filled there.

I wanted to try my hand at writing a source generator, as well as having a slightly less complex/obscure mechanism for the CLI for [lotus](https://github.com/Blokyk/Lotus). So I started writing, not expecting to actually do anything useful, let alone have an actual generator. And yet here we are.

One thing to note is that this generator does not "simply" replace your declaration with calls to the `System.CommandLine` library, which means you won't get some of that lib's built-in features, such as compatibility with [dotnet-suggest](https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md) and debug-mode. However, as I've declared above, my initial goal was simply to get a reflection- and overhead-free alternative to System.CommandLine, which was a bit overpowered for me.
