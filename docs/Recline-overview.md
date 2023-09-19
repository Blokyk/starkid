# Recline overview

### What is Recline?

Recline is a source generator that aims to make writing command line
interfaces (a.k.a. CLI) easier. To achieve that goal, it helps you
focus on the actual *interface* part, i.e. options, commands and
sub-commands, arguments, etc, and generates all the necessary
plumbing for you, like formatting the help texts for each command, or
validating each option or argument before calling any command.
Recline is mainly built for CLIs making use of "verbs," i.e. groups
of subcommands, and has sensible defaults builtin; however, it also
aims to provide some flexibility when needed, and has many ways to be
tweaked to suit your needs (including via the [build-time config](Recline-config.md)).

> **Note**: Although there are quite a lot of references to `System.CommandLine`
> throughout both Recline's documentation and its codebase, it should
> be noted that Recline's functional goals are pretty different from
> [System.CLI's goals](https://github.com/dotnet/command-line-api/blob/main/docs/Functional-goals.md).
>
> That library is a very different workhorse, and awesome in its own
> right! It is clearly cut-out for some pretty heavy workloads and
> scenarios, and has had a lot more work and expertise put into it
> than Recline (I'm just a student working on this in their spare time
> after all!)
>
> You can find my reasons for writing this library regardless in [this project's README](/README.md),
> but for short: I did it for fun, mostly, and because reflection is evil!

## The core model

Recline aims to have as simple a mental model as possible; In fact,
it can even be stated in just two sentences: "Commands (methods)
accept arguments and options (parameters). You can create groups
(classes) of related commands, and you can even have options for the
whole group (fields/properties) that subcommands can then use."

Do you need to have a group inside another group? Then just nest the
corresponding classes, and that's it!

And that's basically all you need to know to get started! Obviously,
Recline provides some additional features, which we will explore
below, but those two sentences are all you really need to think about
when working with this package, almost everything else will come
naturally from your code.

> You might also want to take a look at [Recline's general restrictions](#restrictions)!

<details>
<summary>A small example</summary>

```csharp
[CommandGroup("my-git")]
public static class MyGit
{
    [Option("git-dir")]
    public static FileInfo gitDir = new FileInfo("./.git");

    [CommandGroup("remote")]
    public static class Remote
    {
        [Option("verbose", 'v')]
        public static bool verboseFlag;

        [Command("add")]
        public static int Add(
                                    string remoteName,
                                    Uri repoPath,
            [Option("branch", 't')] string branchName
        ) { ... }

        [Command("remove")]
        public static int Remove(string remoteName) { ... }
    }

    [Command("clone")]
    public static void Clone(Uri repository, FileInfo? directory) { ... }
}
```

This small snippet will result in the following CLI:

```shell
./my-git [--git-dir <path>] clone <repository> [directory]
./my-git [--git-dir <path>] remote [-v | --verbose] add <remoteName> <repoPath> [-t | --branch <branchName>]
./my-git [--git-dir <path>] remote [-v | --verbose] remove <remoteName>
```

Or, as a tree:

```
my-git [--git-dir <path>]
├── clone <repository> [directory]
└── remote [-v | --verbose]
    ├── add <remoteName> <repoPath> [-t | --branch <branchName>]
    └── remove <remoteName>
```

As you can see, the structure of your CLI is exactly identical to
you code structure.
</details>

## Commands, groups and options

> **Note**: This is mostly intended to be a piece of documentation,
> not a tutorial. If you want a gentle introduction to Recline's
> base concepts and how to make your first app, you should check out
> [Your first app with Recline](./Your-first-app-with-Recline.md).

There are two core concepts in Recline, commands and command groups,
which are represented by two attributes: `[Command]` and
`[CommandGroup]`. A command is something that actually *executes*,
while a command group, as the name indicates, is simply a group of
related commands, that doesn't have any intrinsic behavior. This
analogous to the relationship between methods and classes: methods
are the things that actually run, while classes just regroup multiple
methods together (+do some other stuff in OOP); it is such a good
analogy, in fact, that commands are represented by methods, and
groups by classes.

> Much like C# with methods and classes, Recline does not allow
> commands to exist outside of a group. See [the relevant paragraph](#one-command-apps-are-impossible-to-create)
> for more details.

### Commands

As stated above, commands are simply methods with a `[Command]`
attribute. For example:

```csharp
[CommandGroup("door-sim")]
public static class DoorSimulator {
    [Command("greet")]
    public static void SayHello() => Console.WriteLine("Hello!");
}
```

Just like methods can have parameters, commands can have arguments;
in fact, by default, parameters directly correspond to arguments.

This example...
```csharp
[Command("greet")]
public static void SayHello(string name) =>
    Console.WriteLine($"Hello, {name}!");
```
...would create a new command with a mandatory argument.

```
> ./door-sim greet Lila
Hello, Lila!
```

Of course, you can have parameters of any types, as long as there
is a way to convert from a string to that type (see [the section on parsing](#parsing--validation)
for more info.)

You can also have as many parameters as you want, so if your command
requires 42 arguments for some reason, then so be it, Recline won't
even notice!

```csharp
[Command("register")]
public static void PleaseNeverDoThis(
  int group,
  Guid id,
  string firstName,
  string lastName,
  ...36 parameters later...
  float pixelDensity,
  FileInfo idCardSignature
) => Console.WriteLine("Nope.");
```

### Options and flags

However, CLIs with more than 2 or 3 arguments are quite rare; in most
cases it's a better idea to use options and flags to parameterize a
command. This can be achieved by marking a parameter with `[Option]`.

For example, we could turn the `name` argument into an option:

```csharp
[Command("greet")]
public static void SayHello([Option("name")] string? name) =>
    name is null
        ? Console.WriteLine("Hello!")
        : Console.WriteLine($"Hello, {name}!");
```

```
> ./door-sim greet
Hello!
> ./door-sim greet --name Max
Hello, Max!
```

Flags (sometimes also called switches) are simply options with the
`bool` type. While options always require an operand, flags will
always be set to true when specified, unless the user uses
`--my-flag=false`.

```csharp
[Command("greet")]
public static void SayHello([Option("name")] string? name, [Option("louder")] bool louder) {
    var msg = name is null ? "Hello!" : $"Hello, {name}!";

    if (louder)
        msg = msg.ToUpper(); + "!!";

    Console.WriteLine(msg);
}
```

```
> ./door-sim greet --name Emily --louder
HELLO, EMILY!!!
> ./door-sim greet --name Emily --louder=false
Hello, Emily!
```

### Command groups

### Special arguments

> todo: params
> todo: optional args

### Global options

### Default commands

#### Bonus: Using default commands to write a single-command app

## Parsing & validation

> **Note:** In the following sections, *operand* refers to either an
> argument or the value of an option. For example:
>
> ```shell
> # myApp <some-file-arg> [--log <some-other-file>]
> ./myApp foo.txt --log log.txt
>         ^~~~~~^       ^~~~~~^
>         operand       operand
> ```
>
> For clarity, a lot of the following examples use fields marked as
> options to demonstrate the mechanisms, but all of them also work on
> arguments and command-level (parameter) options.

### Auto-parsers

Part of making the relation between code and CLI as transparent as
possible is also being able to easily express restrictions on values,
which is most often expressed through the type of each operand.
Recline allows you to do away with a lot of boilerplate code by
automatically finding a way to convert a string into whatever type is
required for the operand, relying on .NET's convention of the
`TryParse`/`Parse` pattern to cover most common cases and to easily
allow you to implement it for your own types.

Specifically, given an operand which should have type `T`, Recline
will check:
- Is `T == string`? If yes, don't do anything
- Is `string` implicitly castable to `T`? If yes, don't do anything
- Is `T` an enum? If yes, use the `Enum.TryParse` method
- Does `T` have a constructor accepting a single parameter of type
  `string`? If yes, then use that constructor
- Does `T` have a static method `T Parse(string)`? If yes, use
  that method. *Those are sometimes called direct parsers*
- Does `T` have a static method `bool TryParse(string, out T)`?
  If yes, use that method, using the return value to determine if
  there was an error. *Those are sometimes called indirect parsers*

This, however, does have limits. Besides [Recline's general restrictions](#restrictions),
the auto parser cannot:
- Discover extension methods ([issue #7](https://github.com/Blokyk/fuzzy-octo-chainsaw/issues/7))

### Manual parsers

In case Recline cannot find a parsing method automatically, or if
you'd like to override the default one, you can specify one yourself
using the `[ParseWith(...)]` attribute:

> **Note**: For technical reasons, using a method or lambda as an
> argument in attributes is currently impossible in .NET, and the
> [issue](https://github.com/dotnet/csharplang/issues/343) currently
> tracking it is mostly inactive (go revive it if you want to see
> that feature!), so you ***have*** to specify the method > inside
> a `nameof()` expression. You *cannot* just use a string instead!

```csharp
[Option("range")]
[ParseWith(nameof(ParseRange))]
public static (int start, int end) range;

/* ... */

public static (int start, int end) ParseRange(string str) {
    var parts = s.Split("..", 2);

    if (parts.Length != 2 || parts.Any(p => p.Length == 0))
        throw new FormatException("Range must be of the format 'num..num'");

    var start = Int32.Parse(parts[0]);
    var end = Int32.Parse(parts[1]);

    if (start > end)
        throw new Exception("The range's ending value must be greater than or equal to its starting value!");

    return (start, end);
}
```

Manual parsing methods can take two forms (names don't matter):
- `bool TryParseStuff(string, out T?)`
- `T ParseStuff(string)`

Contrary to classic C# code, direct `Parse`-style methods are
preferred over `TryParse`-style ones when used for parsing operands.
The reason for this is that the latter gives a lot less feedback:
you only know that your input is either valid or invalid, but not
why it might be invalid. If we used the `TryParse` pattern in the
example above, then there would be no way for the user to know *what*
was wrong with their input, only that it was invalid. However, by
using exceptions directly, it becomes instantly clear to the user
what they need to change in case of a parsing error.

You can check [Recline's general restrictions](#restrictions) for
a list of restrictions on what can and can't be a manual parsing
method, but basically: almost anything that isn't generic. Because
of the `nameof` mechanism, it is unfortunately impossible to specify
type argument for a generic method, and inferring them would probably
be too expansive in terms of performance (and even then, that's
assuming there *is* a perfect fit for it, which is not a given!)

### Validators

You might want to run some sort of validation on operands before
running any command, and Recline has a tool for that called...
validators! They can especially useful when you have an option in a
command group with a bunch of subcommands: traditionally, you'd
have to add one or two lines at the start of each method to validate
that specific option. Multiply that by 3, 5 or 10 options, and
your commands start to be pretty heavily bloated! With validators,
it doesn't matter where or how many times that option or argument
is used

To specify a validator, you use the `[ValidateWith(...)]` attribute:

```csharp
[Option("speed")]
[ValidateWith(nameof(FloatIsPositive))]
public static float speed;

/* ... */

public static bool FloatIsPositive(float val)
    => val >= 0;
```

Validator methods come in two variants as well:
- `bool IsValid(T)`
- `void Check(T)`

Those have similar tradeoffs to `TryParse`/`Parse` in `ParseWith`
attributes, except that the `ValidateWith` attribute accepts an
optional message to be displayed when the validation fails. Thus,
you can easily re-use validators for different members and still
get complete control over error messages. For example:

```csharp
[Option("framework", 'f')]
[ValidateWith(nameof(IntIsPositive), "Framework version must be positive.")]
public static int frameworkVersion;

[Option("rank", 'k')]
[ValidateWith(nameof(IntIsPositive), "Negative rank values are not supported anymore.")]
public static int rankMagnitude;
```

The `ValidateWith` attribute also supports directly using bool
properties for validation. This greatly reduces the boilerplate
needed in a lot of cases. For example, if you simply want to check
that a file exists before, you could do the following:

```csharp
[Option("log-dir", 'd')]
[ValidateWith(nameof(DirectoryInfo.Exists))]
public static DirectoryInfo logDir;
```

Additionally, you can specify multiple validators for a single
operand. They will be applied in series, in the same order as their
corresponding attribute.

```csharp
[Option("save-file")]
[ValidateWith(nameof(FileHasSaveExtension), "Save files must end in either .sav or .dat")]
[ValidateWith(nameof(FileInfo.Exists))]
public static FileInfo savefile;

public static bool FileIsAbsolute(FileInfo f)
    => Path.GetExtension(f.Name) is ".dat" or ".sav";
```

This will first check if it ends with the right extension, and then
whether it exists or not.

In terms of restrictions, validators have mostly the same limitations
as manual parsers (in addition to [Recline's general restrictions](#restrictions))

## Help text generation and customization

> fixme: should we put error messages:
> fixme:  - here
> fixme:  - in the relevant parsing/validator sections
> fixme:  - in a separate h2 section

### How help text is generated

> todo: what's displayed on each "page"
> todo: + mention quickly xml with a link to #customization

#### Textual output

> todo: talk about the two column system (+ when line breaks are inserted)

### Customization

> todo: allowed xml tags
> todo: Recline_Help_MaxCharsPerLine

#### Custom argument/option value names


## Restrictions

As a source generator, Recline has some base restrictions that we
either could never avoid, or have chosen to not fight against, often
because it would:

- be too hard to implement a work-around, or the implementation would
  too fragile
- be too costly in terms of generator-runtime
  - performance in the source generator is something we're already
    struggling with, and we have to keep a constant watch on it if
    we want to make sure we're not destroying people's IDEs
- be at odds with the simple mental model we want to keep, or
  otherwise conflict with the rest of the design
- introduce some other, hidden, arbitrary restriction (see the
  [visiblity restriction](#classesmethodsfields-must-be-internal)
  for an example)

### Classes/methods/fields must be `internal`

One of the first restriction you'll encounter is that most things
that interact with Recline has to be `internal` or `public`. This
includes classes marked `[CommandGroup]`, methods marked `[Command]`,
fields/properties marked `[Option]`, as well as methods and props
used for parsing and validation.

<details>
<summary>Reason</summary>
The reason why everything has to be at least `internal` is because
Recline generates code *outside* the command classes, and thus it
has to be able to reference the fields, methods and classes that make
up the different groups and commands.

We could partly replace the `internal` restriction with a `partial`
one, by generating things "inside" the participating classes, which
is more common for source generators generally. However, not
*everything* could be marked `private`, and the distinction between
what's allowed and what isn't might seem arbitrary and a bit
clunky to most users.

To figure out why, let's go through a little example. We'll assume
that Recline generates everything needed (`Main()`, command
descriptors, etc...) in the top-most command group class. Let's say
we have the following class structure:

```
MyCLI
├── Foo
│   └── Fib
├── Bar
│   └── Bob
└── ReclineStuff
    ├── Main()
    ├── MyCLICmdDesc
    └── ...
```

So, our goal is to make as many classes private as possible. Remember
that every field/method/nested class has to be accessible inside of
`ReclineStuff`, otherwise it won't work. To start off, I've got bad
news: `MyCLI` cannot be private, because top-level type declarations
have to be internal or public. Thankfully, every member of `MyCLI`
*can* be private, since `ReclineStuff` is itself a member and thus
can access everything above or on the same nesting level as itself.
This means that *technically* `Foo` and `Bar` can be private, since
they're also members of `MyCLI`... but that's only a shallow victory,
because Recline's code will need to access *their* members, so
anything inside `Foo`/`Bar` will need to be public, including `Fib`
and `Bob` (and anything inside *those*).

TLDR, we end up with only *one* level that can be private, and even
then any level below that has to be public... This is annoying at
best and confusing at worst.
</details>

### Classes/methods/fields must be `static`
> todo: why it's easier and why it's better for design *and* perf

### No generics anywhere
> todo: explain that this could be lifted for parsers/validators

### "One command apps" are impossible to create
> todo: we tried and... it was bad perf-wise AND design-wise (easy to
> make mistakes, and going from single-command to multi either
> requires a shift in mental model OR is really cumbersome to use
> with attributes everywhere and hidden dependencies)

> originally from the "commands and groups" section:
> This is because it is heavily geared towards
> multi-command apps (CLIs with multiple levels of commands and
> sub-commands), and there are quite a lot of difference between
> single- and multi-command scenarios. [While it is possible](#bonus-using-default-commands-to-write-a-single-command-app),
> to write single-command apps, please understand that this not
> really a scenario we favor, and we will not be making any huge
> changes to Recline just to cater to it. If you find *do* a
> **non-intrusive** way to make it easier that also matches the
> criteria we outlined earlier, please [do file an issue](https://github.com/blokyk/recline/issues)
> so that we can discuss it! I'd also eventually like to make an
> alternative generator specialized for single-command apps, which
> would probably re-use a lot of Recline plumbing, so if that's
> something you'd like to work on, don't hesitate to hit me up!