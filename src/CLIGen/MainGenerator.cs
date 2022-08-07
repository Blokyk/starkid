using System.IO;
using System.Text;

using CLIGen;

namespace CLIGen.Generator;

[Generator]
public partial class MainGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(
            static postInitCtx => {
                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_Utils.g.cs",
                    SourceText.From(Ressources.UtilsClassStr, Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_Program.g.cs",
                    SourceText.From(Ressources.ProgClassStr, Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_CLIAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen.Attributes/CLIAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_CommandAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen.Attributes/CommandAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_DescriptionAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen.Attributes/DescriptionAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_OptionAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen.Attributes/OptionAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_SubCommandAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen.Attributes/SubCommandAttribute.cs"), Encoding.UTF8)
                );
            }
        );

        var cliClassDec = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => HasAnyAttributes(node),
                static (ctx, _) => GetCLIClass(ctx)
            )
            .Where((syntax) => syntax is not null);

        // Combine the selected classes with the `Compilation`
        var compilationAndClasses
            = context.CompilationProvider.Combine(cliClassDec.Collect());

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2!, spc));
    }

    static bool HasAnyAttributes(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0};

    static INamedTypeSymbol? GetCLIClass(GeneratorSyntaxContext ctx) {
        var node = (ClassDeclarationSyntax)ctx.Node;

        var classDec = ctx.SemanticModel.GetDeclaredSymbol(node);

        if (classDec is null)
            throw new Exception();

        foreach (var attr in classDec.GetAttributes()) {
            if (attr.AttributeClass?.Name == Ressources.CLIAttribName)
                return classDec;
        }

        return null;
    }
}