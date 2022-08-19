using System.IO;
using Recline.Generator;

var sampleDir = "../sample/";
var testDir = "../raw-sample/";

//var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText("/home/blokyk/csharp/sample-cli/Program.cs"));
var parsexTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + "Parsex.cs"));
var parsex2Tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + "Parsex-2.cs"));
var dotnetTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + "Dotnet.cs"));

var attribAssemblyLoc = typeof(Recline.CLIAttribute).Assembly.Location;

var generator = new MainGenerator();

var unit = CSharpCompilation.Create(
    "Tests",
    syntaxTrees: new[] { parsexTree },
    references: new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(attribAssemblyLoc)
    }
);

var driver = CSharpGeneratorDriver.Create(
    new[] { GeneratorExtensions.AsSourceGenerator(generator) },
    driverOptions: new GeneratorDriverOptions(
        disabledOutputs: IncrementalGeneratorOutputKind.None,
        trackIncrementalGeneratorSteps: true
    )
);

Console.WriteLine("\x1b[33m  init -- parsex\x1b[0m"); {

    var genRun = driver.RunGenerators(unit);
    var results = genRun.GetRunResult().Results[0];

    foreach (var diag in results.Diagnostics) {
        Console.WriteLine(diag.FormatSeverity() + diag.GetMessage());
    }

    var errorCount = results.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Count() + (results.Exception is not null ? 1 : 0);

    if (errorCount != 0) {
        Console.WriteLine("\x1b[31mThere were " + errorCount + " errors.\x1b[0m");
    } else {
        Console.WriteLine("Successfully generated " + results.GeneratedSources.Length + " files.");

        foreach (var src in results.GeneratedSources) {
            using var writer = new StreamWriter(testDir + src.HintName);

            src.SourceText.Write(writer);
        }
    }

    Console.WriteLine($"Total: {genRun.GetTimingInfo().GeneratorTimes[0].ElapsedTime.TotalMilliseconds:0.00} ms");
}

Console.WriteLine("\x1b[33m  mod -- parsex-2\x1b[0m"); {
    var newUnit = unit.ReplaceSyntaxTree(parsexTree, parsex2Tree);

    var genRun = driver.RunGenerators(newUnit);
    var results = genRun.GetRunResult().Results[0];

    foreach (var diag in results.Diagnostics) {
        Console.WriteLine(diag.FormatSeverity() + diag.GetMessage());
    }

    var errorCount = results.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Count() + (results.Exception is not null ? 1 : 0);

    if (errorCount != 0) {
        Console.WriteLine("\x1b[31mThere were " + errorCount + " errors.\x1b[0m");
    } else {
        Console.WriteLine("Successfully generated " + results.GeneratedSources.Length + " files.");

        foreach (var src in results.GeneratedSources) {
            using var writer = new StreamWriter(testDir + src.HintName);

            src.SourceText.Write(writer);
        }
    }

    Console.WriteLine($"Total: {genRun.GetTimingInfo().GeneratorTimes[0].ElapsedTime.TotalMilliseconds:0.00} ms");
}

Console.WriteLine("\x1b[33m  change -- dotnet\x1b[0m"); {
    var newUnit = unit.ReplaceSyntaxTree(parsexTree, dotnetTree);

    var genRun = driver.RunGenerators(newUnit);
    var results = genRun.GetRunResult().Results[0];

    foreach (var diag in results.Diagnostics) {
        Console.WriteLine(diag.FormatSeverity() + diag.GetMessage());
    }

    var errorCount = results.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Count() + (results.Exception is not null ? 1 : 0);

    if (errorCount != 0) {
        Console.WriteLine("\x1b[31mThere were " + errorCount + " errors.\x1b[0m");
    } else {
        Console.WriteLine("Successfully generated " + results.GeneratedSources.Length + " files.");

        foreach (var src in results.GeneratedSources) {
            using var writer = new StreamWriter(testDir + src.HintName);

            src.SourceText.Write(writer);
        }
    }

    Console.WriteLine($"Total: {genRun.GetTimingInfo().GeneratorTimes[0].ElapsedTime.TotalMilliseconds:0.00} ms");
}