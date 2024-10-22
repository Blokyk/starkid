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

    // note: if we recreate a new help generator each time, its cache will also reset every
    // time, so we could just remove it entirely
    static void GenerateHelpText(InvokableBase invokable, StarKidConfig config, SourceProductionContext spc)
        => spc.AddSource(
            "StarKid_" + invokable.ID + ".HelpText.g.cs",
            SourceText.From(HelpGenerator.ToSourceCode(invokable, config), Encoding.UTF8)
        );
}