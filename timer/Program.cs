using System.Collections.Immutable;
using System.IO;

using StarKid.Generator;

internal static class Program
{
    private static void Main(string[] args) {
        var sampleDir = "../sample/";

        var n = 25_000;

        string[] sampleNames = [
            "Parsex",
            "Parsex-2",
            "Dotnet",
            "Stuff",
            "Lotus",
        ];

        var samples = sampleNames.Select(name => {
            var path = sampleDir + name + ".cs";
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
            var unit = CreateCompUnit(name, tree);
            return new Sample(name, path, tree, unit);
        }).ToArray();

        var generator = new StarKidGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        );

        RunDriver(samples[0], ref driver); // warmup run
        var randomSamples = Random.Shared.GetItems(samples, n);

        var allResults = new List<StarKidResults>(n);
        for (int i = 0; i < randomSamples.Length; i++) {
            allResults.Add(RunDriver(randomSamples[i], ref driver));

            ReportProgress(i, n);
        }
        Console.WriteLine();

        Console.WriteLine(new string('-', Console.BufferWidth));

        var allSteps = allResults.SelectMany(res => res.Steps).ToArray();

        var stepGroups = allSteps.GroupBy(s => s.Name);

        foreach (var stepGroup in stepGroups.OrderBy(group => group.Key)) {
            Console.WriteLine($"\e[2mMedian \e[0m{stepGroup.Key[8..]}\e[2m runtime: \e[0m{stepGroup.Median(GetStepMilliseconds):0.000}ms");
        }

        Console.WriteLine(new string('-', Console.BufferWidth));

        Console.WriteLine($"\e[2mMedian \e[0mtotal\e[2m runtime: \e[0m{allResults.Median(s => s.TotalTime.TotalMilliseconds):0.000}ms");

        var overhead = allResults.Median(static result => result.TotalTime.TotalMilliseconds - result.Steps.Sum(GetStepMilliseconds));
        Console.WriteLine($"\e[2mMedian \e[0mroslyn overhead: {overhead:0.000}ms");
    }

    // a workaround for CS9236
    static double GetStepMilliseconds(Step s) => s.ElapsedTime.TotalMilliseconds;

    static StarKidResults RunDriver(Sample sample, ref GeneratorDriver driver) {
        driver = driver.RunGenerators(sample.Compilation);

        var results = driver.GetRunResult().Results.First(r => r.Generator.GetGeneratorType() == typeof(StarKidGenerator));
        var steps = GetStarKidSteps(results);

        var totalTime = driver.GetTimingInfo().GeneratorTimes.First(r => r.Generator.GetGeneratorType() == typeof(StarKidGenerator)).ElapsedTime;

        return new StarKidResults(sample, totalTime, steps);
    }

    static CSharpCompilation CreateCompUnit(string assemblyName, SyntaxTree tree)
        => CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [tree],
            references: [
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(FileInfo).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                        .WithSpecificDiagnosticOptions([KeyValuePair.Create("CLI006", ReportDiagnostic.Suppress)])
        );

    static ImmutableArray<Step> GetStarKidSteps(GeneratorRunResult results) {
        var steps = results.TrackedSteps.Where(kv => kv.Key.StartsWith("starkid_"));

        if (results.TrackedOutputSteps.TryGetValue("ImplementationSourceOutput", out var outputStep))
            steps = steps.Append(new("starkid_output", outputStep));

        return [..steps.Select(kv
            => new Step(
                kv.Key,
                kv.Value.Aggregate(TimeSpan.Zero, (acc, step) => acc + step.ElapsedTime)
            ))];
    }

    static void ReportProgress(int current, int total) {
        var precision = total / 10000;
        if (precision == 0 || current % precision == 0)
            UpdateConsoleLine($"Progress: {(current+1) / (float)total * 100:0.00}%");
    }

    static void UpdateConsoleLine(object text) {
        if (Console.IsOutputRedirected) {
            Console.WriteLine(text);
            return;
        }

        Console.CursorLeft = 0;
        Console.Write(new string(' ', Console.BufferWidth - 1));
        Console.CursorLeft = 0;
        Console.Write(text);
    }

    static double Median<T>(this IEnumerable<T> sequence, Func<T, double> selector) {
        if (!sequence.Any()) return 0;

        var ordered = sequence.Select(selector).Order();
        return ordered.ElementAt(ordered.Count() / 2);
    }
}

record Sample(string Name, string Path, SyntaxTree Tree, CSharpCompilation Compilation);
record Step(string Name, TimeSpan ElapsedTime);
record StarKidResults(Sample Sample, TimeSpan TotalTime, ImmutableArray<Step> Steps);
