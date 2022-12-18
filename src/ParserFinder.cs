using Recline.Generator.Model;

namespace Recline.Generator;

public class ParserFinder
{
    private readonly ImmutableArray<Diagnostic>.Builder _diagnostics;

    private readonly Cache<ITypeSymbol, ITypeSymbol, bool> _implicitConversionsCache;
    private readonly Cache<ITypeSymbol, ParserInfo> _typeParserCache;
    private readonly Cache<ParseWithAttribute, ITypeSymbol, ParserInfo> _attrParserCache;

    private readonly SemanticModel _model;

    public ParserFinder(ref ImmutableArray<Diagnostic>.Builder diags, SemanticModel model) {
        _diagnostics = diags;
        _model = model;

        _typeParserCache = new(SymbolEqualityComparer.Default, FindParserForType);

        _attrParserCache = new(
            Utils.ParseWithAttributeComparer,
            SymbolEqualityComparer.Default,
            GetParserFromName
        );

        _implicitConversionsCache = new(
            SymbolEqualityComparer.Default,
            SymbolEqualityComparer.Default,
            _model.Compilation.HasImplicitConversion
        );
    }

    public bool TryGetParserFromName(ParseWithAttribute attr, ITypeSymbol targetType, out ParserInfo parser) {
        parser = _attrParserCache.GetValue(attr, targetType);

        if (parser is ParserInfo.Invalid invalidParser) {
            if (invalidParser.Diagnostic is not null) {
                _diagnostics.Add(invalidParser.Diagnostic); // TODO: change location when attached
            } else {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.CouldntFindNamedParser,
                        attr.ParserNameSyntaxRef.GetLocation(),
                        attr.ParserName
                    )
                );
            }

