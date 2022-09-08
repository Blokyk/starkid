namespace Recline.Generator;

internal static class Diagnostics {
    public static readonly DiagnosticDescriptor TimingInfo
        = new(
            "CLI000",
            "{0} took: {1:0}ms ({1:0.000)}",
            "{0} took: {1:0}ms ({1:0.000)}",
            "Debug",
            DiagnosticSeverity.Info,
#if DEBUG
            true
#else
            false
#endif
        );

    public static readonly DiagnosticDescriptor TooManyCLIClasses
        = new(
            "CLI001",
            "Only one class may be marked with [CLI]",
            "Both classes '{0}' and '{1}' are marked with [CLI]",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor BothCmdAndSubCmd
        = new(
            "CLI002",
            "Member '{0}' can't have both [Command] and [SubCommand] attributes",
            "A member can't be both an command and a sub-command",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor BothOptAndCmd
        = new(
            "CLI003",
            "Member '{0}' can't have both [Option] and [Command/SubCommand] attributes",
            "A member can't be both an option and a command",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParseOnNonOptOrArg
        = new(
            "CLI004",
            "[ParseWith] can only be used on options or arguments",
            "[ParseWith] can only be used on options or arguments",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidateOnNonOptOrArg
        = new(
            "CLI005",
            "[ValidateWith] can only be used on options or arguments",
            "[ValidateWith] can only be used on options or arguments",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParamsHasToBeString
        = new(
            "CLI006",
            "Can't use type '{0}' for CLI's params argument",
            "'params' arguments must be of type string[]",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidColumnLength
        = new(
            "CLI007",
            "Invalid column length in project settings",
            "Invalid column length in project settings",
            "Recline.Config",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdMustBeStatic
        = new(
            "CLI100",
            "Method '{0}' must be static to be a command",
            "Methods marked with [Command] must be static",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdCantBeGeneric
        = new(
            "CLI101",
            "Generic method '{0}' cannot be a command",
            "Methods marked with [Command] can't be generic",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdMustBeOrdinary
        = new(
            "CLI102",
            "{1} method '{0}' cannot be a command",
            "{1}s can't be commands",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdMustBeVoidOrInt
        = new(
            "CLI103",
            "Method '{0}' returns '{1}', which is invalid for a command",
            "Commands must return either void or int",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindRootCmd
        = new(
            "CLI104",
            "Couldn't find entry point method '{0}'",
            "Couldn't find entry point method '{0}'",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindParentCmd
        = new(
            "CLI105",
            "Couldn't find parent method '{1}' for sub-command '{0}'",
            "'{1}' must be the name of a method marked with [Command] or [SubCommand]",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor TooManyRootCmd
        = new(
            "CLI106",
            "Ambiguity between methods '{1}' and '{2}'",
            "More than one method with name '{0}' for entry point",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyCmdName
        = new(
            "CLI107",
            "Null/empty/whitespace-only command names are disallowed",
            "Command names cannot be null, empty or only contain whitespace",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyParentCmdName
        = new(
            "CLI107",
            "Null/empty/whitespace-only parent command names are disallowed",
            "Parent command names cannot be null, empty or only contain whitespace",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );



    public static readonly DiagnosticDescriptor CmdNameAlreadyExists
        = new(
            "CLI108",
            "Another command with the name '{0}' was already declared",
            "Another command with the name '{0}' was already declared",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMethodCantBeGeneric
        = new(
            "CLI200",
            "Method '{0}' can't be generic to be an option",
            "Options can't be generic",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMethodWrongReturnType
        = new(
            "CLI201",
            "Method '{0}' returns type '{1}', which is invalid for options",
            "Options must return either void, bool, int, string or Exception",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMethodTooManyArguments
        = new(
            "CLI202",
            "Method '{0}' has {1} parameters, where one was expected maximum",
            "Option-methods can only have 0 or 1 parameters",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMethodWrongParamType
        = new(
            "CLI203",
            "Parameter '{0}' should have type 'string', not {1}",
            "The parameter of an option-method should always be a string",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMustBeStatic
        = new(
            "CLI204",
            "Member '{0}' has to be static to be an option",
            "Options must be marked static",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptsInEntryMethod
        = new(
            "CLI205",
            "Entry-point methods '{0}' can't have parameters with [Option]",
            "Entry-point methods currently don't allow [Options] parameters, use fields and properties instead",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyOptShortName
        = new(
            "CLI206",
            "Whitespace characters can't be used for option's aliases",
            "Whitespace characters can't be used for option's aliases",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyOptLongName
        = new(
            "CLI207",
            "Null/empty/whitespace-only option names are disallowed",
            "Option names cannot be null, empty or only contain whitespace",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NonWritableOptField
        = new(
            "CLI208",
            "Can't use 'readonly' on option fields",
            "Fields marked with [Option] must be writable",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NonWritableOptProp
        = new(
            "CLI208",
            "An option's property must have a public set accessor",
            "Properties marked with [Option] must have a public set accessor",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptNameAlreadyExists
        = new(
            "CLI209",
            "Another option with the name '{0}' was already declared for this command",
            "Another option with the name '{0}' was already declared for this command",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptAliasAlreadyExists
        = new(
            "CLI209",
            "Another option with the alias '{0}' was already declared for this command",
            "Another option with the alias '{0}' was already declared for this command",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor DescCantBeNull
        = new(
            "CLI300",
            "Description for '{0}' can't be null",
            "Descriptions can't be null",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OnlyDescAttr
        = new(
            "CLI301",
            "Useless [Description] attribute",
            "The [Description] attribute is only useful on commands, options, arguments or CLI classes",
            "Recline.Analysis",
            DiagnosticSeverity.Warning,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindAutoParser
        = new(
            "CLI400",
            "Couldn't find a suitable parsing method for type '{0}'",
            "Type '{0}' can't be automatically parsed from a string. "
            + "Make sure it has either a constructor that has a single string parameter, or matching TryParse or Parse methods",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoGenericAutoParser
        = new(
            "CLI401",
            "Can't auto-parse generic type '{0}'",
            "Auto-parsing generic types is not supported",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindNamedParser
        = new(
            "CLI402",
            "Couldn't find method '{1}.{0}' for parsing",
            "Couldn't find method '{0}' in type '{1}' to parse this option/argument",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NotValidParserType
        = new(
            "CLI403",
            "Parser's containing type '{0}' can't be an array, pointer, or unbound generic type",
            "Parser's containing type '{0}' can't be an array, pointer, or unbound generic type",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoValidParserMethod
        = new(
            "CLI404",
            "No overload for '{0}' can be used as a parsing method here",
            "No overload for '{0}' can be used as a parsing method here",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );
}