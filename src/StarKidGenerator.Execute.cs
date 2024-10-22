using Microsoft.CodeAnalysis.Diagnostics;

using StarKid.Generator.CodeGeneration;
using StarKid.Generator.CommandModel;
using StarKid.Generator.SymbolModel;

namespace StarKid.Generator;

public partial class StarKidGenerator
{
    static void GenerateParserAndHandlers(Group rootGroup, ImmutableArray<string> usings, StarKidConfig config, SourceProductionContext spc) {
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