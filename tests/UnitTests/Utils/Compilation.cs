using StarKid.Generator;

namespace StarKid.Tests;

internal static class Compilation
{
    public static CSharpCompilation From(string source) => From(source, ParseOptions.Default);
    public static CSharpCompilation From(string source, CSharpParseOptions parseOptions) => From(source, parseOptions, CompilationOptions.DefaultConsole);
    public static CSharpCompilation From(string source, CSharpCompilationOptions options) => From(source, ParseOptions.Default, options);
    public static CSharpCompilation From(string source, CSharpParseOptions parseOptions, CSharpCompilationOptions compOptions) => From(CSharpSyntaxTree.ParseText(source, parseOptions), compOptions);

    public static GeneratorDriverRunResult RunStarKid(this CSharpCompilation comp)
        => CSharpGeneratorDriver.Create(
            [new StarKidGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        ).RunGenerators(comp).GetRunResult();

    public static readonly CSharpSyntaxTree _attribTree
        = SyntaxTree.Of(StarKidGenerator._attributeCode);
    public static CSharpCompilation WithoutStarKidAttributes(this CSharpCompilation comp)
        => comp.RemoveSyntaxTrees([_attribTree]);

    public static CSharpCompilation From(RS.SyntaxTree tree) => From(tree, CompilationOptions.DefaultConsole);
    public static CSharpCompilation From(
        RS.SyntaxTree tree,
        CSharpCompilationOptions options
    ) => CSharpCompilation.Create(
        "Tests",
        syntaxTrees: [ tree, _attribTree ],
        references: [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.IO.FileInfo).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location)
        ],
        options: options
    );

    public static SemanticModel GetDefaultSemanticModel(this CSharpCompilation comp) => comp.GetSemanticModel(comp.SyntaxTrees[0], false);
}