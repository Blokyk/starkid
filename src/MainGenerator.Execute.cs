using Microsoft.CodeAnalysis.Diagnostics;
using StarKid.Generator.Model;

namespace StarKid.Generator;

public partial class MainGenerator
{
    static void GenerateFromData(Group? rootGroup, ImmutableArray<string> usings, AnalyzerConfigOptions analyzerConfig, LanguageVersion langVersion, SourceProductionContext spc) {
        if (rootGroup is null)
            return;

        var config = ParseConfig(analyzerConfig, langVersion, spc);

        var cmdDescCode = CodeGenerator.ToSourceCode(rootGroup, usings, config);

        spc.AddSource(
            "StarKid_CmdDescDynamic.g.cs",
            SourceText.From(cmdDescCode, Encoding.UTF8)
        );

        spc.AddSource(
            "StarKid_StarKidProgram.g.cs",
            SourceText.From(_starkidProgramCode, Encoding.UTF8)
        );

        SymbolInfoCache.FullReset();
    }
}