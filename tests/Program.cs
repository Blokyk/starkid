using System.IO;
using Recline.Generator;

var sampleDir = "../sample/";
var testDir = "../raw-sample/";

string parsexPath = "Parsex.cs", parsex2Path = "Parsex-2.cs.old", dotnetPath = "Dotnet.cs.old";

var parsexTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + parsexPath));
var parsex2Tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + parsex2Path));
var dotnetTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + dotnetPath));

var generator = new MainGenerator();

var unit = CSharpCompilation.Create(
    "Tests",
    syntaxTrees: new[] { parsexTree },
    references: new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
    },
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary/*, specificDiagnosticOptions: new KeyValuePair<string, ReportDiagnostic>[] {
        KeyValuePair.Create("CLI000", ReportDiagnostic.Suppress)
    }*/)
);

var driver = CSharpGeneratorDriver.Create(
    new[] { generator.AsSourceGenerator() },
    driverOptions: new GeneratorDriverOptions(
        disabledOutputs: IncrementalGeneratorOutputKind.None,
        trackIncrementalGeneratorSteps: true
    )
);

void runDriver(string phase, string filename, CSharpCompilation unit) {
    Console.WriteLine("\x1b[33m  " + phase + " -- " + filename[..filename.IndexOf('.')].ToLowerInvariant() + "\x1b[0m");

    var genRun = driver.RunGenerators(unit);
    var results = genRun.GetRunResult().Results[0];

    foreach (var diag in results.Diagnostics) {
        Console.WriteLine(diag.FormatSeverity() + diag.GetMessage());
    }

    var errorCount = results.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error) + (results.Exception is not null ? 1 : 0);

    if (errorCount != 0) {
        Console.WriteLine("\x1b[31mThere were " + errorCount + " errors.\x1b[0m");
    } else {
        Console.WriteLine("Successfully generated " + results.GeneratedSources.Length + " files.");

        if (filename[^4..] != ".old") {
            File.Copy(sampleDir + filename, testDir + "Main.cs", true);

            foreach (var src in results.GeneratedSources) {
                using var writer = new StreamWriter(testDir + src.HintName);

                src.SourceText.Write(writer);
            }
        }
    }

    Console.WriteLine($"Total: {genRun.GetTimingInfo().GeneratorTimes[0].ElapsedTime.TotalMilliseconds:0.00} ms");
}

runDriver("init", parsexPath, unit);
runDriver("edit", parsex2Path, unit.ReplaceSyntaxTree(parsexTree, parsex2Tree));
runDriver("paste", dotnetPath, unit.ReplaceSyntaxTree(parsexTree, dotnetTree));