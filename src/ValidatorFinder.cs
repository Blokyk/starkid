using StarKid.Generator.AttributeModel;
using StarKid.Generator.CommandModel;
using StarKid.Generator.SymbolModel;

namespace StarKid.Generator;

public class ValidatorFinder
{
    private readonly Action<Diagnostic> addDiagnostic;

    private readonly Cache<ValidateWithAttribute, ITypeSymbol, ValidatorInfo> _attrValidatorCache;
    private readonly Cache<ITypeSymbol, ITypeSymbol, bool> _implicitConversionsCache;

    private readonly Compilation _compilation;

    public ValidatorFinder(Action<Diagnostic> addDiagnostic, Compilation compilation) {
        this.addDiagnostic = addDiagnostic;
        _compilation = compilation;

        _attrValidatorCache = new(
            EqualityComparer<ValidateWithAttribute>.Default,
            SymbolEqualityComparer.Default,
            GetValidatorCore
        );

        _implicitConversionsCache = new(
            SymbolEqualityComparer.Default,
            SymbolEqualityComparer.Default,
            _compilation.HasImplicitConversion
        );
    }

    public bool TryGetValidator(ValidateWithAttribute attr, ITypeSymbol argType, out ValidatorInfo validator) {
        validator = _attrValidatorCache.GetValue(attr, argType) with { Message = attr.ErrorMessage };

        if (validator is not ValidatorInfo.Invalid invalidValidator)
            return true;

        addDiagnostic(
            Diagnostic.Create(
                invalidValidator.Descriptor,
                attr.ValidatorNameExpr.GetLocation(),
                invalidValidator.MessageArgs
            )
        );

        return false;
    }

    ValidatorInfo GetValidatorCoreWithoutProperty(ValidateWithAttribute attr, ITypeSymbol type) {
        if (type is not INamedTypeSymbol argType)
            return new ValidatorInfo.Invalid(Diagnostics.UnvalidatableType, type.GetErrorName());

        var members = _compilation.GetMemberGroup(attr.ValidatorNameExpr);

        int candidateMethods = 0;
        ValidatorInfo? validator = null;

        foreach (var member in members) {
            if (member is not IMethodSymbol method)
                break;

            candidateMethods++;

            if (TryGetValidatorFromMethod(method, argType, out validator))
                return validator;
        }

        if (candidateMethods == 0) {
            return new ValidatorInfo.Invalid(
                Diagnostics.CouldntFindValidator,
                attr.ValidatorNameExpr
            );
        }

        if (candidateMethods == 1)
            return validator!; // nonull: always assigned when there's a candidate

        return new ValidatorInfo.Invalid(
            Diagnostics.NoValidValidatorMethod,
            attr.ValidatorNameExpr, argType.GetErrorName()
        );
    }

    ValidatorInfo GetValidatorCore(ValidateWithAttribute attr, ITypeSymbol operandType) {
        if (operandType is not (INamedTypeSymbol or IArrayTypeSymbol))
            return new ValidatorInfo.Invalid(Diagnostics.UnvalidatableType, operandType.GetErrorName());

        var memberSymbolInfo = _compilation.GetSymbolInfo(attr.ValidatorNameExpr);

        if (memberSymbolInfo.Symbol is not null) {
            var symbol = memberSymbolInfo.Symbol;

            // we don't need to check for methods, since those would be rejected by
            // GetSymbolInfo since the expression technically refers to a method group

            if (symbol is not IPropertySymbol propSymbol)
                goto COULDNT_FIND_VALIDATOR;

            // we return even if it's invalid because there's no alternative anyway
            return GetValidatorFromProperty(propSymbol, operandType);
        } else if (memberSymbolInfo.CandidateReason == CandidateReason.MemberGroup) {
            ValidatorInfo? validator = null;

            foreach (var symbol in memberSymbolInfo.CandidateSymbols) {
                // we should only be getting methods here, cf note above
                if (symbol is not IMethodSymbol methodSymbol)
                    goto COULDNT_FIND_VALIDATOR;

                if (TryGetValidatorFromMethod(methodSymbol, operandType, out validator))
                    return validator;
            }

            // if there's only one candidate symbol, then return the error specific to it
            if (memberSymbolInfo.CandidateSymbols.Length == 1)
                return validator!; // nonnull: if there's any candidate, we will always have tried to find a validator

            return new ValidatorInfo.Invalid(
                Diagnostics.NoValidValidatorMethod,
                attr.ValidatorNameExpr, operandType.GetErrorName()
            );
        }

    COULDNT_FIND_VALIDATOR:
        return new ValidatorInfo.Invalid(
            Diagnostics.CouldntFindValidator,
            attr.ValidatorNameExpr
        );
    }

    private enum ArgTypeRelation {
        Unvalidatable, IncompatibleTypes,
        DirectlyCompatible, ElementWiseOnly
    }

