using StarKid.Generator.Model;

namespace StarKid.Generator;

internal static class CommonTypes
{
    static CommonTypes() {
    }

    internal static MinimalTypeInfo BOOL = new(
        "Boolean",
        null,
        "System.Boolean",
        false,
        Location.None
    ) { SpecialType = SpecialType.System_Boolean };
    internal static MinimalTypeInfo INT32 = new(
        "Int32",
        null,
        "System.Int32",
        false,
        Location.None
    ) { SpecialType = SpecialType.System_Int32 };
    internal static MinimalTypeInfo CHAR = new(
        "Char",
        null,
        "System.Char",
        false,
        Location.None
    ) { SpecialType = SpecialType.System_Char };
    internal static MinimalTypeInfo STR = new(
        "String",
        null,
        "System.String",
        false,
        Location.None
    ) { SpecialType = SpecialType.System_String };
    internal static MinimalTypeInfo VOID = new(
        "void",
        null,
        "void", // can't be referred to by System.Void
        false,
        Location.None
    ) { SpecialType = SpecialType.System_Void };
    internal static MinimalTypeInfo EXCEPTION = new(
        "Exception",
        null,
        "System.Exception",
        false,
        Location.None
    ) { SpecialType = SpecialType.None };
    internal static MinimalTypeInfo NULLABLE = new(
        "Nullable<T>",
        null,
        "System.Nullable<T>",
        true,
        Location.None
    ) { SpecialType = SpecialType.System_Nullable_T };
    internal static MinimalTypeInfo ENUM = new(
        "Enum",
        null,
        "System.Enum",
        false,
        Location.None
    ) { SpecialType = SpecialType.System_Enum };
    internal static MinimalTypeInfo DOUBLE = new(
        "Double",
        null,
        "System.Double",
        false,
        Location.None
    ) { SpecialType = SpecialType.System_Double };
    internal static MinimalTypeInfo SINGLE = new(
        "Single",
        null,
        "System.Single",
        false,
        Location.None
    ) { SpecialType = SpecialType.System_Single };
    internal static MinimalTypeInfo DATE_TIME = new(
        "DateTime",
        null,
        "System.DateTime",
        false,
        Location.None
    ) { SpecialType = SpecialType.System_DateTime };

    internal static SymbolDisplayFormat memberMinimalDisplayFormat = new(
        parameterOptions: SymbolDisplayParameterOptions.IncludeName,
        //typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        //genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        //memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
    );
}