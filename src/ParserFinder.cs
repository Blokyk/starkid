using StarKid.Generator.AttributeModel;
using StarKid.Generator.CommandModel;
using StarKid.Generator.SymbolModel;

namespace StarKid.Generator;

public class ParserFinder
{
    private readonly Action<Diagnostic> addDiagnostic;

    private readonly Cache<ITypeSymbol, ITypeSymbol, bool> _implicitConversionsCache;
    private readonly TypeCache<ParserInfo> _typeParserCache;

    private readonly Compilation _compilation;

    // todo: convert this to an array and lookup by casting to numeric underlying type
    private static readonly Dictionary<SpecialType, ParserInfo> _specialTypesMap
        = new() { {
                SpecialType.System_String,
                ParserInfo.StringIdentity.Instance
            }, {
                SpecialType.System_Boolean,
                ParserInfo.AsBool.Instance
            }, {
                SpecialType.System_Object,
                ParserInfo.StringIdentity.Instance
            }, {
                SpecialType.System_Char,
                new ParserInfo.DirectMethod(
                    "System.Char.Parse",
                    CommonTypes.CHAR
                )
            }, {
                SpecialType.System_Int32,
                new ParserInfo.DirectMethod(
                    "System.Int32.Parse",
                    CommonTypes.INT32
                )
            }, {
                SpecialType.System_Double,
                new ParserInfo.BoolOutMethod(
                    "System.Double.TryParse",
                    CommonTypes.DOUBLE
                )
            }, {
                SpecialType.System_Single,
                new ParserInfo.DirectMethod(
                    "System.Single.Parse",
                    CommonTypes.SINGLE
                )
            }, {
                SpecialType.System_DateTime,
                new ParserInfo.DirectMethod(
                    "System.DateTime.Parse",
                    CommonTypes.DATE_TIME
                )
            }
        };

    public ParserFinder(Action<Diagnostic> addDiagnostic, Compilation compilation) {
        this.addDiagnostic = addDiagnostic;

        _compilation = compilation;

        _typeParserCache = new(
            t => t is INamedTypeSymbol target
                ? FindParserForTypeCore(target)
                : InvalidParserType(t),
            _specialTypesMap
        );

        _implicitConversionsCache = new(
            SymbolEqualityComparer.Default,
            SymbolEqualityComparer.Default,
            _compilation.HasImplicitConversion
        );
    }

    public bool TryGetParserFromName(ParseWithAttribute attr, ITypeSymbol targetType, bool isOption, out ParserInfo parser) {
        parser = GetParserFromNameCore(attr, targetType);

        if (parser is not ParserInfo.Invalid invalidParser)
            return true;

        // if this is an array option, the parser might be for items instead of the array
        if (targetType is IArrayTypeSymbol { ElementType: var itemType }) {
            if (isOption) {
                parser = GetParserFromNameCore(attr, itemType);

                if (parser is not ParserInfo.Invalid invalidItemParser)
                    return true;

                invalidParser = invalidItemParser;
            }
        }

        addDiagnostic(
            Diagnostic.Create(
                invalidParser.Descriptor,
                attr.ParserNameExpr.GetLocation(),
                invalidParser.MessageArgs
            )
        );

        return false;
    }

    ParserInfo GetParserFromNameCore(ParseWithAttribute attr, ITypeSymbol targetType) {
        var members
            = _compilation
                .GetMemberGroup(attr.ParserNameExpr)
                .OfType<IMethodSymbol>()
                .ToArray();

        ParserInfo? parserInfo = null;

        foreach (var method in members) {
            if (method.MethodKind != MethodKind.Ordinary) {
                return new ParserInfo.Invalid(
                    Diagnostics.CouldntFindNamedParser,
                    attr.ParserNameExpr
                );
            }

            if (TryGetParserInfo(method, targetType, out parserInfo))
                return parserInfo;
        }

        if (members.Length == 1)
            return parserInfo!;

        // if there's more than one methods, better to just say none of them fit
        // rather than spit out just the reason the last candidate was rejected
        if (members.Length > 1) {
            return new ParserInfo.Invalid(
                Diagnostics.NoValidParserOverload,
                attr.ParserNameExpr
            );
        }

        return new ParserInfo.Invalid(
            Diagnostics.CouldntFindNamedParser,
            attr.ParserNameExpr
        );
    }

