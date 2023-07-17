using Microsoft.CodeAnalysis.Diagnostics;
using Recline.Generator.Model;

namespace Recline.Generator;

public partial class MainGenerator
{
    static void GenerateFromData(Group? rootGroup, ImmutableArray<string> usings, AnalyzerConfigOptions analyzerConfig, LanguageVersion langVersion, SourceProductionContext spc) {
        if (rootGroup is null)
            return;

        var config = ParseConfig(analyzerConfig, langVersion, spc);

        var usingsCode = CodegenHelpers.GenerateUsingsHeaderCode(usings);
        var cmdDescCode = CodeGenerator.ToSourceCode(rootGroup, config);

        spc.AddSource(
            Resources.GenNamespace + "_CmdDescDynamic.g.cs",
            SourceText.From(usingsCode + cmdDescCode, Encoding.UTF8)
        );

        spc.AddSource(
            Resources.GenNamespace + "_ReclineProgram.g.cs",
            SourceText.From(_reclineProgramCode, Encoding.UTF8)
        );

        CommonTypes.Reset();
        SymbolInfoCache.FullReset();
    }
}