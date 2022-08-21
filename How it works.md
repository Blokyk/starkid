# How it works

## Analysis

- Find a class marked `[CLI]`

- Get the app name and potential entry point from that attribute

- Check if the class is also marked with `[Description]`, and get the desc from it if that's the case

- Get all members of the class and collect options, descriptions and commands
  
  - For properties, fields and methods, check if they're marked with `[Option]`. If that's the case, extract the long name, alias, arg name, description (via the `[Description]` attribute), and default value.
    
    - If it's a method, we also check if its return type is one of `bool`, `int`, `string`, or `Exception`, and if that's the case, it marks that option as needing auto-handling later.
  
  - Recheck methods to find [sub]commands, and only accept ones that return either `int` or `void`. Scan signature for options and arguments with the same rules as above.

> `todo` ~~Check that every option and command have unique names and backing symbols~~ 

- If the CLI has an entry point (named `EPName` here) :
  
  1. Check if any command has a `BackingSymbol` with a name that matches `EPName`. Go to step 4 if that's the case
  
  2. Try to find a method with a matching name, and check that there's exactly one.
  
  3. Parse that method as if it was a command, while dropping the requirement for the attributes and adding a few restrictions (like no option parameters)
  
  4. Set the `rootCmd` object to the command, but change the name and description to the appropriate values and set ParentSymbolName to null

- Bind every command to its parent based on the name from the based on the ParentSymbolName. It is important to note that commands only store their *parent*, but not children. If the CLI has an entry point, we also set every top-level commands' parent to be `rootCmd`

## Generation & command descriptors

The generated parser uses **command descriptors** (`CmdDesc`), which contains info about a command's flags, options and subs, to understand the app's interfaces and set the right values. The base `CmdDesc` class is abstract and defines the "interface" for getting gathering info about a command from the outside; it is also responsible for managing positional arguments based on the definitions of derived cmd descriptions via the `TryAddPosArg()` method. The base `CmdDesc` also contains the global flags and options for the app, which every other command then inherits.

> **Note**: Options (and flags) are stored in a `Dictionary<string, Action<string?>>`, which maps raw option names (e.g. `--version`, or `-v`) to special thunks, which are responsible for setting/storing the value of each option. Descriptors fake a sort of "inheritance" between those by basically updated its base's dictionary to add its own options. This is a terrible idea only barely excused by the fact I wrote the initial prototype in about an hour and am now too afraid to break it.
> 
> While most thunks are pretty simple, there's two special cases we have to handle here :
> 
> - *Options defined as a parameter to a method/cmd* -- While for top-level options, state is simply managed by the static members of the CLI class, for non-global options, the `CmdDesc` object has to keep track of its value since it will need to pass it to the method when the command is actually called.
> 
> - *Method-backed options* -- Options which rely on a method call rather than a simple assignment have to be special-cased, both because of the change in syntax, but also because they might need auto-handling, which requires wrapping the call with the `ThrowIfNotValid` helper.

A descriptor is always generated for the app, whether it actually has a root command (i.e. entry point) or not; it always contains the help text as well as the sub commands list. This is because `CmdDesc` is abstract, and since `Invoke()` (cf later) and `HelpString` are both instance members, we need a basic/default instance to query and interact with in the parser. The app's descriptor also contains the list of top-level commands, since sub-command lists are **not** inherited.

The inheritance between descriptors means that, if not overridden by the current desc, a global/earlier option can be used, which is another key feature that `System.CommandLine` doesn't provide by default (or rather, not without boilerplate).

Since one of the main feature of this parser was avoiding reflection and runtime-overhead, sub commands are 
