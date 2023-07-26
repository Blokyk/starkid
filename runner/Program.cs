using System.Collections.Immutable;
using System.IO;
using Recline.Generator;

var sampleDir = "../sample/";
var testDir = "../raw-sample/";

string  parsexPath = "Parsex.cs",
        parsex2Path = "Parsex-2.cs",
        dotnetPath = "Dotnet.cs",
        lotusPath = "Lotus.cs"
        ;

var parsexTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + parsexPath));
var parsex2Tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + parsex2Path));
var dotnetTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + dotnetPath));
var lotusTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sampleDir + lotusPath));

var generator = new MainGenerator();

GeneratorDriver driver = CSharpGeneratorDriver.Create(
    new[] { generator.AsSourceGenerator() },
    driverOptions: new GeneratorDriverOptions(
        disabledOutputs: IncrementalGeneratorOutputKind.None,
        trackIncrementalGeneratorSteps: true
    )
);

var unit = createCompUnit("Parsex", parsexTree);
var dotnetUnit = createCompUnit("Dotnet", dotnetTree);
var lotusUnit = createCompUnit("Lotus", lotusTree);

runDriver("init", parsexPath, unit);
runDriver("edit", parsex2Path, unit.ReplaceSyntaxTree(parsexTree, parsex2Tree));
runDriver("paste", dotnetPath, unit.ReplaceSyntaxTree(parsexTree, dotnetTree));
runDriver("new", lotusPath, lotusUnit);
runDriver("redo", lotusPath, lotusUnit);

/// <summary>
/// Runs the generator in the given unit, and, if filename doesn't
/// end with '.old', copies the original file and the result into $testDir
/// </summary>
void runDriver(string phase, string filename, CSharpCompilation unit) {
    Console.WriteLine("\x1b[33m  " + phase + " -- " + filename[..filename.IndexOf('.')].ToLowerInvariant() + "\x1b[0m");

    if (args.Contains("--profile")) {
        Console.WriteLine("Press any key to launch this run... \nIf you want to attach, we're PID " + Environment.ProcessId);
        Console.ReadKey(intercept: true);
        Console.CursorTop -= 2; // go back to the previous lines, so that we'll erase the message
    }

    driver = driver.RunGenerators(unit);
    var results = driver.GetRunResult().Results[0];

    Utils.DisplayReclineSteps(results);

    foreach (var diag in driver.GetRunResult().Diagnostics) {
        Console.WriteLine(diag.FormatSeverity() + diag.GetMessage());
    }

    var errorCount = results.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

    if (results.Exception is not null) {
        Console.WriteLine("\x1b[31mThere was an exception: \"\x1b[1m" + results.Exception.Message + "\"\x1b[0m");
        Console.WriteLine("Stack trace:");
        Console.WriteLine(results.Exception.StackTrace);
    } else if (errorCount != 0) {
        Console.WriteLine("\x1b[31mThere were " + errorCount + " errors.\x1b[0m");
    } else {
        Console.WriteLine("Successfully generated " + results.GeneratedSources.Length + " files.");

        if (filename[^4..] != ".old") {
            foreach (var path in Directory.EnumerateFiles(testDir, "*.g.cs"))
                File.Delete(path);

            File.Copy(sampleDir + filename, testDir + "Main.cs", true);

            foreach (var src in results.GeneratedSources) {
                using var writer = new StreamWriter(testDir + src.HintName);

                src.SourceText.Write(writer);
            }
        }
    }

    Console.WriteLine($"Total: {driver.GetTimingInfo().GeneratorTimes[0].ElapsedTime.TotalMilliseconds:0.00} ms");
    Console.WriteLine();
}

static CSharpCompilation createCompUnit(string assemblyName, SyntaxTree tree) {
    return CSharpCompilation.Create(
        assemblyName,
        syntaxTrees: new[] { tree },
        references: new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        },
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithSpecificDiagnosticOptions(new[] {KeyValuePair.Create("CLI008", ReportDiagnostic.Suppress)})
    );
}