    public bool TryFindParserForType(ITypeSymbol targetType, Location queryLocation, out ParserInfo parser) {
        parser = FindParserForType(targetType);

        if (parser is not ParserInfo.Invalid invalidParser)
            return true;

        addDiagnostic(
            Diagnostic.Create(
                invalidParser.Descriptor,
                queryLocation,
                invalidParser.MessageArgs
            )
        );

        return false;
    }

    private static ParserInfo.Invalid InvalidParserType(ITypeSymbol type)
        => new(Diagnostics.NotValidParserType, type);

    ParserInfo FindParserForType(ITypeSymbol t)
        => t is INamedTypeSymbol targetType
            ? _typeParserCache.GetValue(targetType)
            : InvalidParserType(t);

    ParserInfo FindParserForTypeCore(INamedTypeSymbol sourceType) {
        if (sourceType.SpecialType == SpecialType.System_String)
            return ParserInfo.StringIdentity.Instance;

        // this will only be true for nullable *value types*, contrary to SymbolUtils.IsNullable
        if (sourceType.ConstructedFrom.SpecialType is SpecialType.System_Nullable_T) {
            return FindParserForType(sourceType.TypeArguments[0]);
        }

        // * too costly for now
        // if (_implicitConversionsCache.GetValue(CommonTypes.STR, sourceType))
        //     return new ParserInfo.Identity(MinimalTypeInfo.FromSymbol(sourceType));

        if (sourceType.IsUnboundGenericType) {
            return new ParserInfo.Invalid(Diagnostics.GiveUp);
        }

        if (sourceType.EnumUnderlyingType is not null) {
            var minTypeInfo = MinimalTypeInfo.FromSymbol(sourceType);

            return new ParserInfo.DirectMethod(
                "WrapParseEnum<" + minTypeInfo.FullName + ">",
                minTypeInfo
            );
        }

        ParserInfo? parserInfo = null;

        foreach (var ctor in sourceType.Constructors) {
            if (TryGetCtorParserInfo(ctor, sourceType, out parserInfo))
                return parserInfo;
        }

        // todo(#7): support extension methods

        // if we didn't find a suitable constructor, try to find Parse(string) or TryParse(string, out $target)

        int candidateCount = 0;

        var directMembers = sourceType.GetMembers("Parse").OfType<IMethodSymbol>();
        foreach (var method in directMembers) {
            candidateCount++;
            if (TryGetDirectParserInfo(method, sourceType, out parserInfo))
                return parserInfo;
        }

        var boolOutMembers = sourceType.GetMembers("TryParse").OfType<IMethodSymbol>();
        foreach (var method in boolOutMembers) {
            candidateCount++;
            if (TryGetBoolOutParserInfo(method, sourceType, out parserInfo))
                return parserInfo;
        }

        switch (candidateCount) {
            case 0:
                return new ParserInfo.Invalid(Diagnostics.CouldntFindAutoParser, sourceType.GetErrorName());
            case 1:
                return parserInfo!; // notnull: parserInfo gets set at the same time as candidateCount gets incremented
            default:
                return new ParserInfo.Invalid(Diagnostics.NoValidAutoParser, sourceType.GetErrorName());
        }
    }

    bool CouldBeValidParser(IMethodSymbol method, [NotNullWhen(false)] out ParserInfo? reason) {
        reason = null;

        // todo: add checks like bound generics, etc

        if (method.Parameters.Length != 0) {
            var inputParam = method.Parameters[0];

            if (inputParam.RefKind != RefKind.None) {
                reason = new ParserInfo.Invalid(Diagnostics.ParserParamWrongRefKind, inputParam.RefKind);
                return false;
            }

            if (!SymbolUtils.IsStringLike(inputParam.Type)) {
                reason = new ParserInfo.Invalid(Diagnostics.ParserMustTakeStringParam);
                return false;
            }
        }

        if (method.DeclaredAccessibility < Accessibility.Internal) {
            reason = new ParserInfo.Invalid(Diagnostics.NonAccessibleParser);
            return false;
        }

        // if this is a ctor, the next conditions don't apply
        if (method.MethodKind is MethodKind.Constructor)
            return true;

        if (!method.IsStatic) {
            reason = new ParserInfo.Invalid(Diagnostics.ParserHasToBeStatic);
            return false;
        }

        // note: when we refactor to lift the restriction on generic methods,
        // also change the Enum.Parse code in FindParserForType
        if (method.IsGenericMethod) {
            reason = new ParserInfo.Invalid(Diagnostics.ParserCantBeGenericMethod);
            return false;
        }

        if (method.ReturnsByRef || method.ReturnsByRefReadonly) {
            reason = new ParserInfo.Invalid(Diagnostics.ParserCantReturnRef);
            return false;
        }

        return true;
    }

