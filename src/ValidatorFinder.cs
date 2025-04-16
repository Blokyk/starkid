using StarKid.Generator.AttributeModel;
using StarKid.Generator.CommandModel;
using StarKid.Generator.SymbolModel;

namespace StarKid.Generator;

public class ValidatorFinder
{
    private readonly Action<Diagnostic> addDiagnostic;

    private readonly Cache<ValidateWithAttribute, ITypeSymbol, ValidatorInfo> _attrValidatorMethodCache;
    private readonly Cache<ValidatePropAttribute, ITypeSymbol, ValidatorInfo> _attrValidatorPropCache;
    private readonly Cache<ITypeSymbol, ITypeSymbol, bool> _implicitConversionsCache;

    private readonly Compilation _compilation;

    public ValidatorFinder(Action<Diagnostic> addDiagnostic, Compilation compilation) {
        this.addDiagnostic = addDiagnostic;
        _compilation = compilation;

        _attrValidatorMethodCache = new(
            EqualityComparer<ValidateWithAttribute>.Default,
            SymbolEqualityComparer.Default,
            GetValidatorMethodCore
        );

        _attrValidatorPropCache = new(
            EqualityComparer<ValidatePropAttribute>.Default,
            SymbolEqualityComparer.Default,
            GetValidatorPropertyCore
        );

        _implicitConversionsCache = new(
            SymbolEqualityComparer.Default,
            SymbolEqualityComparer.Default,
            _compilation.HasImplicitConversion
        );
    }

    public bool TryGetValidator(ValidateWithAttribute attr, ITypeSymbol argType, out ValidatorInfo validator) {
        validator = _attrValidatorMethodCache.GetValue(attr, argType) with { Message = attr.ErrorMessage };

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

    public bool TryGetValidator(ValidatePropAttribute attr, ITypeSymbol argType, out ValidatorInfo validator) {
        validator = _attrValidatorPropCache.GetValue(attr, argType) with { Message = attr.ErrorMessage };

        if (validator is not ValidatorInfo.Invalid invalidValidator)
            return true;

        addDiagnostic(
            Diagnostic.Create(
                invalidValidator.Descriptor,
                attr.PropertyNameExpr.GetLocation(),
                invalidValidator.MessageArgs
            )
        );

        return false;
    }

    ValidatorInfo GetValidatorMethodCore(ValidateWithAttribute attr, ITypeSymbol operandType) {
        if (operandType is not (INamedTypeSymbol or IArrayTypeSymbol))
            return new ValidatorInfo.Invalid(Diagnostics.UnvalidatableType, operandType.GetErrorName());

        var members = _compilation.GetMemberGroup(attr.ValidatorNameExpr);

        int candidateMethods = 0;
        ValidatorInfo? validator = null;

        foreach (var member in members) {
            if (member is not IMethodSymbol method)
                break;

            candidateMethods++;

            if (TryGetValidatorFromMethod(method, operandType, out validator))
                return validator;
        }

        if (candidateMethods == 0) {
            return new ValidatorInfo.Invalid(
                Diagnostics.CouldntFindValidatorMethod,
                attr.ValidatorNameExpr
            );
        }

        if (candidateMethods == 1)
            return validator!; // notnull: always assigned when there's a candidate

        return new ValidatorInfo.Invalid(
            Diagnostics.NoValidValidatorMethod,
            attr.ValidatorNameExpr, operandType.GetErrorName()
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
            var typeArg
                = operandType is IArrayTypeSymbol { ElementType: var elemType }
                ? elemType
                : operandType;
            method = method.Construct(typeArg);
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

    ValidatorInfo GetValidatorPropertyCore(ValidatePropAttribute attr, ITypeSymbol operandType) {
        if (operandType is not (INamedTypeSymbol or IArrayTypeSymbol))
            return new ValidatorInfo.Invalid(Diagnostics.UnvalidatableType, operandType.GetErrorName());

        var memberSymbolInfo = _compilation.GetSymbolInfo(attr.PropertyNameExpr);

        if (memberSymbolInfo.Symbol is not IPropertySymbol propSymbol) {
            return new ValidatorInfo.Invalid(
                Diagnostics.CouldntFindValidatorProp,
                attr.PropertyNameExpr
            );
        }

        // we return even if it's invalid because it'll probably have the most useful error message
        return GetValidatorFromProperty(propSymbol, attr.ExpectedValue, operandType);
    }

    ValidatorInfo GetValidatorFromProperty(
        IPropertySymbol prop,
        bool expectedValue,
        ITypeSymbol argType
    ) {
        if (prop.Type.SpecialType is not SpecialType.System_Boolean) {
            return new ValidatorInfo.Invalid(
                Diagnostics.ValidatorPropertyReturnMismatch,
                prop.GetErrorName()
            );
        }

        var targetType = prop.ContainingType;

        // this functions does two things:
        //    - it unwraps the types directly, since any combination is
        //      valid in this case (thanks to ThrowIfNotValid* overloads)
        //    - it reverses the order of the call to `IsBaseOrInterfaceOf`
        // the reason why we need a different function for these is that
        // ClassifyTypeRelation() also tries to match-up array-arg/item-target
        // situations to enable repeatable options; thus, inverting the order
        // of the parameters or changing the nullability would mess up that
        // detection mecanism
        Func<ITypeSymbol, ITypeSymbol, bool> reverseBaseOf =
            (argType, targetType) => {
                targetType = targetType.UnwrapNullable();
                argType = argType.UnwrapNullable();

                return targetType.IsSelfOrBaseOrInterfaceOf(argType);
            };

        switch (ClassifyTypeRelation(argType, targetType, reverseBaseOf)) {
            case ArgTypeRelation.DirectlyCompatible:
                return new ValidatorInfo.Property(prop.Name, expectedValue, MinimalMemberInfo.FromSymbol(prop));
            case ArgTypeRelation.ElementWiseOnly:
                return new ValidatorInfo.Property(prop.Name, expectedValue, MinimalMemberInfo.FromSymbol(prop)) {
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