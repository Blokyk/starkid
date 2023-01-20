using Recline.Generator.Model;

namespace Recline.Generator;

internal static class CommonTypes
{
    [MemberNotNullWhen(
        true,
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
        nameof(ENUMMinInfo),
        nameof(DOUBLE),
        nameof(DOUBLEMinInfo),
        nameof(SINGLE),
        nameof(SINGLEMinInfo),
        nameof(DATE_TIME),
        nameof(DATE_TIMEMinInfo)
    )]
    private static bool IsInitialized { get; set; } = false;

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
    internal static INamedTypeSymbol DOUBLE = null!;
    internal static MinimalTypeInfo DOUBLEMinInfo = null!;
    internal static INamedTypeSymbol SINGLE = null!;
    internal static MinimalTypeInfo SINGLEMinInfo = null!;
    internal static INamedTypeSymbol DATE_TIME = null!;
    internal static MinimalTypeInfo DATE_TIMEMinInfo = null!;

    internal static SymbolDisplayFormat memberMinimalDisplayFormat = new(
        parameterOptions: SymbolDisplayParameterOptions.IncludeName,
        //typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        //genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        //memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
    );

    internal static void Reset() {
        IsInitialized = false;
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
        DOUBLE = null!;
        DOUBLEMinInfo = null!;
        SINGLE = null!;
        SINGLEMinInfo = null!;
        DATE_TIME = null!;
        DATE_TIMEMinInfo = null!;
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
        nameof(ENUMMinInfo),
        nameof(DOUBLE),
        nameof(DOUBLEMinInfo),
        nameof(SINGLE),
        nameof(SINGLEMinInfo),
        nameof(DATE_TIME),
        nameof(DATE_TIMEMinInfo)
    )]
    internal static void Refresh(Compilation compilation, bool force = false) {
        if (IsInitialized && !force)
            return;

        BOOL = compilation.GetSpecialType(SpecialType.System_Boolean);
        INT32 = compilation.GetSpecialType(SpecialType.System_Int32);
        CHAR = compilation.GetSpecialType(SpecialType.System_Char);
        STR = compilation.GetSpecialType(SpecialType.System_String);
        VOID = compilation.GetSpecialType(SpecialType.System_Void);
        NULLABLE = compilation.GetSpecialType(SpecialType.System_Nullable_T);
        ENUM = compilation.GetSpecialType(SpecialType.System_Enum);
        DOUBLE = compilation.GetSpecialType(SpecialType.System_Double);
        SINGLE = compilation.GetSpecialType(SpecialType.System_Single);
        DATE_TIME = compilation.GetSpecialType(SpecialType.System_DateTime);

        BOOLMinInfo = MinimalTypeInfo.FromSymbol(BOOL);
        INT32MinInfo = MinimalTypeInfo.FromSymbol(INT32);
        CHARMinInfo = MinimalTypeInfo.FromSymbol(CHAR);
        STRMinInfo = MinimalTypeInfo.FromSymbol(STR);
        VOIDMinInfo = MinimalTypeInfo.FromSymbol(VOID);
        NULLABLEMinInfo = MinimalTypeInfo.FromSymbol(NULLABLE);
        ENUMMinInfo = MinimalTypeInfo.FromSymbol(ENUM);
        DOUBLEMinInfo = MinimalTypeInfo.FromSymbol(DOUBLE);
        SINGLEMinInfo = MinimalTypeInfo.FromSymbol(SINGLE);
        DATE_TIMEMinInfo = MinimalTypeInfo.FromSymbol(DATE_TIME);

        EXCEPTION = compilation.GetTypeByMetadataName("System.Exception")!;
        EXCEPTIONMinInfo = MinimalTypeInfo.FromSymbol(EXCEPTION);

        IsInitialized = true;
    }
}