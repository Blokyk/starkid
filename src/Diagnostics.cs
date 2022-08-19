namespace Recline.Generator;

internal static class Diagnostics {
    public static readonly DiagnosticDescriptor TimingInfo
        = new DiagnosticDescriptor(
            "CLI000",
            "{0} took: {1}ms",
            "{0} took: {1}ms",
            "Debug",
            DiagnosticSeverity.Info,
            true
        );

    public static readonly DiagnosticDescriptor TooManyCLIClasses
        = new DiagnosticDescriptor(
            "CLI001",
            "Only one class may be marked with [CLI]",
            "Both classes '{0}' and '{1}' are marked with [CLI]",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdMustBeStatic
        = new DiagnosticDescriptor(
            "CLI010",
            "Method '{0}' must be static to be a command",
            "Methods marked with [Command] must be static",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdCantBeGeneric
        = new DiagnosticDescriptor(
            "CLI011",
            "Method '{0}' can't be generic to be a command",
            "Methods marked with [Command] can't be generic",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdMustBeOrdinary
        = new DiagnosticDescriptor(
            "CLI012",
            "{1} method '{0}' cannot be a command",
            "{1}s can't be commands",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdMustBeVoidOrInt
        = new DiagnosticDescriptor(
            "CLI013",
            "Method '{0}' returns '{1}', which is invalid for a command",
            "Commands must return either void or int",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindRootCmd
        = new DiagnosticDescriptor(
            "CLI014",
            "Couldn't find entry point method '{0}'",
            "Couldn't find entry point method '{0}'",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindParentCmd
        = new DiagnosticDescriptor(
            "CLI015",
            "Couldn't find parent method '{1}' for sub-command '{0}'",
            "'{1}' must be the name of a method marked with [Command] or [SubCommand]",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor TooManyRootCmd
        = new DiagnosticDescriptor(
            "CLI016",
            "Ambiguity between methods '{1}' and '{2}'",
            "More than one method with name '{0}' for entry point",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptsInEntryMethod
        = new DiagnosticDescriptor(
            "CLI017",
            "Entry-point methods '{0}' can't have parameters with [Option]",
            "Entry-point methods currently don't allow [Options] parameters, use fields and properties instead",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor BothCmdAndSubCmd
        = new DiagnosticDescriptor(
            "CLI100",
            "Member '{0}' can't have both [Command] and [SubCommand] attributes",
            "A member can't be both an command and a sub-command",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor BothOptAndCmd
        = new DiagnosticDescriptor(
            "CLI101",
            "Member '{0}' can't have both [Option] and [Command/SubCommand] attributes",
            "A member can't be both an option and a command",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMethodCantBeGeneric
        = new DiagnosticDescriptor(
            "CLI020",
            "Method '{0}' can't be generic to be an option",
            "Options can't be generic",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMethodWrongReturnType
        = new DiagnosticDescriptor(
            "CLI021",
            "Method '{0}' returns type '{1}', which is invalid for options",
            "Options must return either void, bool, int, string or Exception",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMethodTooManyArguments
        = new DiagnosticDescriptor(
            "CLI022",
            "Method '{0}' has {1} parameters, where one was expected maximum",
            "Option-methods can only have 0 or 1 parameters",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMethodWrongParamType
        = new DiagnosticDescriptor(
            "CLI023",
            "Parameter '{0}' should have type 'string', not {1}",
            "The parameter of an option-method should always be a string",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMustBeStatic
        = new DiagnosticDescriptor(
            "CLI024",
            "Member '{0}' has to be static to be an option",
            "Options must be marked static",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptMustNotBeReadonly
        = new DiagnosticDescriptor(
            "CLI025",
            "Member '{0}' can't be readonly to be an option",
            "Options can't be marked readonly",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor DescCantBeNull
        = new DiagnosticDescriptor(
            "CLI030",
            "Description for '{0}' can't be null",
            "Description texts can't be null",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );
}