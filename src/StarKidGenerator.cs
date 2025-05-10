using System.Diagnostics;

namespace StarKid.Generator;

[Generator(LanguageNames.CSharp)]
public partial class StarKidGenerator : IIncrementalGenerator
{
    private const string _cmdGroupAttributeName = "StarKid.CommandGroupAttribute";
    private const string _cmdAttributeName = "StarKid.CommandAttribute";
    private const string _optAttributeName = "StarKid.OptionAttribute";
    private const string _parseAttributeName = "StarKid.ParseWithAttribute";
    private const string _validateAttributeName = "StarKid.ValidateWithAttribute";
    private const string _validatePropAttributeName = "StarKid.ValidatePropAttribute";

    internal static readonly string _starkidProgramCode;

    static StarKidGenerator() {
        _starkidProgramCode = MiscUtils.GetStaticResource("StarKidProgram.cs");
        _attributeCode = MiscUtils.GetStaticResource("StarKidAttributes.cs");
    }

    internal static readonly string _attributeCode;

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        if (Debugger.IsAttached)
            Debugger.Break();

        #if DEBUG
            var fawmnWarmup
                = context
                    .SyntaxProvider
                    .ForAttributeWithMetadataName(
                        _cmdGroupAttributeName,
                        (node, _) => node is ClassDeclarationSyntax,
                        (_, _) => 0
                    )
                    .Collect()
                    .WithTrackingName("fawmn_warmup");

            context.RegisterSourceOutput(fawmnWarmup, (_, _) => { });
        #endif

        context.RegisterPostInitializationOutput(
            static postInitCtx =>
                postInitCtx.AddSource(
                    "StarKid_Attributes.g.cs",
                    SourceText.From(_attributeCode, Encoding.UTF8)
                )
        );

        var usingsSource = GetUsedNamespacesProvider(context).Collect().WithTrackingName("starkid_usings");

        var starkidConfigSource = GetStarKidConfigProvider(context).WithTrackingName("starkid_config");

        // todo: separate this into three pipelines:
        //      - ParseWith resolution with FAWMN (returns a map between symbol name and parser)
        //      - same with ValidateWith
        //      - Group building, like before
        // We can then bind parsers back to their original symbols by going through the whole
        // command tree (maybe have an variable for like "needs a parser/validator" so that
        // we don't lookup *every single symbol name*)
        //
        // do we also want to separate auto-parsers? the problem is that we need to do the group
        // building first, but we could the ParseWith
        // part after group_building, and have group_building output a list of stuff that needs
        // an auto-parser
        //
        // todo: we could also make another pipeline to get the xml docs and use a similar strategy to merge back
        // we'd have to do that for every symbol with either [Command], [CommandGroup], or [Option]
        var groupsSource
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _cmdGroupAttributeName,
                    (node, _) => node is ClassDeclarationSyntax,
                    (ctx, _) =>
                        DataOrDiagnostics.From(addDiag =>
                            CreateGroup((INamedTypeSymbol)ctx.TargetSymbol, ctx.SemanticModel, addDiag)
                        )
                )
                .WithTrackingName("starkid_group_building");

        var groupTreeSource
            = groupsSource
                .Collect()
                .Select(
                    (groups, addDiag, _) =>
                        BindGroupTree(groups, addDiag)
                )
                .WithTrackingName("starkid_binding");

        // generates the parser + command infos (`CmdDesc` classes)
        context.RegisterImplementationSourceOutput(
            groupTreeSource
                .Combine(usingsSource)
                .Combine(starkidConfigSource),
            static (spc, groupTreeAndUsingsAndConfig)
                => GenerateParserAndHandlers(
                    groupTreeAndUsingsAndConfig.Left.Left,  // root group
                    groupTreeAndUsingsAndConfig.Left.Right, // usings
                    groupTreeAndUsingsAndConfig.Right,      // config
                    spc
                ));

        var allInvokables
            = groupsSource
                .SelectMany(
                    (rootGroup, _, _)
                        => InvokableUtils.TraverseInvokableTree(rootGroup)
                ).WithTrackingName("starkid_traverse_invokable");

        // generates help text from (bound) invokables
        // we need them to be bounded so that we now the full ID
        // of each invokable, and thus its CmdDesc class's name
        context.RegisterImplementationSourceOutput(
            allInvokables.Combine(starkidConfigSource),
            static (spc, invokableAndConfig) =>
                GenerateHelpText(
                    invokableAndConfig.Left,  // invokable (group or cmd)
                    invokableAndConfig.Right, // config
                    spc
                )
        );

        // context.RegisterDiagnosticSource(usingsSource);
        context.RegisterDiagnosticSource(groupTreeSource);
        context.RegisterDiagnosticSource(starkidConfigSource);
    }

    /// <summary>
    /// Creates a new IVsP<string> listing all the namespaces used
    /// </summary>
    private IncrementalValuesProvider<string> GetUsedNamespacesProvider(IncrementalGeneratorInitializationContext context) {
        var groupUsings
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _cmdGroupAttributeName, // fixme: we also need to check commands
                    (node, _) => node is ClassDeclarationSyntax,
                    (ctx, _) => SyntaxUtils.GetReachableNamespaceNames(ctx.TargetNode)
                )
                .SelectMany((arr, _) => arr);

        var cmdUsings
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    "CommandAttribute",
                    (node, _) => node is MethodDeclarationSyntax,
                    (ctx, _) => SyntaxUtils.GetReachableNamespaceNames(ctx.TargetNode)
                )
                .SelectMany((arr, _) => arr);

        return groupUsings.Concat(cmdUsings);
    }

    private IncrementalValueProvider<DataOrDiagnostics<StarKidConfig>> GetStarKidConfigProvider(IncrementalGeneratorInitializationContext context) {
        var langVersionSource
            = context
                .CompilationProvider
                .Select(
                    (comp, _) => (comp as CSharpCompilation)?.LanguageVersion ?? LanguageVersion.Default
                );

        var csprojOptionsSource
            = context.AnalyzerConfigOptionsProvider.Select((opts, _) => opts.GlobalOptions);

        return
            csprojOptionsSource.Combine(langVersionSource)
                .Select(
                    static (combined, _) =>
                        DataOrDiagnostics.From(
                            addDiag => StarKidConfig.Parse(
                                combined.Left, // analyzer config
                                combined.Right, // lang version
                                addDiag
                            )
                        )
                );
    }
}