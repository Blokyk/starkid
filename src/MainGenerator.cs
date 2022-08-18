using System.IO;
using System.Text;
using System.Collections.Immutable;
using System.Diagnostics;

using Recline;
using Recline.Generator.Model;

namespace Recline.Generator;

[Generator]
public partial class MainGenerator : IIncrementalGenerator
{
    public static long postInitMS = -1;
    public static long analysisMS = -1;
    public static long codegenMS = -1;

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(
            static postInitCtx => {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_Program.g.cs",
                    SourceText.From(Ressources.ProgClassStr, Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_CLIAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/recline/src/Static/Attributes/CLIAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_CommandAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/recline/src/Static/Attributes/CommandAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_DescriptionAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/recline/src/Static/Attributes/DescriptionAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_OptionAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/recline/src/Static/Attributes/OptionAttribute.cs"), Encoding.UTF8)
                );

                postInitCtx.AddSource(
                    Ressources.GenNamespace + "_SubCommandAttribute.g.cs",
                    SourceText.From(File.ReadAllText("/home/blokyk/csharp/recline/src/Static/Attributes/SubCommandAttribute.cs"), Encoding.UTF8)
                );
                watch.Stop();
                postInitMS = watch.ElapsedMilliseconds;
            }
        );

        var cliClassDec = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Recline.CLIAttribute",
                static (node, _) => HasAnyAttributes(node),
                static (ctx, _) => {
                    var watch = new System.Diagnostics.Stopwatch();
                    watch.Start();
                    var res = GetCmdBuilder(ctx);
                    watch.Stop();
                    analysisMS = watch.ElapsedMilliseconds;
                    return res;
                }
            )
            .Collect();

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(cliClassDec,
            static (spc, source) => Execute(source, spc));
    }

    static bool HasAnyAttributes(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0};

    static CLIData? GetCmdBuilder(GeneratorAttributeSyntaxContext ctx) {
        var sw = new Stopwatch();
        sw.Start();

        var declsBuilder = ImmutableArray.CreateBuilder<ClassDeclarationSyntax>();

        var model = ctx.SemanticModel;

        Utils.UpdatePredefTypes(model.Compilation);

        foreach (var syntaxRef in ctx.TargetSymbol.DeclaringSyntaxReferences) {
            declsBuilder.Add((syntaxRef.GetSyntax() as ClassDeclarationSyntax)!);
        }

        var nonNullClassSyntaxes = declsBuilder.ToImmutable();

        if (nonNullClassSyntaxes.Length == 0)
            return null;

        var classSymbol = (ctx.TargetSymbol as INamedTypeSymbol)!;

        var fullClassName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // var cliAttrib

        if (!classSymbol.TryGetAttribute(Ressources.CLIAttribName, out var cliAttrib))
            throw new Exception("Couldn't get CLI attribute for class " + classSymbol.Name);

        if (!AttributeParser.TryParseCLIAttrib(cliAttrib, out var cliAttr))
            throw new Exception("Couldn't parse CLI attribute on class " + classSymbol.Name);

        var (appName, entryPointName, helpExitCode) = cliAttr;

        string? appDesc = null;

        if (classSymbol.TryGetAttribute(Ressources.DescAttribName, out var descAttrib)) {
            if (!Utils.TryGetDescription(descAttrib, out appDesc))
                throw new Exception("Couldn't parse description for class " + classSymbol.Name);
        }

        var optList = new List<Option>();
        var cmdList = new List<Command>();

        var members = classSymbol.GetMembers().Where(m => m.Kind is SymbolKind.Field or SymbolKind.Property or SymbolKind.Method);

        foreach (var member in members) {
            if (!TryGetOptions(member, model, out var opt)) {
                throw new Exception("Couldn't parse symbol " + member.Name + " into an option");
            }

            if (opt is not null)
                optList.Add(opt);
        }

        var classMethods = members.OfType<IMethodSymbol>();

        foreach (var method in classMethods) {
            if (!TryGetCommand(method, model, out var cmd)) {
                throw new Exception("Couldn't parse symbol " + method.Name + " into a cmd");
            }

            if (cmd is not null)
                cmdList.Add(cmd);
        }

        var opts = optList.ToArray();
        var cmds = cmdList.ToArray();

        //TODO: check that every option and cmd has a unique name and/or alias

        Command? rootCmd = null;
        var posArgs = Array.Empty<Argument>();

        if (entryPointName is not null) {
            entryPointName = Utils.GetLastNamePart(entryPointName.AsSpan());

            // TODO: check that, if the entry point name is Main, then the main method
            // is marked partial without implementation. We also need to create a stub/trampoline
            // from $cliClassName to our Program's Main method

            rootCmd = cmds.FirstOrDefault(
                cmd => cmd.BackingSymbol.Name == entryPointName
            );

            if (rootCmd is null) {
                // technically, we could use classSymbol.GetMembers(entryPointName),
                // but that'd probably be slower

                // we don't actually have to use .ToArray here, we could just try to iterate
                // and error if we can't call MoveNext() exactly once. But that would be
                // an incredibly small optimisation

                var candidates = classMethods.Where(
                    m => m.Name == entryPointName
                ).ToArray();

                if (candidates.Length < 1)
                    throw new Exception("Could not find any method named " + entryPointName);
                else if (candidates.Length > 1)
                    throw new Exception(classSymbol.Name + " contains multiple methods named " + entryPointName);

                var method = candidates[0];

                if (!TryGetEntryPointCommand(method, model, out rootCmd))
                    throw new Exception("Couldn't parse method " + method.Name + " as an entry point");
            }

            if (rootCmd is null)
                throw new Exception("wtf??");

            if (rootCmd.Options.Length != 0)
                throw new Exception("Entry point cannot have parameters marked as options. Please use fields or properties to declare them.");

            rootCmd = rootCmd with {
                Name = appName,
                Description = appDesc ?? rootCmd.Description,
                ParentSymbolName = null
            };

            posArgs = rootCmd.Args;
        }

        for (int i = 0; i < cmds.Length; i++) {
            if (cmds[i].ParentSymbolName is not null) {
                if (!TryBindParentCmd(cmds[i], cmds, out var newCmd))
                    // Are you sure you marked '${cmds[i].ParentCmdName}' with [Command] ?
                    throw new Exception("Couldn't bind parent cmd '" + cmds[i].ParentSymbolName + "' of sub-cmd '" + cmds[i].Name + "'");

                cmds[i] = newCmd;
            } else if (rootCmd is not null) {
                cmds[i].ParentCmd = rootCmd;
            }
        }

        if (!TryGetAllUniqueUsings(classSymbol, out var usings))
            throw new Exception("Couldn't collect all usings for class " + classSymbol.Name);

        sw.Stop();
        analysisTime = sw.Elapsed;

        return new CLIData(
            appName,
            fullClassName,
            usings,
            rootCmd is null ? null : (rootCmd, posArgs),
            opts,
            appDesc,
            cmds,
            helpExitCode
        );
    }
}