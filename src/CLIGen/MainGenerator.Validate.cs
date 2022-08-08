using System.Text;
using System.Collections.Immutable;

using CLIGen;

namespace CLIGen.Generator;

public partial class MainGenerator : IIncrementalGenerator
{
    static bool Validate(Compilation compilation, ClassDeclarationSyntax[] classes, SourceProductionContext context, out SemanticModel model, out INamedTypeSymbol classSymbol) {
        model = null!;
        classSymbol = null!;

        if (classes.Length > 1) {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CG001",
                    "Multiple classes with CLI attribute",
                    "Both {0} and {1} classes are marked with [CLI], but only one is allowed",
                    "Blokyk.CLIGen",
                    DiagnosticSeverity.Error,
                    true
                ),
                classes.First().GetLocation(),
                classes[0].Identifier, classes[1].Identifier
            ));

            return false;
        }

        model = compilation.GetSemanticModel(classes[0].SyntaxTree);
        classSymbol = model.GetDeclaredSymbol(classes[0])!;

        if (classSymbol is null) {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CG002",
                    "Couldn't get type symbol for class {0}",
                    "Couldn't get INamedTypeSymbol for class declaration with name {0}",
                    "Blokyk.CLIGen",
                    DiagnosticSeverity.Error,
                    true
                ),
                classes[0].GetLocation(),
                classes[0].Identifier
            ));

            return false;
        }

        if (!classSymbol.IsStatic || classSymbol.IsGenericType) {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CG002",
                    "CLI class needs to be an internal non-generic static class",
                    "Class {0} is marked [CLI] but doesn't meet non-generic static class requirements",
                    "Blokyk.CLIGen",
                    DiagnosticSeverity.Error,
                    true
                ),
                classSymbol.Locations.First(),
                classSymbol.ToDisplayString()
            ));

            return false;
        }

        /*
        * GetEntryPoint takes too much time
        var potentialEntry = compilation.GetEntryPoint(CancellationToken.None);

        if (potentialEntry is not null && potentialEntry.ContainingNamespace.ToDisplayString() != Ressources.GenNamespace) {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CG003",
                    "Compilation already contains an entry point",
                    "'{0}' is already the entry point for this project. Please rename it so the generator can insert its own",
                    "Blokyk.CLIGen",
                    DiagnosticSeverity.Error,
                    true
                ),
                potentialEntry.Locations.First(),
                potentialEntry.ContainingType.Name + "." + potentialEntry.Name
            ));

            return false;
        }*/

        return true;
    }
}