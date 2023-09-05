using Recline.Generator.Model;
using System.Diagnostics;

namespace Recline.Generator;

public class ParserFinder
{
    private readonly Action<Diagnostic> addDiagnostic;

    private readonly Cache<ITypeSymbol, ITypeSymbol, bool> _implicitConversionsCache;
    private readonly TypeCache<ParserInfo> _typeParserCache;
    private readonly Cache<ParseWithAttribute, ITypeSymbol, ParserInfo> _attrParserCache;

    private readonly SemanticModel _model;

    // todo: convert this to an array and lookup by casting to numeric underlying type
    private static readonly Dictionary<SpecialType, ParserInfo> _specialTypesMap
        = new() { {
                SpecialType.System_String,
                ParserInfo.StringIdentity
            }, {
                SpecialType.System_Boolean,
                ParserInfo.AsBool
            }, {
                SpecialType.System_Object,
                ParserInfo.StringIdentity
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

    public ParserFinder(Action<Diagnostic> addDiagnostic, SemanticModel model) {
        this.addDiagnostic = addDiagnostic;
        _model = model;

        // todo: make those static and reset them on every end of the pipeline
        _typeParserCache = new(FindParserForTypeCore, _specialTypesMap);

        _attrParserCache = new(
            EqualityComparer<ParseWithAttribute>.Default,
            SymbolEqualityComparer.Default,
            GetParserFromNameCore
        );

        _implicitConversionsCache = new(
            SymbolEqualityComparer.Default,
            SymbolEqualityComparer.Default,
            _model.Compilation.HasImplicitConversion
        );
    }

    public bool TryGetParserFromName(ParseWithAttribute attr, ITypeSymbol targetType, out ParserInfo parser) {
        parser = _attrParserCache.GetValue(attr, targetType);

        if (parser is not ParserInfo.Invalid invalidParser)
            return true;

        addDiagnostic(
            Diagnostic.Create(
                invalidParser.Descriptor,
                attr.ParserNameSyntaxRef.GetLocation(),
                invalidParser.MessageArgs
            )
        );

        return false;
    }

    ParserInfo GetParserFromNameCore(ParseWithAttribute attr, ITypeSymbol targetType) {
        var members = _model.GetMemberGroup(attr.ParserNameSyntaxRef.GetSyntax());

        bool hasAnyMethodWithName = false; // can't use members.Length cause they're not all methods

        ParserInfo? parserInfo = null;

        foreach (var member in members) {
            if (member.Kind != SymbolKind.Method)
                continue;

            hasAnyMethodWithName = true;

            var method = (member as IMethodSymbol)!;

            if (method.MethodKind != MethodKind.Ordinary)
                continue;

            if (TryGetParserInfo(method, targetType, out parserInfo))
                return parserInfo;
        }

        if (hasAnyMethodWithName) {
            if (members.Length == 1)
                return parserInfo!;

            // if there's more than one methods, better to just say none of them fit
            // rather than spit out just the reason the last candidate was rejected
            return new ParserInfo.Invalid(
                Diagnostics.NoValidParserOverload,
                attr.ParserName
            );
        }

        return new ParserInfo.Invalid(
            Diagnostics.CouldntFindNamedParser,
            attr.ParserName
        );
    }

    public bool TryFindParserForType(ITypeSymbol sourceType, Location queryLocation, out ParserInfo parser) {
        parser = FindParserForType(sourceType);

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

    ParserInfo FindParserForType(ITypeSymbol sourceType)
        => _typeParserCache.GetValue(sourceType);

    ParserInfo FindParserForTypeCore(ITypeSymbol sourceType) {
        if (sourceType.SpecialType == SpecialType.System_String)
            return ParserInfo.StringIdentity;

        if (sourceType is not INamedTypeSymbol targetType) {
            return new ParserInfo.Invalid(Diagnostics.NotValidParserType);
        }

        // this will only be true for nullable *value types*, contrary to SymbolUtils.IsNullable
        if (targetType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) {
            return FindParserForType(targetType.TypeArguments[0]);
        }

        // * too costly for now
        // if (_implicitConversionsCache.GetValue(CommonTypes.STR, sourceType))
        //     return new ParserInfo.Identity(MinimalTypeInfo.FromSymbol(sourceType));

        if (targetType.IsUnboundGenericType) {
            return new ParserInfo.Invalid(Diagnostics.GiveUp);
        }

        if (targetType.EnumUnderlyingType is not null) {
            var minTypeInfo = MinimalTypeInfo.FromSymbol(targetType);

            return new ParserInfo.DirectMethod(
                "WrapParseEnum<" + minTypeInfo.FullName + ">",
                minTypeInfo
            );
        }

        ParserInfo? parserInfo = null;

        foreach (var ctor in targetType.Constructors) {
            if (TryGetParserInfo(ctor, targetType, out parserInfo))
                return parserInfo;
        }

        // todo(#7): support extension methods

        // if we didn't find a suitable constructor, try to find Parse(string) or TryParse(string, out $target)

        ParserInfo? boolOutParseCandidate = null;
        int candidateCount = 0;

        foreach (var member in targetType.GetMembers()) {
            if (member.Kind != SymbolKind.Method)
                continue;

            var method = (member as IMethodSymbol)!;

            if (method.Name is "TryParse" or "Parse") {
                candidateCount++;
                if (TryGetParserInfo(method, targetType, out parserInfo)) {
                    // always prefer Parse methods over TryParse, since the
                    // first generally gives more info with exception
                    // messages
                    if (parserInfo is ParserInfo.BoolOutMethod bo)
                        boolOutParseCandidate = bo;
                    else
                        return parserInfo;
                }
            }
        }

        // if we're here, we went through every method without finding
        // any Parse() method
        if (boolOutParseCandidate is not null)
            return boolOutParseCandidate;

        switch (candidateCount) {
            case 0:
                return new ParserInfo.Invalid(Diagnostics.CouldntFindAutoParser, targetType.GetErrorName());
            case 1:
                return parserInfo!; // notnull: parserInfo gets set at the same time as candidateCount gets incremented
            default:
                return new ParserInfo.Invalid(Diagnostics.NoValidAutoParser, targetType.GetErrorName());
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
        Debug.Assert(method.Parameters.Length is 1);

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
        Debug.Assert(ctor.Parameters.Length is 1);

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
        Debug.Assert(method.Parameters.Length is 2);

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
                    Diagnostics.ParamCountWrongForParser,
                    method.GetErrorName(),
                    method.Parameters.Length
                );
                return false;
        }
    }
}