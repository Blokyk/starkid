namespace Recline.Generator;

internal static class Diagnostics {
    public static readonly DiagnosticDescriptor TimingInfo
        = new(
            "CLI000",
            "{0} took: {1}ms",
            "{0} took: {1}ms",
            "Debug",
            DiagnosticSeverity.Info,
            true
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
            "Method '{0}' can't be generic to be a command",
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

    public static readonly DiagnosticDescriptor OptMustNotBeReadonly
        = new(
            "CLI205",
            "Member '{0}' can't be readonly to be an option",
            "Options can't be marked readonly",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptsInEntryMethod
        = new(
            "CLI206",
            "Entry-point methods '{0}' can't have parameters with [Option]",
            "Entry-point methods currently don't allow [Options] parameters, use fields and properties instead",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyOptShortName
        = new(
            "CLI207",
            "Whitespace characters can't be used for option's aliases",
            "Whitespace characters can't be used for option's aliases",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyOptLongName
        = new(
            "CLI208",
            "Null/empty/whitespace-only option names are disallowed",
            "Option names cannot be null, empty or only contain whitespace",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NonWritableOptField
        = new(
            "CLI209",
            "Can't use 'readonly' on option fields",
            "Fields marked with [Option] must be writable",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NonWritableOptProp
        = new(
            "CLI209",
            "An option's property must have a public set accessor",
            "Properties marked with [Option] must have a public set accessor",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor DescCantBeNull
        = new(
            "CLI300",
            "Description for '{0}' can't be null",
            "Description texts can't be null",
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
}