using System.Text;
using System.Collections.Immutable;

using CLIGen;

namespace CLIGen.Generator;

public partial class MainGenerator : IIncrementalGenerator
{
    static bool Validate(Compilation compilation, ImmutableArray<INamedTypeSymbol> classes, SourceProductionContext context) {
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
                classes.First().Locations.First(),
                classes[0].ToDisplayString(), classes[1].ToDisplayString()
            ));

            return false;
        }

        if (!classes[0].IsStatic || classes[0].IsGenericType) {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CG002",
                    "CLI class needs to be an internal non-generic static class",
                    "Class {0} is marked [CLI] but doesn't meet non-generic static class requirements",
                    "Blokyk.CLIGen",
                    DiagnosticSeverity.Error,
                    true
                ),
                classes.First().Locations.First(),
                classes[0].ToDisplayString()
            ));

            return false;
        }

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
        }

        return true;
    }
}