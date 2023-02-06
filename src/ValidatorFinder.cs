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

        if (validator is ValidatorInfo.Invalid errorInfo) {
            if (errorInfo.Diagnostic is not null) {
                addDiagnostic(errorInfo.Diagnostic);
            } else {
                errorInfo = errorInfo with {
                    Diagnostic =
                        Diagnostic.Create(
                            Diagnostics.CouldntFindValidator,
                            attr.ValidatorNameSyntaxRef.GetLocation(),
                            attr.ValidatorName
                        )
                };

                validator = errorInfo;

                addDiagnostic(errorInfo.Diagnostic);
            }
        }

        return validator is not ValidatorInfo.Invalid;
    }

    ValidatorInfo GetValidatorCoreWithoutProperty(ValidateWithAttribute attr, ITypeSymbol argType) {
        var members = _model.GetMemberGroup(attr.ValidatorNameSyntaxRef.GetSyntax());

        bool hasAnyMethodWithName = false;

        foreach (var member in members) {
            if (member is not IMethodSymbol method)
                break;

            hasAnyMethodWithName = true;

            var validator = GetValidatorFromMethod(method, attr, argType);

            if (validator is not null)
                return validator;
        }

        if (hasAnyMethodWithName) {
            return new ValidatorInfo.Invalid(
                Diagnostic.Create(
                    Diagnostics.NoValidValidatorMethod,
                    attr.ValidatorNameSyntaxRef.GetLocation(),
                    attr.ValidatorName, argType.GetErrorName()
                )
            );
        }

        return new ValidatorInfo.Invalid(
            Diagnostic.Create(
                Diagnostics.CouldntFindValidator,
                attr.ValidatorNameSyntaxRef.GetLocation(),
                attr.ValidatorName
            )
        );
    }

    ValidatorInfo GetValidatorCore(ValidateWithAttribute attr, ITypeSymbol argType) {
        var memberSymbolInfo = _model.GetSymbolInfo(attr.ValidatorNameSyntaxRef.GetSyntax());

        if (memberSymbolInfo.Symbol is not null) {
            var symbol = memberSymbolInfo.Symbol;

            // we don't need to check for methods, since those would be rejected by
            // GetSymbolInfo since the expression technically refers to a method group

            if (symbol is not IPropertySymbol propSymbol)
                goto COULDNT_FIND_VALIDATOR;

            var validator = GetValidatorFromProperty(propSymbol, attr, argType);

            if (validator is not null)
                return validator;
        } else if (memberSymbolInfo.CandidateReason == CandidateReason.MemberGroup) {
            foreach (var symbol in memberSymbolInfo.CandidateSymbols) {
                // we should only be getting methods here, cf note above
                if (symbol is not IMethodSymbol methodSymbol)
                    goto COULDNT_FIND_VALIDATOR;

                var validator = GetValidatorFromMethod(methodSymbol, attr, argType);

                if (validator is not null)
                    return validator;
            }

            return new ValidatorInfo.Invalid(
                Diagnostic.Create(
                    Diagnostics.NoValidValidatorMethod,
                    attr.ValidatorNameSyntaxRef.GetLocation(),
                    attr.ValidatorName, argType.GetErrorName()
                )
            );
        }

    COULDNT_FIND_VALIDATOR:
        return new ValidatorInfo.Invalid(
            Diagnostic.Create(
                Diagnostics.CouldntFindValidator,
                attr.ValidatorNameSyntaxRef.GetLocation(),
                attr.ValidatorName
            )
        );
    }

    ValidatorInfo? GetValidatorFromMethod(
        IMethodSymbol method,
        ValidateWithAttribute attr,
        ITypeSymbol argType
    ) {
        if (method.MethodKind != MethodKind.Ordinary)
            return null;

        if (method.Parameters.Length != 1)
            return null;

        if (!_implicitConversionsCache.GetValue(argType, method.Parameters[0].Type))
            return null;

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

    ValidatorInfo GetValidatorFromProperty(
        IPropertySymbol prop,
        ValidateWithAttribute attr,
        ITypeSymbol argType
    ) {
        if (!SymbolUtils.Equals(prop.Type, CommonTypes.BOOL)) {
            return new ValidatorInfo.Invalid(
                Diagnostic.Create(
                    Diagnostics.ValidatorPropertyReturnMismatch,
                    attr.ValidatorNameSyntaxRef.GetLocation(),
                    prop.GetErrorName()
                )
            );
        }

        if (!prop.ContainingType.IsBaseOf(argType)) {
            return new ValidatorInfo.Invalid(
                Diagnostic.Create(
                    Diagnostics.PropertyValidatorNotOnArgType,
                    attr.ValidatorNameSyntaxRef.GetLocation(),
                    prop.GetErrorName(), argType.GetErrorName()
                )
            );
        }

        return new ValidatorInfo.Property(prop.Name, MinimalMemberInfo.FromSymbol(prop));
    }
}