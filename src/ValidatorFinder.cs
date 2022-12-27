using Recline.Generator.Model;

namespace Recline.Generator;

public class ValidatorFinder
{
    private readonly ImmutableArray<Diagnostic>.Builder _diagnostics;

    private readonly Cache<ValidateWithAttribute, ITypeSymbol, ValidatorInfo> _attrValidatorCache;
    private readonly Cache<ITypeSymbol, ITypeSymbol, bool> _implicitConversionsCache;

    private readonly SemanticModel _model;

    public ValidatorFinder(ref ImmutableArray<Diagnostic>.Builder diags, SemanticModel model) {
        _diagnostics = diags;
        _model = model;

        _attrValidatorCache = new(
            Utils.ValidateWithAttributeComparer,
            SymbolEqualityComparer.Default,
            GetValidatorCore
        );

        _implicitConversionsCache = new(
            SymbolEqualityComparer.Default,
            SymbolEqualityComparer.Default,
            _model.Compilation.HasImplicitConversion
        );
    }

    public bool TryGetValidator(ValidateWithAttribute attr, ITypeSymbol argType, out ValidatorInfo validator) {
        validator = _attrValidatorCache.GetValue(attr, argType) with { Message = attr.ErrorMessage };

        if (validator is ValidatorInfo.Invalid errorInfo) {
            if (errorInfo.Diagnostic is not null) {
                _diagnostics.Add(errorInfo.Diagnostic);
            } else {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.CouldntFindValidator,
                        attr.ValidatorNameSyntaxRef.GetLocation(),
                        attr.ValidatorName
                    )
                );
            }
        }

        return validator is not ValidatorInfo.Invalid;
    }

    ValidatorInfo GetValidatorCore(ValidateWithAttribute attr, ITypeSymbol argType) {
        var members = _model.GetMemberGroup(attr.ValidatorNameSyntaxRef.GetSyntax());

        bool hasAnyMethodWithName = false; // can't use members.Length cause they're not all methods

        foreach (var member in members) {
            //todo: allow properties (if they're bool obviously)
            if (member.Kind != SymbolKind.Method)
                continue;

            hasAnyMethodWithName = true;

            var method = (member as IMethodSymbol)!;

            if (method.MethodKind != MethodKind.Ordinary)
                continue;

            if (method.Parameters.Length != 1)
                continue;

            if (!_implicitConversionsCache.GetValue(argType, method.Parameters[0].Type))
                continue;

            // at this point there's no other possible overload

            if (!method.IsStatic) {
                return new ValidatorInfo.Invalid(
                    Diagnostic.Create(
                        Diagnostics.ValidatorMustBeStatic,
                        attr.ValidatorNameSyntaxRef.GetLocation(),
                        method.GetErrorName()
                    )
                );
            }

            var minMethodInfo = MinimalMethodInfo.FromSymbol(method);

            var containingTypeFullName = SymbolInfoCache.GetFullTypeName(method.ContainingType);

            var returnType = minMethodInfo.ReturnType;

            if (returnType == CommonTypes.BOOLMinInfo)
                return new ValidatorInfo.Method.Bool(containingTypeFullName + "." + minMethodInfo.Name, minMethodInfo);

            // we need nullability here
            if (SymbolUtils.Equals(method.ReturnType, CommonTypes.EXCEPTION))
                return new ValidatorInfo.Method.Exception(containingTypeFullName + "." + minMethodInfo.Name, minMethodInfo);

            if (SymbolUtils.Equals(method.ReturnType, CommonTypes.STR))
                return new ValidatorInfo.Method.String(containingTypeFullName + "." + minMethodInfo.Name, minMethodInfo);

            return new ValidatorInfo.Invalid(
                    Diagnostic.Create(
                        Diagnostics.ValidatorReturnMismatch,
                        attr.ValidatorNameSyntaxRef.GetLocation(),
                        method.GetErrorName()
                    )
                );
        }

        if (!hasAnyMethodWithName) {
            return new ValidatorInfo.Invalid(
                Diagnostic.Create(
                    Diagnostics.NoValidValidatorMethod,
                    attr.ValidatorNameSyntaxRef.GetLocation(),
                    attr.ValidatorName, argType.GetErrorName()
                )
            );
        }

        return new ValidatorInfo.Invalid();
    }
}