namespace Recline.Generator;

internal static class Diagnostics {
    public static readonly DiagnosticDescriptor TimingInfo
        = new(
            "CLI000",
            "{0} took: {1:0}ms ({1:0.000)}",
            "{0} took: {1:0}ms ({1:0.000)}",
            "Recline.Debug",
            DiagnosticSeverity.Info,
#if DEBUG
            true
#else
            false
#endif
        );

    public static readonly DiagnosticDescriptor GiveUp
        = new(
            "CLI000",
            "Recline gave up analyzing invalid code",
            "Recline gave up analyzing invalid code",
            "Recline.Analysis",
            DiagnosticSeverity.Hidden,
            false
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
            "Can't use type '{0}' for params argument for commands",
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

    public static readonly DiagnosticDescriptor GroupClassMustNotBeGeneric
        = new(
            "CLI100",
            "Command group classes must not be generic, and neither should any containing type",
            "Command group classes must not be generic, and neither should any containing type",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor GroupClassMustBeStatic
        = new(
            "CLI100",
            "Command group classes must be static (and their containing type as well)",
            "Command group classes must be static (and their containing type as well)",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdCantBeGeneric
        = new(
            "CLI101",
            "Command methods cannot be generic",
            "Command methods cannot be generic",
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
            "Type '{0}' doesn't have any static method named 'Parse' or 'TryParse', or a constructor with a single string parameter",
            "Type '{0}' doesn't have any static method named 'Parse' or 'TryParse', or a constructor with a single string parameter",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoValidAutoParser
        = new(
            "CLI400",
            "None of the 'Parse' or 'TryParse' methods for type '{0}' were valid parsers",
            "None of the 'Parse' or 'TryParse' methods for type '{0}' were valid parsers",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoGenericAutoParser
        = new(
            "CLI401",
            "Can't auto-parse unbound generic type '{0}'",
            "Auto-parsing generic types is not supported",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoValidParserOverload
        = new(
            "CLI402",
            "No overload for '{0}' can be used as a parsing method here",
            "No overload for '{0}' can be used as a parsing method here",
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

    public static readonly DiagnosticDescriptor CouldntFindNamedParser
        = new(
            "CLI404",
            "Couldn't find a method named '{0}' suitable for parsing",
            "Couldn't find method '{0}', or it wasn't suitable to parse this option/argument",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserCantReturnRef
        = new(
            "CLI405",
            "Parsing methods cannot return by ref or ref readonly",
            "Parsing methods cannot return by ref or ref readonly",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserCantBeGenericMethod
        = new(
            "CLI406",
            "Parsing methods cannot have any type parameters",
            "Parsing methods cannot have any type parameters",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserHasToBeStatic
        = new(
            "CLI407",
            "Parsing methods have to be static",
            "Parsing methods have to be static",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserHasToReturnTargetType
        = new(
            "CLI408",
            "This parser doesn't return a type compatible with '{0}'",
            "There is no implicit conversion from return type '{1}' to target type '{0}'",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserMustTakeStringParam
        = new(
            "CLI409",
            "A parsing method's first parameter must be of type 'string'",
            "A parsing method's first parameter must be of type 'string'",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidIndirectParserForm
        = new(
            "CLI410",
            "Indirect parsers must be of the form 'bool Foo(string, out T)'",
            "Indirect parsers must be of the form 'bool Foo(string, out T)'",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor IndirectParserWrongTargetType
        = new(
            "CLI411",
            "The out parameter of an indirect parser must be exactly the same as the target type",
            "This out parameter must be of target type '{0}' to be a valid indirect parser",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParamCountWrongForParser
        = new(
            "CLI412",
            "Parsing methods must have either 1 or 2 parameters",
            "Method '{0}' needs either 1 or 2 parameters to be a parser, but has '{1}' instead",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParseWithMustBeNameOfExpr
        = new(
            "CLI499",
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
            "Couldn't find a method named '{0}' suitable for validation",
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
            "Validator method '{0}' must return bool or void",
            "Validator method '{0}' must return bool or void",
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
            "Validator methods must be static",
            "Method '{0}' must be static to be used as a validator",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidatorWrongParameter
        = new(
            "CLI505",
            "Validator methods must take a single parameter of type '{0}'",
            "Validator methods must take a single parameter of type '{0}'",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidateWithMustBeNameOfExpr
        = new(
            "CLI599",
            "The parameter to a ValidateWith attribute must be a nameof expression",
            "Expression '{0}' can't be used as a parameter to a ValidateWith attribute; "
            + "it must be a nameof expression",
            "Recline.Analysis",
            DiagnosticSeverity.Error,
            true
        );
}