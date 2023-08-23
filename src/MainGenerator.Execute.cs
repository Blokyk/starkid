using Microsoft.CodeAnalysis.Diagnostics;
using Recline.Generator.Model;

namespace Recline.Generator;

public partial class MainGenerator
{
    static void GenerateFromData(Group? rootGroup, ImmutableArray<string> usings, AnalyzerConfigOptions analyzerConfig, LanguageVersion langVersion, SourceProductionContext spc) {
        if (rootGroup is null)
            return;

        var config = ParseConfig(analyzerConfig, langVersion, spc);

        var cmdDescCode = CodeGenerator.ToSourceCode(rootGroup, usings, config);

        spc.AddSource(
            "Recline_CmdDescDynamic.g.cs",
            SourceText.From(cmdDescCode, Encoding.UTF8)
        );

        spc.AddSource(
            "Recline_ReclineProgram.g.cs",
            SourceText.From(_reclineProgramCode, Encoding.UTF8)
        );

        SymbolInfoCache.FullReset();
    }
}