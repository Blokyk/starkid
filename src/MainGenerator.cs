using System.IO;
using System.Diagnostics;

using Recline.Generator.Model;

namespace Recline.Generator;

[Generator(LanguageNames.CSharp)]
public partial class MainGenerator : IIncrementalGenerator
{
    private const string staticFolderPath = "/home/blokyk/csharp/recline/src/Static/";
    private static readonly string[] _staticFilenames = new string[] {};

    private static readonly string[] _attributeNames = new[] {
        nameof(CLIAttribute),
        nameof(CommandAttribute),
        nameof(DescriptionAttribute),
        nameof(OptionAttribute),
        nameof(SubCommandAttribute),
        nameof(ParseWithAttribute),
    };

    public static double postInitMS = -1;
    public static double analysisMS = -1;
    public static double codegenMS = -1;

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(
            static postInitCtx => {
                // FIXME: load stuff from const strings when everything is stable

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                foreach (var filename in _staticFilenames) {
                    postInitCtx.AddSource(
                        Resources.GenNamespace + "_" + filename + ".g.cs",
                        SourceText.From(File.ReadAllText(staticFolderPath + filename + ".cs"), Encoding.UTF8)
                    );
                }

                var sb = new StringBuilder();

                sb.AppendLine(@"
#define GEN
#nullable enable

using System;

namespace Recline;

");

                foreach (var filename in _attributeNames) {
                    sb.AppendLine(File.ReadAllText(staticFolderPath + "Attributes/" + filename + ".cs"));
                }

                postInitCtx.AddSource(
                    Resources.GenNamespace + "_Attributes.g.cs",
                    SourceText.From(sb.ToString(), Encoding.UTF8)
                );
                watch.Stop();
                postInitMS = watch.Elapsed.TotalMilliseconds;
            }
        );

        var cliDataSource
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    "Recline.CLIAttribute",
                    static (node, _) => HasAnyAttributes(node),
                    static (ctx, _) => {
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        var res = GetCmdData(ctx);
                        watch.Stop();
                        analysisMS = watch.Elapsed.TotalMilliseconds;
                        return res;
                    }
                )
                .WithComparer(new CLIDataComparer())
                .WithTrackingName("recline_clidata")
                .Collect();

        var analysisOpts
            = context
                .AnalyzerConfigOptionsProvider
                .WithTrackingName("recline_options")
                .Select(
                    (opts, _) => {
                        if (!opts.GlobalOptions.TryGetValue("build_property.Recline_ColumnLength", out var columnLengthStr))
                            return 80;

                        if (!Int32.TryParse(columnLengthStr, out var columnLength))
                            return 0;

                        return columnLength;
                    }
                );

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(cliDataSource.Combine(analysisOpts),
            static (spc, cliDataAndOpts) => GenerateFromData(cliDataAndOpts.Left, cliDataAndOpts.Right, spc));
    }

    static bool HasAnyAttributes(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    static (CLIData? data, ImmutableArray<Diagnostic> diags) GetCmdData(GeneratorAttributeSyntaxContext ctx) {
        var sw = new Stopwatch();
        sw.Start();

        var model = ctx.SemanticModel;
        CommonTypes.Refresh(model.Compilation);

        var attribParser = new AttributeParser();

        static (CLIData? data, ImmutableArray<Diagnostic> diags) bail(ModelBuilder modelBuilder) {
            var data = modelBuilder.MakeCLIData(out var diags);
            CommonTypes.Clear();
            return (data, diags);
        }

        var classSymbol = (ctx.TargetSymbol as INamedTypeSymbol)!;

        if (!ModelBuilder.TryCreateFromSymbol(classSymbol, attribParser, model, out var modelBuilder))
            return bail(modelBuilder);

        var members = classSymbol.GetMembers().Where(m => m.Kind is SymbolKind.Field or SymbolKind.Property or SymbolKind.Method).ToArray();

        foreach (var member in members) {
            if (!modelBuilder.TryAdd(member))
                return bail(modelBuilder);
        }

        var data = modelBuilder.MakeCLIData(out var diags);
        CommonTypes.Clear();
        return (data, diags);
    }
}

internal class CLIDataComparer : EqualityComparer<(CLIData? data, ImmutableArray<Diagnostic> diags)>
{
    public override bool Equals((CLIData? data, ImmutableArray<Diagnostic> diags) x, (CLIData? data, ImmutableArray<Diagnostic> diags) y)
        => x.diags.IsDefaultOrEmpty
        && y.diags.IsDefaultOrEmpty
        && CLIData.Equals(x.data, y.data);

    public override int GetHashCode((CLIData? data, ImmutableArray<Diagnostic> diags) obj)
        => obj.data?.GetHashCode() ?? 0;
}