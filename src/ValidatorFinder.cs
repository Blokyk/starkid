using Recline.Generator.Model;

namespace Recline.Generator;

public class ValidatorFinder
{
    private readonly Action<Diagnostic> addDiagnostic;

    private readonly Cache<ValidateWithAttribute, ITypeSymbol, ValidatorInfo> _attrValidatorCache;
    private readonly Cache<ITypeSymbol, ITypeSymbol, bool> _implicitConversionsCache;

    private readonly SemanticModel _model;

    public ValidatorFinder(Action<Diagnostic> addDiagnostic, SemanticModel model) {
        this.addDiagnostic = addDiagnostic;
        _model = model;

        _attrValidatorCache = new(
            EqualityComparer<ValidateWithAttribute>.Default,
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

        if (validator is not ValidatorInfo.Invalid invalidValidator)
            return true;

        addDiagnostic(
            Diagnostic.Create(
                invalidValidator.Descriptor,
                attr.ValidatorNameSyntaxRef.GetLocation(),
                invalidValidator.MessageArgs
            )
        );

        return false;
    }

    ValidatorInfo GetValidatorCoreWithoutProperty(ValidateWithAttribute attr, ITypeSymbol type) {
        if (type is not INamedTypeSymbol argType)
            return new ValidatorInfo.Invalid(Diagnostics.NotValidValidatorType, type.GetErrorName());

        var members = _model.GetMemberGroup(attr.ValidatorNameSyntaxRef.GetSyntax());

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
                attr.ValidatorName
            );
        }

        if (candidateMethods == 1)
            return validator!; // nonull: always assigned when there's a candidate

        return new ValidatorInfo.Invalid(
            Diagnostics.NoValidValidatorMethod,
            attr.ValidatorName, argType.GetErrorName()
        );
    }

    ValidatorInfo GetValidatorCore(ValidateWithAttribute attr, ITypeSymbol type) {
        if (type is not INamedTypeSymbol argType)
            return new ValidatorInfo.Invalid(Diagnostics.NotValidValidatorType, type.GetErrorName());

        var memberSymbolInfo = _model.GetSymbolInfo(attr.ValidatorNameSyntaxRef.GetSyntax());

        if (memberSymbolInfo.Symbol is not null) {
            var symbol = memberSymbolInfo.Symbol;

            // we don't need to check for methods, since those would be rejected by
            // GetSymbolInfo since the expression technically refers to a method group

            if (symbol is not IPropertySymbol propSymbol)
                goto COULDNT_FIND_VALIDATOR;

            // we return even if it's invalid because there's no alternative anyway
            return GetValidatorFromProperty(propSymbol, argType);
        } else if (memberSymbolInfo.CandidateReason == CandidateReason.MemberGroup) {
            ValidatorInfo? validator = null;

            foreach (var symbol in memberSymbolInfo.CandidateSymbols) {
                // we should only be getting methods here, cf note above
                if (symbol is not IMethodSymbol methodSymbol)
                    goto COULDNT_FIND_VALIDATOR;

                if (TryGetValidatorFromMethod(methodSymbol, argType, out validator))
                    return validator;
            }

            // if there's only one candidate symbol, then return the error specific to it
            if (memberSymbolInfo.CandidateSymbols.Length == 1)
                return validator!; // nonnull: if there's any candidate, we will always have tried to find a validator

            return new ValidatorInfo.Invalid(
                Diagnostics.NoValidValidatorMethod,
                attr.ValidatorName, argType.GetErrorName()
            );
        }

    COULDNT_FIND_VALIDATOR:
        return new ValidatorInfo.Invalid(
            Diagnostics.CouldntFindValidator,
            attr.ValidatorName
        );
    }

    bool TryGetValidatorFromMethod(
        IMethodSymbol method,
        INamedTypeSymbol argType,
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
                argType.GetErrorName()
            );

            return false;
        }

        if (!_implicitConversionsCache.GetValue(argType, method.Parameters[0].Type)) {
            validator = new ValidatorInfo.Invalid(
                Diagnostics.ValidatorWrongParameter,
                argType.GetErrorName()
            );

            return false;
        }

        var minMethodInfo = MinimalMethodInfo.FromSymbol(method);

        var containingTypeFullName = SymbolInfoCache.GetFullTypeName(method.ContainingType);

        if (method.ReturnsVoid) {
            validator = new ValidatorInfo.Method.Exception(containingTypeFullName + "." + minMethodInfo.Name, minMethodInfo);
            return true;
        }

        if (minMethodInfo.ReturnType.SpecialType == SpecialType.System_Boolean) {
            validator = new ValidatorInfo.Method.Bool(containingTypeFullName + "." + minMethodInfo.Name, minMethodInfo);
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
        INamedTypeSymbol argType
    ) {
        if (prop.Type.SpecialType != SpecialType.System_Boolean) {
            return new ValidatorInfo.Invalid(
                Diagnostics.ValidatorPropertyReturnMismatch,
                prop.GetErrorName()
            );
        }

        if (!prop.ContainingType.IsBaseOf(argType)) {
            return new ValidatorInfo.Invalid(
                Diagnostics.PropertyValidatorNotOnArgType,
                prop.GetErrorName(), argType.GetErrorName()
            );
        }

        return new ValidatorInfo.Property(prop.Name, MinimalMemberInfo.FromSymbol(prop));
    }
}