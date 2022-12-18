namespace Recline.Generator;

internal static class CommonTypes
{
    internal static INamedTypeSymbol BOOL = null!;
    internal static MinimalTypeInfo BOOLMinInfo = null!;
    internal static INamedTypeSymbol INT32 = null!;
    internal static MinimalTypeInfo INT32MinInfo = null!;
    internal static INamedTypeSymbol CHAR = null!;
    internal static MinimalTypeInfo CHARMinInfo = null!;
    internal static INamedTypeSymbol STR = null!;
    internal static MinimalTypeInfo STRMinInfo = null!;
    internal static INamedTypeSymbol VOID = null!;
    internal static MinimalTypeInfo VOIDMinInfo = null!;
    internal static INamedTypeSymbol EXCEPTION = null!;
    internal static MinimalTypeInfo EXCEPTIONMinInfo = null!;
    internal static INamedTypeSymbol NULLABLE = null!;
    internal static MinimalTypeInfo NULLABLEMinInfo = null!;
    internal static INamedTypeSymbol ENUM = null!;
    internal static MinimalTypeInfo ENUMMinInfo = null!;

    internal static SymbolDisplayFormat memberMinimalDisplayFormat = new(
        parameterOptions: SymbolDisplayParameterOptions.IncludeName,
        //typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        //genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        //memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
    );

    internal static void Clear() {
        BOOL = null!;
        BOOLMinInfo = null!;
        INT32 = null!;
        INT32MinInfo = null!;
        CHAR = null!;
        CHARMinInfo = null!;
        STR = null!;
        STRMinInfo = null!;
        VOID = null!;
        VOIDMinInfo = null!;
        NULLABLE = null!;
        NULLABLEMinInfo = null!;
        EXCEPTION = null!;
        EXCEPTIONMinInfo = null!;
        ENUM = null!;
        ENUMMinInfo = null!;
    }

    [MemberNotNull(
        nameof(BOOL),
        nameof(BOOLMinInfo),
        nameof(INT32),
        nameof(INT32MinInfo),
        nameof(CHAR),
        nameof(CHARMinInfo),
        nameof(STR),
        nameof(STRMinInfo),
        nameof(VOID),
        nameof(VOIDMinInfo),
        nameof(NULLABLE),
        nameof(NULLABLEMinInfo),
        nameof(EXCEPTION),
        nameof(EXCEPTIONMinInfo),
        nameof(ENUM),
        nameof(ENUMMinInfo)
    )]
    internal static void Refresh(Compilation compilation) {
        BOOL = compilation.GetSpecialType(SpecialType.System_Boolean);
        INT32 = compilation.GetSpecialType(SpecialType.System_Int32);
        CHAR = compilation.GetSpecialType(SpecialType.System_Char);
        STR = compilation.GetSpecialType(SpecialType.System_String);
        VOID = compilation.GetSpecialType(SpecialType.System_Void);
        NULLABLE = compilation.GetSpecialType(SpecialType.System_Nullable_T);
        ENUM = compilation.GetSpecialType(SpecialType.System_Enum);

        BOOLMinInfo = MinimalTypeInfo.FromSymbol(BOOL);
        INT32MinInfo = MinimalTypeInfo.FromSymbol(INT32);
        CHARMinInfo = MinimalTypeInfo.FromSymbol(CHAR);
        STRMinInfo = MinimalTypeInfo.FromSymbol(STR);
        VOIDMinInfo = MinimalTypeInfo.FromSymbol(VOID);
        NULLABLEMinInfo = MinimalTypeInfo.FromSymbol(NULLABLE);
        ENUMMinInfo = MinimalTypeInfo.FromSymbol(ENUM);

        EXCEPTION = compilation.GetTypeByMetadataName("System.Exception")!;
        EXCEPTIONMinInfo = MinimalTypeInfo.FromSymbol(EXCEPTION);
    }
}