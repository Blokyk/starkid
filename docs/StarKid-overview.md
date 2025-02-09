# StarKid overview

1. [What is StarKid?](#what-is-starkid)
2. [The core model](#the-core-model)
3. [Commands, options, and groups](#commands-options-and-groups)
   1. [Commands](#commands)
   2. [Options and flags](#options-and-flags)
   3. [Command groups](#command-groups)
4. [Parsing & validation](#parsing--validation)
   1. [Auto-parsers](#auto-parsers)
   2. [Manual parsers](#manual-parsers)
   3. [Validators](#validators)
5. [Advanced notions](#advanced-notions)
   1. [Special arguments](#special-arguments)
   2. [Global options](#global-options)
   3. [Default commands](#default-commands)
6. [Help text generation and customization](#help-text-generation-and-customization)
   1. [How help text is generated](#how-help-text-is-generated)
   2. [Customization](#customization)
7. [Tips & Tricks](#tips--tricks)
   1. [Creating a single-command app](#creating-a-single-command-app)
   2. [Executable options, or how to implement `--version`](#executable-options-or-how-to-implement-version)
8. [Restrictions](#restrictions)
   1. [Classes/methods/fields must be `internal`](#classesmethodsfields-must-be-internal)
   2. [Classes/methods/fields must be `static`](#classesmethodsfields-must-be-static)
   3. [No generics anywhere](#no-generics-anywhere)
   4. ["One command apps" are impossible to create](#one-command-apps-are-impossible-to-create)

## What is StarKid?

StarKid is a source generator that aims to make writing command line
interfaces (a.k.a. CLI) easier. To achieve that goal, it helps you
focus on the actual *interface* part, i.e. options, commands and
subcommands, arguments, etc, and generates all the necessary
plumbing for you, like formatting the help texts for each command, or
validating each option or argument before calling any command.
StarKid is mainly built for CLIs making use of "verbs," i.e. groups
of subcommands, and has sensible defaults builtin; however, it also
aims to provide some flexibility when needed, and has many ways to be
tweaked to suit your needs (including via the [build-time config](StarKid-config.md)).

> [!NOTE]
> Although there are quite a lot of references to `System.CommandLine`
> throughout both StarKid's documentation and its codebase, it should
> be noted that StarKid's functional goals are pretty different from
> [System.CLI's goals](https://github.com/dotnet/command-line-api/blob/main/docs/Functional-goals.md).
>
> That library is a very different workhorse, and awesome in its own
> right! It is clearly cut-out for some pretty heavy workloads and
> scenarios, and has had a lot more work and expertise put into it
> than StarKid (I'm just a student working on this in their spare time
> after all!)
>
> You can find my reasons for writing this library regardless in [this project's README](/README.md),
> but for short: I did it for fun, mostly, and because reflection is evil!

## The core model

StarKid aims to have as simple a mental model as possible; In fact,
it can even be stated in just two sentences: "Commands (methods)
accept arguments and options (parameters). You can create groups
(classes) of related commands, and you can even have options for the
whole group (fields/properties) that subcommands can then use."

Do you need to have a group inside another group? Then just nest the
corresponding classes, and that's it!

And that's basically all you need to know to get started! Obviously,
StarKid provides some additional features, which we will explore
below, but those two sentences are all you really need to think about
when working with this package, almost everything else will come
naturally from your code.

> You might also want to take a look at [StarKid's general restrictions](#restrictions)!

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

## Commands, options, and groups

> [!NOTE]
> This is mostly intended to be a piece of documentation,
> not a tutorial. If you want a gentle introduction to StarKid's
> base concepts and how to make your first app, you should check out
> [Your first app with StarKid](./Your-first-app-with-StarKid.md).

There are two core concepts in StarKid, commands and command groups,
which are represented by two attributes: `[Command]` and
`[CommandGroup]`. A command is something that actually *executes*,
while a command group, as the name indicates, is simply a group of
related commands, that doesn't have any intrinsic behavior. This
analogous to the relationship between methods and classes: methods
are the things that actually run, while classes just regroup multiple
methods together (+do some other stuff in OOP); it is such a good
analogy, in fact, that commands are represented by methods, and
groups by classes.

> [!IMPORTANT]
> Much like C# with methods and classes, StarKid does not allow
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
requires 42 arguments for some reason, then so be it, StarKid won't
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
) => Console.WriteLine("l.o.l.");
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
always be set to true when used, unless the user uses the
`--my-flag=false` syntax.

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

Now that we know the ins-and-outs of commands and options, we're ready
to tackle the last big concept in StarKid: command groups. As the name
implies, they allow you to group related commands under a parent
command. For example, `git remote` is a group of commands that operate
on remotes: `add`, `show`, `remove`, etc. In fact, `git` itself is
also a group of commands that all operate on git repos: `status`,
`add`, `commit`, etc. Those all have multiple common options or flags,
like `--git-dir`, which would be annoying to reimplement for each
subcommand.

StarKid has been built with command groups at its heart, and since
commands are represented by methods, it seems only natural that groups
of commands would be classes. Naturally, you can also have groups
inside groups; this is achieved by nesting classes (and not
sub-classing, because [everything in StarKid is static](#classesmethodsfields-must-be-static))
inside each other. Let's try to model `git`'s CLI using StarKid:

```csharp
[CommandGroup("git")]
static class ToyGit {
    [Option("git-dir")] public static FileInfo gitDir;

    [Command("add")]
    public static void Add(FileInfo pathspec)
	=> Console.WriteLine($"Adding {pathspec} to index.");

    [CommandGroup("remote")]
    public static class RemoteCmd {
        [Command("add")]
        public static void Add(string name, Uri url)
            => Console.WriteLine($"Adding {name} remote from {url}");

        [Command("show")]
        public static void Show()
            => Console.WriteLine($"List of remotes: ...");

        [Command("remove")]
        public static void Remove(string name)
            => Console.WriteLine($"Removing remote {name}");
    }
}
```

> todo: the rest

## Parsing & validation

> [!NOTE]
> In the following sections, *operand* refers to either an
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
StarKid allows you to do away with a lot of boilerplate code by
automatically finding a way to convert a string into whatever type is
required for the operand, relying on .NET's convention of the
`TryParse`/`Parse` pattern to cover most common cases and to easily
allow you to implement it for your own types.

Specifically, given an operand which should have type `T`, StarKid
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

This, however, does have limits. Besides [StarKid's general restrictions](#restrictions),
the auto parser cannot:
- Discover extension methods ([issue #7](https://github.com/Blokyk/fuzzy-octo-chainsaw/issues/7))

### Manual parsers

In case StarKid cannot find a parsing method automatically, or if
you'd like to override the default one, you can specify one yourself
using the `[ParseWith(...)]` attribute:

> [!WARNING]
> You ***have*** to specify the method's name inside a `nameof()`
> expression. You *cannot* just use a string instead!
>
> Ideally, you could use the same syntax as when converting a method
> group to a delegate, but for technical reasons, using delegates as
> argument in attributes is currently impossible in .NET, and the
> [issue](https://github.com/dotnet/csharplang/issues/343) currently
> tracking it is mostly inactive (go revive it if you want to see
> that feature!).

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

You can check [StarKid's general restrictions](#restrictions) for
a list of restrictions on what can and can't be a manual parsing
method, but basically: almost anything that isn't generic. Because
of the `nameof` mechanism, it is unfortunately impossible to specify
type argument for a generic method, and inferring them would probably
be too expansive in terms of performance (and even then, that's
assuming there *is* a perfect fit for it, which is not a given!)

### Validators

You might want to run some sort of validation on operands before
running any command, and StarKid has a tool for that called...
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
public static FileInfo saveFile;

public static bool FileIsAbsolute(FileInfo f)
    => Path.GetExtension(f.Name) is ".dat" or ".sav";
```

This will first check if it ends with the right extension, and then
whether it exists or not.

In terms of restrictions, validators have mostly the same limitations
as manual parsers (in addition to [StarKid's general restrictions](#restrictions))

## Advanced notions

### Repeatable options

In general, options are meant to be specified at most once. Repeated
options are almost always an error on the user's or programmer's part.
However, sometimes you *do* want to have an option that can be used
multiple times. This is possible in StarKid by using arrays!

At its simplest, this just looks like adding `[]` to an option's type.
```cs
[Option("filter", 'f')]
public static string[] Filters;
```

This then allows the user to specify the `--filter` option multiple
times: `./foo process myfile.mp4 --filter vignette --filter grain -f invert`.

This can also be combined with [parsers and validators](#parsing--validation)!

In the case of auto-parsers, you don't need to do anything special: each
use of an `int[]` option will be parsed as an `int` on the command line.

For manual parsers, you can specify a parser for the element type, and
every use of the option will be parsed individually using that parser:

```cs
[Option("output", 'o')]
[ParseWith(nameof(Resolution.FromString))]
public static Resolution[] OutputResolutions;
```

With this, we could have a command line like:
```sh
./foo process myfile.mp4 -f grain -o 1920x1080 -o 1280x720 -f invert
```

> [!NOTE] No need to worry about any interference with "one-off" options
> with an array typed but that are parsed from a single string: direct
> type equality takes precedence over element-type comparison, meaning
> that a parser that outputs `int[]` will *not* create a repeatable
> option. (Since arrays can never be auto-parsed directly, there is no
> ambiguity there either!)

You can also have validators for each item, and even for the entire
array!

```cs
[Option("input", 'i')]
[ValidateWith(nameof(FileInfo.Exists))] // you could also use a method, obviously
[ValidateWith(nameof(NoDuplicateFiles))]
public static FileInfo[] Inputs;

static bool NoDuplicateFiles(FileInfo[] files)
    => files.Length == files.Distinct().Count();
```

This would first validate each item by checking the `.Exists` property
of each, and then **when building the final array**, call `NoDuplicateFiles`
on the whole array.

### Global options

By default, options are only scoped to the command or group they were
defined in. For example, this means that an option defined for the
`git subtree` group, like `--prefix`, cannot be used *after* a
subcommand. You can write `git subtree --prefix ./vendor add`, but not
`git subtree add --prefix ./vendor`. This is particularly useful when
you might have conflicting options or aliases between a group and its
subcommands.

However, in some cases, you might want to have an option that can be
used at any level and with any subcommand. This is often the case of the
`verbose` option, which applies no matter what the command is. For this
purpose, StarKid's `[Option]` has the `IsGlobal` property, which allows
you to scope an option to all of a group's descendants.

As an example, the following would allow `bdsys -v pkg update <name>`
but also `bdsys pkg update -v <name>` or `bdsys pkg -v update <name>`.

```cs
[CommandGroup("bdsys")]
public static class BdsysCli {
    [Option("verbose", 'v', IsGlobal = true)]
    public static bool ShouldBeVerbose { get; set; }

    [CommandGroup("pkg")]
    public static class Package {
        public static void Update(string pkgName) {
            if (ShouldBeVerbose)
                Console.WriteLine($"Updating package {pkgName}");
            ...
        }
    }
}
```

> [!NOTE] There is now way to *shadow* global options, i.e. declare
> an option that has the same name or alias as a global option from
> a parent command group. In our example, `Package` couldn't declare
> its own `verbose` option, nor an option with the `v` alias. Therefore,
> you should think carefully about how making an option global might
> impact your CLI's future design/evolution.

### Default commands

Some CLIs do not adhere strictly to the group/command dichotomy, and
instead allow some "groups" to also be used as commands. This is the
case of `git remote`, for example. It can be used as a group (
`git remote add`, `git-remote set-url`, etc.), but it can also be used
as a standalone command, being equivalent to `git remote show`.

To achieve this with StarKid, there is a `DefaultCmdName` property
on the `[CommandGroup]` attribute. It allows you to specify the name of
a command[^cmdname] that will be invoked by default in case no
subcommand was specified.

So, to replicate the `git remote` behavior:

```cs
[CommandGroup("git")]
public static class GitCli {
    [CommandGroup("remote", DefaultCmdName = "show")]
    public static class Remote {
        [Option("verbose", 'v', IsGlobal = true)]
        public static bool ShouldBeVerbose { get; set; }

        [Command("show")]
        public static void Show(
            [Option("dry", 'n')] bool isDryRun
        ) { ... }

        [Command("add")]
        public static void Add(...) {...}
    }
}
```

This allows us to enjoy both `git remote add` to add new remotes, as
well as the classic `git remote -v` to show a list of remote. Importantly,


[^cmdname]: that is, the name of the command on the CLI, not its
associated method, for annoying reasons

### Special arguments

> todo: params
> todo: optional args

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
> todo: StarKid_Help_MaxCharsPerLine
> todo: StarKid_Help_ArgNameCasing

#### Custom argument/option value names

## Tips & Tricks

### Creating a single-command app

> *WARNING*: This is explicitly NOT an expected use-case for
> StarKid. WHile I've bent StarKid in a few places to allow it,
> bug reports or feature requests for this scenario will
> probably have lower priority, and PRs focusing on it might
> be rejected simply to avoid "bloat".

Although StarKid is meant for subcommand-oriented CLIs, [default commands](#default-commands)
make it possible to write an app without ay subcommand, controlled
entirely with options and flags. Let's say we want to implement
a terminal calculator like `bc`. Here's `bc`'s help text:

```
usage: bc [options] [file ...]
  -h  --help         print this usage and exit
  -i  --interactive  force interactive mode
  -l  --mathlib      use the predefined math routines
  -q  --quiet        don't print initial banner
  -s  --standard     non-standard bc constructs are errors
  -w  --warn         warn about non-standard bc constructs
  -v  --version      print version information and exit
```

(The `file...` argument it accepts is a list of files containing
expressions and `bc` statements.)

As you can see, it doesn't have any subcommands, so we'll need to
maneuver a bit to get this working under StarKid. To accomplish
this, we'll create a root command group, and then give it a single
*default* command:

```csharp
[CommandGroup("bc", DefaultCmdName = "#")]
static class App {
    [Command("#")]
    public static void Main(params FileInfo[] files) => ...;
}
```

The `#` command name is special: it makes the command un-invokable
and hides it from the help text. Thus, the only way to invoke it
is to set it as a default command for some group.

In this case, we assign `Main()` as the root command group's default
command. Since there is no other subcommand to invoke, it will be
ran every time the group is invoked, which is every time, since it's
the root group. We'd just have to add the options and implement `Main()`,
and our `bc` clone would be complete!

```csharp
[CommandGroup("bc", DefaultCmdName = "#")]
static class App {
    [Option("interactive", 'i')]
    public static bool forceInteractive;

    /* ... */

    [Command("#")]
    public static void Main(params FileInfo[] files) {
        Console.WriteLine("<?> = 42");
    }
}
```

A full sample can be found in [`samples/bc/Program.cs`](samples/bc/Program.cs).

### Executable options, or how to implement `version`

> todo: explain trick with properties

> todo: warn that this kinda breaks the semantics, and it's also
> somewhat unsupported (I probably won't voluntarily break it, but
> order of execution of options is definitely not stable, especially
> w.r.t. when things are parsed vs validated vs assigned=executed)

### Repeat-based verbosity level (and other count-based flags)

> todo: `bool[]` + .Count

## Restrictions

As a source generator, StarKid has some base restrictions that we
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
  [visibility restriction](#classesmethodsfields-must-be-internal)
  for an example)

### Classes/methods/fields must be `internal`

One of the first restriction you'll encounter is that most things
that interact with StarKid has to be `internal` or `public`. This
includes classes marked `[CommandGroup]`, methods marked `[Command]`,
fields/properties marked `[Option]`, as well as methods and props
used for parsing and validation.

<details>
<summary>Reason</summary>
The reason why everything has to be at least `internal` is because
StarKid generates code *outside* the command classes, and thus it
has to be able to reference the fields, methods and classes that make
up the different groups and commands.

We could partly replace the `internal` restriction with a `partial`
one, by generating things "inside" the participating classes, which
is more common for source generators generally. However, not
*everything* could be marked `private`, and the distinction between
what's allowed and what isn't might seem arbitrary and a bit
clunky to most users.

To figure out why, let's go through a little example. We'll assume
that StarKid generates everything needed (`Main()`, command
descriptors, etc...) in the top-most command group class. Let's say
we have the following class structure:

```
MyCLI
├── Foo
│   └── Fib
├── Bar
│   └── Bob
└── StarKidStuff
    ├── Main()
    ├── MyCLICmdDesc
    └── ...
```

So, our goal is to make as many classes private as possible. Remember
that every field/method/nested class has to be accessible inside of
`StarKidStuff`, otherwise it won't work. To start off, I've got bad
news: `MyCLI` cannot be private, because top-level type declarations
have to be internal or public. Thankfully, every member of `MyCLI`
*can* be private, since `StarKidStuff` is itself a member and thus
can access everything above or on the same nesting level as itself.
This means that *technically* `Foo` and `Bar` can be private, since
they're also members of `MyCLI`... but that's only a shallow victory,
because StarKid's code will need to access *their* members, so
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
> subcommands), and there are quite a lot of difference between
> single- and multi-command scenarios. [While it is possible](#bonus-using-default-commands-to-write-a-single-command-app),
> to write single-command apps, please understand that this not
> really a scenario we favor, and we will not be making any huge
> changes to StarKid just to cater to it. If you find *do* a
> **non-intrusive** way to make it easier that also matches the
> criteria we outlined earlier, please [do file an issue](https://github.com/blokyk/starkid/issues)
> so that we can discuss it! I'd also eventually like to make an
> alternative generator specialized for single-command apps, which
> would probably re-use a lot of StarKid plumbing, so if that's
> something you'd like to work on, don't hesitate to hit me up!
