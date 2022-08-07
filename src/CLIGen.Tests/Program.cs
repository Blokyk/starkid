using System.IO;

var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText("/home/blokyk/csharp/cli-gen/src/CLIGen.Sample/Parsex.cs"));

var attribAssemblyLoc = typeof(CLIGen.CLIAttribute).Assembly.Location;

var unit = CSharpCompilation.Create(
    "Tests",
    syntaxTrees: new[] { syntaxTree },
    references: new [] {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(attribAssemblyLoc)
    }
);

var driver = CSharpGeneratorDriver.Create(new CLIGen.Generator.MainGenerator()).RunGenerators(unit);

var driverResults = driver.GetRunResult().Results;

foreach (var result in driverResults) {
    if (result.Diagnostics.Length != 0) {
        Console.WriteLine("Generated the following diagnostics :");
        Console.WriteLine(String.Join("\t\n", result.Diagnostics.Select(d => d.Descriptor.Title.ToString())));
    }

    Console.WriteLine("Copying " + result.GeneratedSources.Length + " results to out/");

    foreach (var src in result.GeneratedSources) {
        File.WriteAllText("obj/out/" + src.HintName, src.SourceText.ToString());
    }
}