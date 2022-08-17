# CLIGen

A C# source generator to create command-line apps given a simple, code-based description

---

**Disclaimer**: Obviously, don't actually use this for anything serious, it has zero tests and could probably break if you look at it the wrong way. This is, at best, in pre-alpha; more realistically it's barely a prototype.

### ...why tho?

After looking at the implementation of [System.CommandLine.DragonFruit](https://github.com/dotnet/command-line-api/blob/main/docs/DragonFruit-overview.md), I was slightly disappointed to find that it was basically just a redirect to reflection-based parsing. Don't get me wrong, I heavily respect behind that project, both for the simplicity of DragonFruit and the flexibility of System.CommandLine.

I wanted to try my hand at writing a source generator, as well as having a slightly less complex/obscure mechanism for the CLI for [lotus](https://github.com/Blokyk/Lotus). So I started writing, not expecting to actually do anything useful, let alone have an actual generator. And yet here we are.

I don't actually plan on maintaining this any further than I need to for lotus, but feel free to open issues/PR and I'll take a look at them !

## Getting started

// todo

## TL;DR docs

- Use `[CLI]` on a static class to mark it as the descriptor, set the `EntryPoint` property to the name of the method you want to invoke by default; if not set, the app will just error out when no command is supplied

- Use `[Option(longName, alias)]` on a static field or property (with a set accessor) to declare a global new command-line option. If it's a bool, it will be parsed as a switch; otherwise the argument will first be auto-parsed from a string into the desired type

- You can use `[Option]` on method parameters to declare command-specific options.

- Use `[Command(cmdName)]` on a method to link it to a command with the given name. Any parameter not marked `[Option]` will be treated as a positional argument

- Use `[SubCommand(cmdName, parentName)]` on a method to declare it as a sub command of `parentName`. Note that `parentName` is the name of the **method** that you want to sub-command, **not the command**. 

- You can use `[Option]` on a method with a single `string` parameter. This can be used to validate arguments

- Use `[Description(text)]` to add, well, a description to an option, a [sub]command, or even your app

## Known bugs & missing features

- ***The generated `Program.Parse<T>(string)` function doesn't actually do anything***

- Using method to validate options is only possible for top-level options (design flaw)

- The limitations around certain elements could be lifted in some cases (e.g. static class for CLI)

- If the generator can't find a valid description class, it will only generate *a part* of the code, leading to errors

- If the generator encounters any error, it just fails silently instead of reporting diagnostics (however, it generates `CLIGen_err.g.txt` with an error message which can help in narrowing down the problem)

- We don't emit any error when someone defines an arg or option of an unsupported type

- Options declared in the "Entry Point" method parameter list are currently not supported, even they could be implemented just like for normal commands. The bug is that I'm lazy