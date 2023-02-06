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

    public static readonly DiagnosticDescriptor TooManyRootGroups
        = new(
            "CLI001",
            "This assembly defines multiple root groups, which is illegal",
            "Classes '{0}' and '{1}' both declare a root group, because neither of them are nested in a class marked with [CommandGroup], which is illegal",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor BothOptAndCmd
        = new(
            "CLI002",
            "Member '{0}' can't have both [Option] and [Command/SubCommand] attributes",
            "A member can't be both an option and a command",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParseOnNonOptOrArg
        = new(
            "CLI003",
            "[ParseWith] can only be used on options or arguments",
            "[ParseWith] can only be used on options or arguments",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidateOnNonOptOrArg
        = new(
            "CLI003",
            "[ValidateWith] can only be used on options or arguments",
            "[ValidateWith] can only be used on options or arguments",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParamsCantBeParsed
        = new(
            "CLI004",
            "[ParseWith] cannot be used on params arguments",
            "[ParseWith] cannot be used on params arguments",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParamsCantBeValidated
        = new(
            "CLI004",
            "[ValidateWith] cannot be used on params arguments",
            "[ValidateWith] cannot be used on params arguments",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParamsCantBeOption
        = new(
            "CLI005",
            "Parameter '{0}' cannot be used as options because it's a 'params' argument",
            "Parameter '{0}' cannot be used as options because it's a 'params' argument",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParamsHasToBeString
        = new(
            "CLI006",
            "Can't use type '{0}' for on params argument for commands",
            "'params' arguments for commands must be of type string[]",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidValueForProjectProperty
        = new(
            "CLI007",
            "Invalid value for property '{0}' in project settings",
            "Invalid value for property '{0}' in project settings",
            "Recline.Config",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor GroupClassMustBeStatic
        = new(
            "CLI100",
            "Class '{0}' must be static to be a [CommandGroup]",
            "Command groups must be static",
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

    public static readonly DiagnosticDescriptor CouldntFindDefaultCmd
        = new(
            "CLI104",
            "Couldn't find a command named '{0}' in this group",
            "Couldn't find a command named '{0}' in this group",
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

    public static readonly DiagnosticDescriptor CmdNameAlreadyExists
        = new(
            "CLI107",
            "Another command with the name '{0}' was already declared",
            "Another command with the name '{0}' was already declared",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidCmdName
        = new(
            "CLI107",
            "Command names can only contain '-', '_', or ASCII letters/digits",
            "Command names can only contain '-', '_', or ASCII letters/digits",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor UselessSpecialCmdName
        = new(
            "CLI108",
            "Special name '#' is illegal because the containing group's default command name isn't '#'",
            "Special name '#' is illegal since the containing group's default command name isn't '#', "
                + "therefore this command would never be invoked",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyOptAlias
        = new(
            "CLI200",
            "Whitespace characters can't be used for option's aliases",
            "Whitespace characters can't be used for option's aliases",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidOptAlias
        = new(
            "CLI200",
            "'{0}' is not a valid option alias",
            "Option aliases can only contain '-', '_', or ASCII letters/digits",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyOptLongName
        = new(
            "CLI200",
            "Null/empty/whitespace-only option names are disallowed",
            "Option names cannot be null, empty or only contain whitespace",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidOptLongName
        = new(
            "CLI200",
            "'{0}' is not a valid option name",
            "Option names can only contain '-', '_', or ASCII letters/digits",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptCantBeNamedHelp
        = new(
            "CLI200",
            "Option can't be named \"--help\" or be aliased to '-h'.",
            "Option can't be named \"--help\" or be aliased to '-h'.",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptNameAlreadyExists
        = new(
            "CLI201",
            "Another option with the name '{0}' was already declared in this command, or in a parent group",
            "Another option with the name '{0}' was already declared in this command, or in a parent group",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptAliasAlreadyExists
        = new(
            "CLI201",
            "Another option with the alias '{0}' was already declared in this command, or in a parent group",
            "Another option with the alias '{0}' was already declared in this command, or in a parent group",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NonWritableOptField
        = new(
            "CLI202",
            "Can't use 'readonly' on option fields",
            "Fields marked with [Option] must be writable",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NonWritableOptProp
        = new(
            "CLI202",
            "Properties marked with [Option] must have a public set accessor",
            "Properties marked with [Option] must have a public set accessor",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
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
            "Couldn't find suitable method '{0}' for parsing",
            "Couldn't find method '{0}', or it wasn't suitable to parse this option/argument",
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

    public static readonly DiagnosticDescriptor ParseWithMustBeNameOfExpr
        = new(
            "CLI405",
            "The parameter to a ParseWith attribute must be a nameof expression",
            "Expression '{0}' can't be used as a parameter to a ParseWith attribute; "
            + "it must be a nameof expression",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindValidator
        = new(
            "CLI500",
            "Couldn't find suitable a method named '{0}' for validation",
            "Couldn't find any method named '{0}' suitable to validate this option/argument",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NotValidValidatorType
        = new(
            "CLI501",
            "Validator's containing type '{0}' can't be an array, pointer, or unbound generic type",
            "Validator's containing type '{0}' can't be an array, pointer, or unbound generic type",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor PropertyValidatorNotOnArgType
        = new(
            "CLI501",
            "Property '{0}' must be a member of '{1}' to be used for validation",
            "Property '{0}' must be a member of '{1}' to be used for validation",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoValidValidatorMethod
        = new(
            "CLI502",
            "No overload for method '{0}' can be used as a validator for type '{1}'",
            "No overload for method '{0}' can be used as a validator for type '{1}'",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidatorReturnMismatch
        = new(
            "CLI503",
            "Validator method '{0}' must return bool, string? or Exception?",
            "Validator method '{0}' must return bool, string? or Exception?",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidatorPropertyReturnMismatch
        = new(
            "CLI503",
            "Property '{0}' must be of type bool to be used for validation",
            "Property '{0}' must be of type bool to be used for validation",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidatorMustBeStatic
        = new(
            "CLI504",
            "Validator method '{0}' must be static",
            "Method '{0}' must be static to be used as a validator",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidateWithMustBeNameOfExpr
        = new(
            "CLI505",
            "The parameter to a ValidateWith attribute must be a nameof expression",
            "Expression '{0}' can't be used as a parameter to a ValidateWith attribute; "
            + "it must be a nameof expression",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );
}