    private ArgTypeRelation ClassifyTypeRelation(
        ITypeSymbol srcType,
        ITypeSymbol dstType,
        Func<ITypeSymbol, ITypeSymbol, bool> areCompatible
    ) {
        if (srcType is not (INamedTypeSymbol or IArrayTypeSymbol)
         || dstType is not (INamedTypeSymbol or IArrayTypeSymbol))
            return ArgTypeRelation.Unvalidatable;

        if (areCompatible(srcType, dstType))
            return ArgTypeRelation.DirectlyCompatible;

        if (srcType is IArrayTypeSymbol { ElementType: var itemType }
         && areCompatible(itemType, dstType))
            return ArgTypeRelation.ElementWiseOnly;

        return ArgTypeRelation.IncompatibleTypes;
    }

    bool TryGetValidatorFromMethod(
        IMethodSymbol method,
        ITypeSymbol operandType,
        out ValidatorInfo validator
    ) {
        if (method.MethodKind != MethodKind.Ordinary) {
            validator = new ValidatorInfo.Invalid(
                Diagnostics.NoValidValidatorMethod,
                method.GetErrorName()
            );

            return false;
        }

        if (!method.IsStatic) {
            validator = new ValidatorInfo.Invalid(
                Diagnostics.ValidatorMustBeStatic,
                method.GetErrorName()
            );

            return false;
        }

        if (method.Parameters.Length != 1) {
            validator = new ValidatorInfo.Invalid(
                Diagnostics.ValidatorWrongParameter,
                operandType.GetErrorName()
            );

            return false;
        }

        // if it's an instance of Nullable<T>, unwrap it first
        if (operandType is INamedTypeSymbol {
                    ConstructedFrom.SpecialType: SpecialType.System_Nullable_T,
                    TypeArguments: [var innerOperandType]
                }
        ) {
            operandType = innerOperandType;
        }

        if (method.IsGenericMethod) {
            // if this validator has more than one type param, then we can't do anything
            // with it right now (cf #43)
            if (method.Arity > 1) {
                validator = new ValidatorInfo.Invalid(
                    Diagnostics.ValidatorHasWrongTypeArity,
                    method.Arity
                );
                return false;
            }

            // fixme: this isn't correct for repeatable arguments
            // assume the type parameter is supposed to be the symbol's type; so, we do
            // the rest of the checks with that assumption by substituting the type param
            // with the symbol's type
            method = method.Construct(operandType);
        }

        bool elementWise = false;
        switch (ClassifyTypeRelation(operandType, method.Parameters[0].Type, _implicitConversionsCache.GetValue)) {
            case ArgTypeRelation.DirectlyCompatible:
                break;
            case ArgTypeRelation.ElementWiseOnly:
                elementWise = true;
                break;
            case ArgTypeRelation.Unvalidatable:
                validator = new ValidatorInfo.Invalid(
                    Diagnostics.UnvalidatableType,
                    operandType.GetErrorName()
                );

                return false;
            case ArgTypeRelation.IncompatibleTypes:
                validator = new ValidatorInfo.Invalid(
                    Diagnostics.ValidatorWrongParameter,
                    operandType.GetErrorName()
                );

                return false;
        }

        var minMethodInfo = MinimalMethodInfo.FromSymbol(method);

        var containingTypeFullName = SymbolInfoCache.GetFullTypeName(method.ContainingType);

        if (method.ReturnsVoid) {
            var methodName = minMethodInfo.ToString();

            validator = new ValidatorInfo.Method.Exception(minMethodInfo) {
                IsElementWiseValidator = elementWise
            };
            return true;
        }

        if (minMethodInfo.ReturnType.SpecialType == SpecialType.System_Boolean) {
            validator = new ValidatorInfo.Method.Bool(minMethodInfo) {
                IsElementWiseValidator = elementWise
            };
            return true;
        }

        validator = new ValidatorInfo.Invalid(
            Diagnostics.ValidatorReturnMismatch,
            method.GetErrorName()
        );

        return false;
    }

    ValidatorInfo GetValidatorFromProperty(
        IPropertySymbol prop,
        ITypeSymbol argType
    ) {
        if (prop.Type.SpecialType is not SpecialType.System_Boolean) {
            return new ValidatorInfo.Invalid(
                Diagnostics.ValidatorPropertyReturnMismatch,
                prop.GetErrorName()
            );
        }

        var targetType = prop.ContainingType;

        switch (ClassifyTypeRelation(argType, targetType, SymbolUtils.IsBaseOrInterfaceOf)) {
            case ArgTypeRelation.DirectlyCompatible:
                return new ValidatorInfo.Property(prop.Name, MinimalMemberInfo.FromSymbol(prop));
            case ArgTypeRelation.ElementWiseOnly:
                return new ValidatorInfo.Property(prop.Name, MinimalMemberInfo.FromSymbol(prop)) {
                    IsElementWiseValidator = true
                };
            default:
                return new ValidatorInfo.Invalid(
                    Diagnostics.PropertyValidatorNotOnArgType,
                    targetType.GetErrorName(), prop.GetErrorName(), argType.GetErrorName()
                );
        }
    }
}