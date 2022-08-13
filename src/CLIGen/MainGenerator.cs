using System.IO;
using System.Text;
using System.Collections.Immutable;

using CLIGen;

namespace CLIGen.Generator;

[Generator]
public partial class MainGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(
            static postInitCtx => {
                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_Program.g.cs",
                    SourceText.From(Ressources.ProgClassStr, Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_CLIAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen/Static/Attributes/CLIAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_CommandAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen/Static/Attributes/CommandAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_DescriptionAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen/Static/Attributes/DescriptionAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_OptionAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen/Static/Attributes/OptionAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_SubCommandAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen/Static/Attributes/SubCommandAttribute.cs"), Encoding.UTF8)
                );
            }
        );

        var cliClassDec = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => HasAnyAttributes(node),
                static (ctx, _) => GetCLIClass(ctx)
            )
            .Collect();

        // Combine the selected classes with the `Compilation`
        var compilationAndClasses = context.CompilationProvider.Combine(cliClassDec);

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2!, spc));
    }

    static bool HasAnyAttributes(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0};

    static ClassDeclarationSyntax? GetCLIClass(GeneratorSyntaxContext ctx) {
        var node = (ClassDeclarationSyntax)ctx.Node;

        foreach (var attr in node.AttributeLists.SelectMany(l => l.Attributes)) {
            if (Utils.GetLastNamePart(attr.Name.ToString().AsSpan()) is "CLIAttribute" or "CLI")
                return node;
        }

        return null;
    }

    public class INamedTypeSymbolComparer
        : IEqualityComparer<INamedTypeSymbol?>
    {
        public static readonly INamedTypeSymbolComparer Default = new();

        public bool Equals(
           INamedTypeSymbol? x,
           INamedTypeSymbol? y) {
            return SymbolEqualityComparer.Default.Equals(x, y);
        }

        public int GetHashCode(INamedTypeSymbol? obj) {
            return SymbolEqualityComparer.Default.GetHashCode(obj);
        }
    }

    public class CompSymbolComparer
   : IEqualityComparer<(Compilation Left, INamedTypeSymbol? Right)>
    {
        public static readonly CompSymbolComparer Default = new();

        public bool Equals(
           (Compilation Left, INamedTypeSymbol? Right) x,
           (Compilation Left, INamedTypeSymbol? Right) y) {
            return SymbolEqualityComparer.Default.Equals(x.Right, y.Right);
        }

        public int GetHashCode((Compilation Left, INamedTypeSymbol? Right) obj) {
            return SymbolEqualityComparer.Default.GetHashCode(obj.Right);
        }
    }
}