    bool TryGetDirectParserInfo(IMethodSymbol method, ITypeSymbol targetType, out ParserInfo parser) {
        if (method.Parameters.Length != 1) {
            parser = new ParserInfo.Invalid(Diagnostics.ParamCountWrongForParser);
            return false;
        }

        if (!CouldBeValidParser(method, out parser!)) // notnull: if CouldBeValidParser is false, parser won't be false
            return false;

        var isReturnTypeTarget = _implicitConversionsCache.GetValue(method.ReturnType, targetType);

        if (!isReturnTypeTarget) {
            parser = new ParserInfo.Invalid(
                Diagnostics.ParserHasToReturnTargetType,
                targetType.GetErrorName(),
                method.ReturnType.GetErrorName()
            );
            return false;
        }

        var targetTypeInfo = MinimalTypeInfo.FromSymbol(targetType);

        var containingTypeFullName = SymbolInfoCache.GetFullTypeName(method.ContainingType);

        parser = new ParserInfo.DirectMethod(containingTypeFullName + "." + method.Name, targetTypeInfo);
        return true;
    }

    bool TryGetCtorParserInfo(IMethodSymbol ctor, ITypeSymbol targetType, out ParserInfo parser) {
        if (ctor.Parameters.Length != 1) {
            parser = new ParserInfo.Invalid(Diagnostics.CouldntFindAutoParser);
            return false;
        }

        if (!CouldBeValidParser(ctor, out parser!)) // notnull: if CouldBeValidParser is false, parser won't be false
            return false;

        var isReturnTypeTarget
            = _implicitConversionsCache.GetValue(ctor.ContainingType, targetType);

        if (!isReturnTypeTarget) {
            parser = new ParserInfo.Invalid(
                Diagnostics.ParserHasToReturnTargetType,
                targetType.GetErrorName(),
                ctor.ContainingType.GetErrorName()
            );
            return false;
        }

        var targetTypeInfo = MinimalTypeInfo.FromSymbol(targetType);

        parser = new ParserInfo.Constructor(targetTypeInfo);
        return true;
    }

    bool TryGetBoolOutParserInfo(IMethodSymbol method, ITypeSymbol targetType, out ParserInfo parser) {
        if (method.Parameters.Length != 2) {
            parser = new ParserInfo.Invalid(Diagnostics.ParamCountWrongForParser);
            return false;
        }

        if (!CouldBeValidParser(method, out parser!)) // notnull: if CouldBeValidParser is false, parser won't be false
            return false;

        // the return type should be exactly bool
        if (method.ReturnType.SpecialType != SpecialType.System_Boolean) {
            parser = new ParserInfo.Invalid(Diagnostics.InvalidIndirectParserForm);
            return false;
        }

        var outParam = method.Parameters[1];

        // the second parameter should always be out
        if (outParam.RefKind != RefKind.Out) {
            parser = new ParserInfo.Invalid(Diagnostics.InvalidIndirectParserForm);
            return false;
        }

        // and its type should always be the same as the target type (out parameters can't be co- or contra- variant)
        if (!SymbolUtils.Equals(outParam.Type, targetType)) {
            parser = new ParserInfo.Invalid(
                Diagnostics.IndirectParserWrongTargetType,
                targetType.GetErrorName()
            );
            return false;
        }

        var containingTypeFullName = SymbolInfoCache.GetFullTypeName(method.ContainingType);

        parser = new ParserInfo.BoolOutMethod(
            containingTypeFullName + "." + method.Name,
            MinimalTypeInfo.FromSymbol(targetType)
        );

        return true;
    }

    bool TryGetParserInfo(IMethodSymbol method, ITypeSymbol targetType, out ParserInfo parser) {
        switch (method.Parameters.Length) {
            case 1:
                if (method.MethodKind is MethodKind.Constructor)
                    return TryGetCtorParserInfo(method, targetType, out parser);
                else
                    return TryGetDirectParserInfo(method, targetType, out parser);
            case 2:
                return TryGetBoolOutParserInfo(method, targetType, out parser);
            default:
                parser = new ParserInfo.Invalid(
                    method.MethodKind is MethodKind.Constructor
                        ? Diagnostics.CouldntFindAutoParser
                        : Diagnostics.ParamCountWrongForParser,
                    method.GetErrorName(),
                    method.Parameters.Length
                );
                return false;
        }
    }
}