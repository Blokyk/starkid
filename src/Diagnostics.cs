namespace StarKid.Generator;

internal static class Diagnostics {
    public static readonly DiagnosticDescriptor TimingInfo
        = new(
            "CLI000",
            "{0} took: {1:0}ms ({1:0.000)}",
            "{0} took: {1:0}ms ({1:0.000)}",
            "StarKid.Debug",
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
            "StarKid gave up analyzing invalid code",
            "StarKid gave up analyzing invalid code",
            "StarKid.Analysis",
            DiagnosticSeverity.Hidden,
            false
        );

    public static readonly DiagnosticDescriptor TooManyRootGroups
        = new(
            "CLI001",
            "This assembly defines multiple root groups, which is illegal",
            "Classes '{0}' and '{1}' both declare a root group, because neither of them are nested in a class marked with [CommandGroup], which is illegal",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor BothOptAndCmd
        = new(
            "CLI002",
            "Member '{0}' can't have both [Option] and [Command/SubCommand] attributes",
            "A member can't be both an option and a command",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParseOnNonOptOrArg
        = new(
            "CLI003",
            "[ParseWith] can only be used on options or arguments",
            "[ParseWith] can only be used on options or arguments",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidateOnNonOptOrArg
        = new(
            "CLI003",
            "[ValidateWith] can only be used on options or arguments",
            "[ValidateWith] can only be used on options or arguments",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParamsCantBeOption
        = new(
            "CLI004",
            "Parameter '{0}' cannot be used as options because it's a 'params' argument",
            "Parameter '{0}' cannot be used as options because it's a 'params' argument",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidValueForProjectProperty
        = new(
            "CLI005",
            "Invalid value for property '{0}' in project settings",
            "Invalid value for property '{0}' in project settings",
            "StarKid.Config",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ConfigPropNotVisible
        = new(
            "CLI006",
            "StarKid cannot read configuration/build property '{0}'",
            "Build property '{0}' should be visible to StarKid's source generator. Check that you installed the package correctly.",
            "StarKid.Config",
            DiagnosticSeverity.Warning,
            true
        );

    public static readonly DiagnosticDescriptor DashDashForbiddenName
        = new(
            "CLI007",
            "The name '--' is forbidden for commands and options",
            "The name '--' is forbidden for commands and options",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NameCantStartWithDash
        = new(
            "CLI007",
            "Option/command names can't start with '-'",
            "Option/command names can't start with '-'",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor GroupClassMustNotBeGeneric
        = new(
            "CLI100",
            "Command group classes must not be generic, and neither should any containing type",
            "Command group classes must not be generic, and neither should any containing type",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor GroupClassMustBeStatic
        = new(
            "CLI100",
            "Command group classes must be static (and their containing type as well)",
            "Command group classes must be static (and their containing type as well)",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdCantBeGeneric
        = new(
            "CLI101",
            "Command methods cannot be generic",
            "Command methods cannot be generic",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdMustBeOrdinary
        = new(
            "CLI102",
            "{1} method '{0}' cannot be a command",
            "{1}s can't be commands",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdMustBeVoidOrInt
        = new(
            "CLI103",
            "Method '{0}' returns '{1}', which is invalid for a command",
            "Commands must return either void or int",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindDefaultCmd
        = new(
            "CLI104",
            "Couldn't find a command named '{0}' in this group",
            "Couldn't find a command named '{0}' in this group",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindParentCmd
        = new(
            "CLI105",
            "Couldn't find parent method '{1}' for sub-command '{0}'",
            "'{1}' must be the name of a method marked with [Command] or [SubCommand]",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor TooManyRootCmd
        = new(
            "CLI106",
            "Ambiguity between methods '{1}' and '{2}'",
            "More than one method with name '{0}' for entry point",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyCmdName
        = new(
            "CLI107",
            "Null/empty/whitespace-only command names are disallowed",
            "Command names cannot be null, empty or only contain whitespace",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdNameAlreadyExists
        = new(
            "CLI107",
            "Another command with the name '{0}' was already declared",
            "Another command with the name '{0}' was already declared",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidCmdName
        = new(
            "CLI107",
            "Command names can only contain '-', '_', or ASCII letters/digits",
            "Command names can only contain '-', '_', or ASCII letters/digits",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdNameCantBeginWithDashDash
        = new(
            "CLI107",
            "Command names cannot begin with '--'",
            "Command names cannot begin with '--', as that would clash with options",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor UselessSpecialCmdName
        = new(
            "CLI108",
            "Special name '#' is illegal because the containing group's default command name isn't '#'",
            "Special name '#' is illegal since the containing group's default command name isn't '#', "
                + "therefore this command would never be invoked",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdParamCantBeRef
        = new(
            "CLI109",
            "Command method parameters cannot be marked 'ref', 'out' or 'in'",
            "Command method parameters cannot be marked 'ref', 'out' or 'in'",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CmdParamTypeCantBeRefStruct
        = new(
            "CLI110",
            "A command method cannot have any ref struct as a parameter",
            "A command method cannot have any ref struct as a parameter",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyOptAlias
        = new(
            "CLI200",
            "Whitespace characters can't be used for option's aliases",
            "Whitespace characters can't be used for option's aliases",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidOptAlias
        = new(
            "CLI200",
            "'{0}' is not a valid option alias",
            "Option aliases can only contain '-', '_', or ASCII letters/digits",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor EmptyOptLongName
        = new(
            "CLI200",
            "Null/empty/whitespace-only option names are disallowed",
            "Option names cannot be null, empty or only contain whitespace",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidOptLongName
        = new(
            "CLI200",
            "'{0}' is not a valid option name",
            "Option names can only contain '-', '_', or ASCII letters/digits",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptCantBeNamedHelp
        = new(
            "CLI200",
            "Option can't be named \"--help\" or be aliased to '-h'",
            "Option can't be named \"--help\" or be aliased to '-h'",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptNameCantBeginWithDashDash
        = new(
            "CLI200",
            "Option names shouldn't include '--' at the beginning",
            "Option names shouldn't include '--' at the beginning, just give the name directly (for example: '{0}')",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptNameAlreadyExists
        = new(
            "CLI201",
            "Another option with the name '{0}' was already declared in this command, or in a parent group",
            "Another option with the name '{0}' was already declared in this command, or in a parent group",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor OptAliasAlreadyExists
        = new(
            "CLI201",
            "Another option with the alias '{0}' was already declared in this command, or in a parent group",
            "Another option with the alias '{0}' was already declared in this command, or in a parent group",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NonWritableOptField
        = new(
            "CLI202",
            "Can't use 'readonly' on option fields",
            "Fields marked with [Option] must be writable",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NonWritableOptProp
        = new(
            "CLI202",
            "Properties marked with [Option] must have a internal or public set accessor",
            "Properties marked with [Option] must have a internal or public set accessor",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindAutoParser
        = new(
            "CLI400",
            "Type '{0}' doesn't have any static method named 'Parse' or 'TryParse', or a constructor with a single string parameter",
            "Type '{0}' doesn't have any static method named 'Parse' or 'TryParse', or a constructor with a single string parameter",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoValidAutoParser
        = new(
            "CLI400",
            "None of the 'Parse' or 'TryParse' methods for type '{0}' were valid parsers",
            "None of the 'Parse' or 'TryParse' methods for type '{0}' were valid parsers",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NonAccessibleParser
        = new(
            "CLI400",
            "Constructor or Parse/TryParse method must be internal or public to be used for parsing",
            "Constructor or Parse/TryParse method must be internal or public to be used for parsing",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoGenericAutoParser
        = new(
            "CLI401",
            "Can't auto-parse unbound generic type '{0}'",
            "Auto-parsing generic types is not supported",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoValidParserOverload
        = new(
            "CLI402",
            "No overload for '{0}' can be used as a parsing method here",
            "No overload for '{0}' can be used as a parsing method here",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NotValidParserType
        = new(
            "CLI403",
            "Parser's containing type '{0}' can't be an array, pointer, or unbound generic type",
            "Parser's containing type '{0}' can't be an array, pointer, or unbound generic type",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindNamedParser
        = new(
            "CLI404",
            "Couldn't find a method named '{0}' suitable for parsing",
            "Couldn't find method '{0}', or it wasn't suitable to parse this option/argument",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserCantReturnRef
        = new(
            "CLI405",
            "Parsing methods cannot return by ref or ref readonly",
            "Parsing methods cannot return by ref or ref readonly",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserCantBeGenericMethod
        = new(
            "CLI406",
            "Parsing methods cannot have any type parameters",
            "Parsing methods cannot have any type parameters",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserHasToBeStatic
        = new(
            "CLI407",
            "Parsing methods have to be static",
            "Parsing methods have to be static",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserHasToReturnTargetType
        = new(
            "CLI408",
            "This parser doesn't return a type compatible with '{0}'",
            "There is no implicit conversion from return type '{1}' to target type '{0}'",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserMustTakeStringParam
        = new(
            "CLI409",
            "A parsing method's first parameter must be of type 'string'",
            "A parsing method's first parameter must be of type 'string'",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor InvalidIndirectParserForm
        = new(
            "CLI410",
            "Indirect parsers must be of the form 'bool Foo(string, out T)'",
            "Indirect parsers must be of the form 'bool Foo(string, out T)'",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor IndirectParserWrongTargetType
        = new(
            "CLI411",
            "The out parameter of an indirect parser must be exactly the same as the target type",
            "This out parameter must be of target type '{0}' to be a valid indirect parser",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParamCountWrongForParser
        = new(
            "CLI412",
            "Parsing methods must have either 1 or 2 parameters",
            "Method '{0}' needs either 1 or 2 parameters to be a parser, but has '{1}' instead",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParserParamWrongRefKind
        = new(
            "CLI413",
            "A parser input (string) parameter cannot be marked 'ref', 'out' or 'in'",
            "A parser input (string) parameter cannot be marked 'ref', 'out' or 'in'",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ParseWithMustBeNameOfExpr
        = new(
            "CLI499",
            "The parameter to a ParseWith attribute must be a nameof expression",
            "Expression '{0}' can't be used as a parameter to a ParseWith attribute; "
            + "it must be a nameof expression",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor CouldntFindValidator
        = new(
            "CLI500",
            "Couldn't find a method named '{0}' suitable for validation",
            "Couldn't find any method named '{0}' suitable to validate this option/argument",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor UnvalidatableType
        = new(
            "CLI501",
            "Can't use a validator with an array, pointer, or unbound generic type",
            "Can't use a validator with an array, pointer, or unbound generic type",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor PropertyValidatorNotOnArgType
        = new(
            "CLI501",
            "Property '{0}' must be a member of '{1}' to be used for validation",
            "Property '{0}' must be a member of '{1}' to be used for validation",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor NoValidValidatorMethod
        = new(
            "CLI502",
            "No overload for method '{0}' can be used as a validator for type '{1}'",
            "No overload for method '{0}' can be used as a validator for type '{1}'",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidatorReturnMismatch
        = new(
            "CLI503",
            "Validator method '{0}' must return bool or void",
            "Validator method '{0}' must return bool or void",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidatorPropertyReturnMismatch
        = new(
            "CLI503",
            "Property '{0}' must be of type bool to be used for validation",
            "Property '{0}' must be of type bool to be used for validation",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidatorMustBeStatic
        = new(
            "CLI504",
            "Validator methods must be static",
            "Method '{0}' must be static to be used as a validator",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidatorWrongParameter
        = new(
            "CLI505",
            "Validator methods for this value must take a single parameter of type '{0}'",
            "Validator methods for this value must take a single parameter of type '{0}'",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );

    public static readonly DiagnosticDescriptor ValidateWithMustBeNameOfExpr
        = new(
            "CLI599",
            "The parameter to a ValidateWith attribute must be a nameof expression",
            "Expression '{0}' can't be used as a parameter to a ValidateWith attribute; "
            + "it must be a nameof expression",
            "StarKid.Analysis",
            DiagnosticSeverity.Error,
            true
        );
}