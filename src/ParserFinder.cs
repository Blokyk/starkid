using Recline.Generator.Model;

namespace Recline.Generator;

public class ParserFinder
{
    private ImmutableArray<Diagnostic>.Builder _diagnostics;

    private Cache<(ITypeSymbol source, ITypeSymbol target), bool> _implicitConversionsCache;
    private Cache<ITypeSymbol, ParserInfo> _typeParserCache;
    private Cache<ParseWithAttribute, ITypeSymbol, ParserInfo> _attrParserCache;

    private SemanticModel _model;

    public ParserFinder(ref ImmutableArray<Diagnostic>.Builder diags, SemanticModel model) {
        _diagnostics = diags;
        _model = model;
        _implicitConversionsCache = new(
            new TupleComparer<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default, SymbolEqualityComparer.Default),
            (t) => _model.Compilation.HasImplicitConversion(t.source, t.target)
        );
        _typeParserCache = new(SymbolEqualityComparer.Default, FindParserForType);
        _attrParserCache = new(Utils.ParseWithAttributeComparer, GetParserFromName);
    }

    public bool TryGetParser(ParseWithAttribute? attr, ITypeSymbol targetType, [NotNullWhen(true)] out ParserInfo? parser)
        => attr is null ? TryFindParserForType(targetType, out parser) : TryGetParserFromName(attr, targetType, out parser);

    public bool TryGetParserFromName(ParseWithAttribute attr, ITypeSymbol targetType, out ParserInfo parser) {
        parser = _attrParserCache.GetValue(attr, targetType);

        if (parser is ParserInfo.Invalid invalidParser) {
            if (invalidParser.Diagnostic is not null)
                _diagnostics.Add(invalidParser.Diagnostic); // TODO: change location when attached
            else
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.CouldntFindNamedParser,
                        Location.None,
                        attr.ParserName, attr.TypeSymbol.GetErrorName()
                    )
                );

            return false;
        }

        return true;
    }

    ParserInfo GetParserFromName(ParseWithAttribute attr, ITypeSymbol targetType) {
        if (attr.TypeSymbol is not INamedTypeSymbol { IsUnboundGenericType: false } type) {
            return new ParserInfo.Invalid(
                Diagnostic.Create(
                    Diagnostics.NotValidParserType,
                    Location.None,
                    attr.TypeSymbol.GetErrorName()
                )
            );
        }

        var members = type.GetMembers(attr.ParserName);

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
                    Location.None,
                    // can't use a member's error name since this needs to be the *name* of the method,
                    // and error name has args and stuff in it
                    attr.TypeSymbol.GetErrorName() + "." + attr.ParserName
                )
            );
    }

    public bool TryFindParserForType(ITypeSymbol sourceType, out ParserInfo parser) {
        //parser = ParserInfo.AsBool;
        //return true;
        parser = _typeParserCache.GetValue(sourceType);

        if (parser is ParserInfo.Invalid)
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CouldntFindAutoParser,
                    Location.None, // FIXME: location
                    sourceType.GetErrorName()
                )
            );

        return parser is not null;
    }

    ParserInfo FindParserForType(ITypeSymbol sourceType) {
        if (SymbolUtils.Equals(sourceType, CommonTypes.STR))
            return new ParserInfo.Identity(CommonTypes.STRMinInfo);

        if (_model.Compilation.HasImplicitConversion(sourceType, CommonTypes.STR))
            return new ParserInfo.Identity(MinimalTypeInfo.FromSymbol(sourceType));

        ParserInfo parser = ParserInfo.Error;

        if (sourceType is not INamedTypeSymbol type)
            return ParserInfo.Error;

        if (SymbolUtils.Equals(type.ConstructedFrom, CommonTypes.NULLABLE))
            return TryFindParserForType(type.TypeArguments[0], out parser) ? parser : ParserInfo.Error;

        /*
        * This is way too strict. In reality, you could have type like this :
        *
        * class Wrapper<T> {
        *     public Wrapper(T item) { ... }
        * }
        *
        * Which would be completely valid if it was "instantiated" as Wrapper<string>
        */
        if (type.IsGenericType)// FIXME: could/should be lifted, cf above
            return new ParserInfo.Invalid(
                Diagnostic.Create(
                    Diagnostics.NoGenericAutoParser,
                    Location.None,
                    type.GetErrorName()
                )
            );

        // TODO: check if enum

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

        if (!isCtor && (!method.IsStatic || method.IsGenericMethod))// || !_model.IsAccessible(1, method))
            return ParserInfo.Error;

        switch (method.Parameters.Length) {
            case 1: {
                var param = method.Parameters[0];

                var isValid = (method.MethodKind == MethodKind.Constructor || _implicitConversionsCache.GetValue((targetType, method.ReturnType)))
                    && _implicitConversionsCache.GetValue((CommonTypes.STR, param.Type));

                if (!isValid)
                    return ParserInfo.Error;

                var targetTypeInfo = MinimalTypeInfo.FromSymbol(targetType);

                if (isCtor)
                    return new ParserInfo.Constructor(targetTypeInfo);

                var containingTypeInfo = MinimalTypeInfo.FromSymbol(method.ContainingType);
                return new ParserInfo.DirectMethod(containingTypeInfo.FullName + "." + method.Name, targetTypeInfo);
            }
            case 2: {
                if (!_implicitConversionsCache.GetValue((CommonTypes.BOOL, method.ReturnType)))
                    return ParserInfo.Error;

                var outParam = method.Parameters[1];

                if (outParam.RefKind != RefKind.Out)
                    return ParserInfo.Error;

                if (!_implicitConversionsCache.GetValue((outParam.Type, targetType)))
                    return ParserInfo.Error;

                var inputParam = method.Parameters[0];

                var isValid = _implicitConversionsCache.GetValue((CommonTypes.STR, inputParam.Type));

                if (!isValid)
                    return ParserInfo.Error;

                var minTypeInfo = MinimalTypeInfo.FromSymbol(targetType);

                return new ParserInfo.BoolOutMethod(
                    minTypeInfo.FullName + "." + method.Name,
                    minTypeInfo
                );
            }
            default: {
                return ParserInfo.Error;
            }
        }
    }
}