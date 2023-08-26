using Recline.Generator.Model;

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

    bool TryGetParserInfo(IMethodSymbol method, ITypeSymbol targetType, out ParserInfo parser) {
        // todo: add checks like bound generics, etc

        var isCtor = method.MethodKind == MethodKind.Constructor;

        if (isCtor) {
            if (method.DeclaredAccessibility < Accessibility.Internal) {
                parser = new ParserInfo.Invalid(Diagnostics.NonAccessibleCtor);
                return false;
            }
        } else {
            if (!method.IsStatic) {
                parser = new ParserInfo.Invalid(Diagnostics.ParserHasToBeStatic);
                return false;
            }

            // note: when we refactor to lift the restriction on generic methods,
            // also change the Enum.Parse code in FindParserForType
            if (method.IsGenericMethod) {
                parser = new ParserInfo.Invalid(Diagnostics.ParserCantBeGenericMethod);
                return false;
            }

            if (method.ReturnsByRef || method.ReturnsByRefReadonly) {
                parser = new ParserInfo.Invalid(Diagnostics.ParserCantReturnRef);
                return false;
            }
        }

        if (method.Parameters.Length != 0) {
            var inputParam = method.Parameters[0];

            if (inputParam.RefKind != RefKind.None) {
                parser = new ParserInfo.Invalid(Diagnostics.ParserParamWrongRefKind, inputParam.RefKind);
                return false;
            }

            if (!SymbolUtils.IsStringLike(inputParam.Type)) {
                parser = new ParserInfo.Invalid(Diagnostics.ParserMustTakeStringParam);
                return false;
            }
        }

        switch (method.Parameters.Length) {
            case 1: { // $targetType Parse(string)
                var isReturnTypeTarget
                    = isCtor
                    ? _implicitConversionsCache.GetValue(method.ContainingType, targetType)
                    : _implicitConversionsCache.GetValue(method.ReturnType, targetType);

                if (!isReturnTypeTarget) {
                    parser = new ParserInfo.Invalid(
                        Diagnostics.ParserHasToReturnTargetType,
                        targetType.GetErrorName(),
                        method.ReturnType.GetErrorName()
                    );
                    return false;
                }

                var targetTypeInfo = MinimalTypeInfo.FromSymbol(targetType);

                if (isCtor) {
                    parser = new ParserInfo.Constructor(targetTypeInfo);
                    return true;
                }

                var containingTypeFullName = SymbolInfoCache.GetFullTypeName(method.ContainingType);

                parser = new ParserInfo.DirectMethod(containingTypeFullName + "." + method.Name, targetTypeInfo);
                return true;
            }
            case 2: { // bool TryParse(string, out $targetType)
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

                return false;
            }
            default: {
                parser = new ParserInfo.Invalid(
                    Diagnostics.ParamCountWrongForParser,
                    method.GetErrorName(),
                    method.Parameters.Length
                );
                return false;
            }
        }
    }
}