            return false;
        }

        return true;
    }

    ParserInfo GetParserFromName(ParseWithAttribute attr, ITypeSymbol targetType) {
        var members = _model.GetMemberGroup(attr.ParserNameSyntaxRef.GetSyntax());

        bool hasAnyMethodWithName = false; // can't use members.Length cause they're not all methods

        foreach (var member in members) {
            if (member.Kind != SymbolKind.Method)
                continue;

            hasAnyMethodWithName = true;

            var method = (member as IMethodSymbol)!;

            if (method.MethodKind != MethodKind.Ordinary)
                continue;

            var parserInfo = GetParserInfo(method, targetType);

            if (parserInfo is not ParserInfo.Invalid)
                return parserInfo;
        }

        return !hasAnyMethodWithName
            ? ParserInfo.Error
            : new ParserInfo.Invalid(
                Diagnostic.Create(
                    Diagnostics.NoValidParserMethod,
                    attr.ParserNameSyntaxRef.GetLocation(),
                    attr.ParserName
                )
            );
    }

    public bool TryFindParserForType(ITypeSymbol sourceType, out ParserInfo parser) {
        parser = _typeParserCache.GetValue(sourceType);

        if (parser is ParserInfo.Invalid) {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CouldntFindAutoParser,
                    Location.None, // FIXME: location
                    sourceType.GetErrorName()
                )
            );
        }

        return parser is not null;
    }

    ParserInfo FindParserForType(ITypeSymbol sourceType) {
        if (sourceType.SpecialType == SpecialType.System_String)
            return new ParserInfo.Identity(CommonTypes.STRMinInfo);

        if (_implicitConversionsCache.GetValue(sourceType, CommonTypes.STR))
            return new ParserInfo.Identity(MinimalTypeInfo.FromSymbol(sourceType));

        if (sourceType is not INamedTypeSymbol type)
            return ParserInfo.Error;

        if (SymbolUtils.Equals(type.ConstructedFrom, CommonTypes.NULLABLE))
            return TryFindParserForType(type.TypeArguments[0], out var parser) ? parser : ParserInfo.Error;

        /*
        * This is way too strict. In reality, you could have type like this :
        *
        * class Wrapper<T> {
        *     public Wrapper(T item) { ... }
        * }
        *
        * Which would be completely valid if it was "instantiated" as Wrapper<string>
        */
        if (type.IsGenericType) { // FIXME: restriction on generics could/should be lifted, cf above
            return new ParserInfo.Invalid(
                Diagnostic.Create(
                    Diagnostics.NoGenericAutoParser,
                    Location.None,
                    type.GetErrorName()
                )
            );
        }

        if (type.EnumUnderlyingType is not null) {
            var minTypeInfo = MinimalTypeInfo.FromSymbol(type);

            // todo: lift the shame i got from writing this code
            // (this is only a temp solution because rn this is the only way
            // a generic method could get into a parser, since we disallow generics
            // in GetParserInfo)
            return new ParserInfo.BoolOutMethod(
                "System.Enum.TryParse<" + minTypeInfo.FullName + ">",
                minTypeInfo
            );
        }

        foreach (var ctor in type.Constructors) {
            var parserInfo = GetParserInfo(ctor, type);

            if (parserInfo is not ParserInfo.Invalid)
                return parserInfo;
        }

        // if we didn't find a suitable constructor, try to find TryParse(string, out $target) or Parse(string)
        // TODO: support extension methods ?

        // prefer TryParse-style methods over Parse-and-throw
        ParserInfo? directParseCandidate = null;

        foreach (var member in type.GetMembers()) {
            if (member.Kind != SymbolKind.Method)
                continue;

            var method = (member as IMethodSymbol)!;

            if (method.Name is "TryParse" or "Parse") {
                var parserInfo = GetParserInfo(method, type);

                if (parserInfo is not ParserInfo.Invalid) {
                    if (parserInfo is ParserInfo.DirectMethod dm)
                        directParseCandidate = dm;
                    else
                        return parserInfo;
                }
            }
        }

        return directParseCandidate ?? ParserInfo.Error;
    }

    ParserInfo GetParserInfo(IMethodSymbol method, ITypeSymbol targetType) {
        // TODO: add checks like bound generics, no ref return, etc

        // TODO: btw, we should probably warn when using a method that takes a non-nullable string
        // parameter for bool opts, as __arg will be null in most cases

        var isCtor = method.MethodKind == MethodKind.Constructor;

        // note: when we lift the restriction on generic methods, also change
        // the enum TryParse code in FindParserForType
        if (!isCtor && (!method.IsStatic || method.IsGenericMethod))// || !_model.IsAccessible(1, method))
            return ParserInfo.Error;

        switch (method.Parameters.Length) {
            case 1: {
                var param = method.Parameters[0];

                var isValid // method has a single arg of type string or equivalent and is either a ctor or a method returning $targetType
                    = (param.Type.SpecialType == SpecialType.System_String || _implicitConversionsCache.GetValue(CommonTypes.STR, param.Type))
                    && (method.MethodKind == MethodKind.Constructor || _implicitConversionsCache.GetValue(targetType, method.ReturnType));

                if (!isValid)
                    return ParserInfo.Error;

                var targetTypeInfo = MinimalTypeInfo.FromSymbol(targetType);

                if (isCtor)
                    return new ParserInfo.Constructor(targetTypeInfo);

                var containingTypeFullName = SymbolInfoCache.GetFullTypeName(method.ContainingType);
                return new ParserInfo.DirectMethod(containingTypeFullName + "." + method.Name, targetTypeInfo);
            }
            case 2: {
                if (method.ReturnType.SpecialType != SpecialType.System_Boolean)
                    return ParserInfo.Error;
                if (!_implicitConversionsCache.GetValue(CommonTypes.BOOL, method.ReturnType))
                    return ParserInfo.Error;

                var outParam = method.Parameters[1];

                if (outParam.RefKind != RefKind.Out)
                    return ParserInfo.Error;

                if (!_implicitConversionsCache.GetValue(outParam.Type, targetType))
                    return ParserInfo.Error;

                var inputParam = method.Parameters[0];

                var isValid
                    = inputParam.Type.SpecialType == SpecialType.System_String
                    || _implicitConversionsCache.GetValue(CommonTypes.STR, inputParam.Type);

                if (!isValid)
                    return ParserInfo.Error;

                var containingTypeFullName = SymbolInfoCache.GetFullTypeName(method.ContainingType);

                return new ParserInfo.BoolOutMethod(
                    containingTypeFullName + "." + method.Name,
                    MinimalTypeInfo.FromSymbol(targetType)
                );
            }
            default: {
                return ParserInfo.Error;
            }
        }
    }
}