# CLIGen

A C# source generator to create command-line apps given a simple, code-based description

---

### ...why tho?

After looking at the implementation of [System.CommandLine.DragonFruit](https://github.com/dotnet/command-line-api/blob/main/docs/DragonFruit-overview.md), I was slightly disappointed to find that it was basically just a redirect to reflection-based parsing. Don't get me wrong, I heavily respect behind that project, both for the simplicity of DragonFruit and the flexibility of System.CommandLine.

I wanted to try my hand at writing a source generator, as well as having a slightly less complex/obscure mechanism for the CLI for [lotus](https://github.com/Blokyk/Parsex). So I started writing, not expecting to actually do anything useful, let alone have an actual generator. And yet here we are.

I don't actually plan on maintaining this any further than I need to for lotus, but feel free to open issues/PR and I'll take a look at them !

## Getting started

// todo

## TL;DR docs

- Use `[CLI]` on a static class to mark it as the descriptor

- Use `[Option(longName, alias)]` on a static field or property (with a set accessor) to declare a new command-line option. If it's a bool, it will be parsed as a switch; otherwise the argument will first be auto-parsed from a string into the desired type

- 

## Known bugs & missing features

- ***The generated `Program.Parse<T>(string)` function doesn't actually do anything***

- The limitations around certain elements could be lifted in some cases (e.g. static class for CLI)

- If the generator can't find a valid description class, it will only generate *a part* of the code, leading to errors

- If the generator encounters any error, it just fails silently instead of reporting diagnostics

- Using escaped reserved keyword (e.g. `@const`) for options/arguments/etc can behave weirdly/unexpectedly or even break the generated code

- The help always contains a line for the CLI without command and one with commands, even if one of those scenarios/invocations is invalid

- It also prints empty command sections :shrug:

- We don't emit any error when someone defines an arg or option of an unsupported type

- Options declared in the "Entry Point" method parameter list are currently not supported, even they could be implemented just like for normal commands. The bug is that I'm lazy
