namespace Recline.Tests;

internal static class Compilation
{
    public static CSharpCompilation From(string source) => From(source, ParseOptions.Default);
    public static CSharpCompilation From(string source, CSharpParseOptions parseOptions) => From(source, parseOptions, CompilationOptions.DefaultConsole);
    public static CSharpCompilation From(string source, CSharpCompilationOptions options) => From(source, ParseOptions.Default, options);
    public static CSharpCompilation From(string source, CSharpParseOptions parseOptions, CSharpCompilationOptions compOptions) => From(CSharpSyntaxTree.ParseText(source, parseOptions), compOptions);

    public static CSharpCompilation From(RS.SyntaxTree tree) => From(tree, CompilationOptions.DefaultConsole);
    public static CSharpCompilation From(
        RS.SyntaxTree tree,
        CSharpCompilationOptions options
    ) => CSharpCompilation.Create(
        "Tests",
        syntaxTrees: new[] { tree },
        references: new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.IO.FileInfo).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location)
        },
        options: options
    );
}