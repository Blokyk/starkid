using System.Diagnostics;
using StarKid.Generator.AttributeModel;
using StarKid.Generator.CommandModel;
using StarKid.Generator.SymbolModel;

namespace StarKid.Generator;

[Generator(LanguageNames.CSharp)]
public partial class StarKidGenerator : IIncrementalGenerator
{
    private static readonly string _groupAttributeName = "StarKid.CommandGroupAttribute";
    private static readonly string _cmdAttributeName = "StarKid.CommandAttribute";
    private static readonly string _optAttributeName = "StarKid.OptionAttribute";
    private static readonly string _parseWithAttributeName = "StarKid.ParseWithAttribute";
    private static readonly string _validateWithAttributeName = "StarKid.ValidateWithAttribute";
    private static readonly string _validatePropAttributeName = "StarKid.ValidatePropAttribute";

    internal static readonly string _starkidProgramCode;

    static StarKidGenerator() {
        _starkidProgramCode = MiscUtils.GetStaticResource("StarKidProgram.cs");
        _attributeCode = MiscUtils.GetStaticResource("StarKidAttributes.cs");
    }

    internal static readonly string _attributeCode;

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        if (Debugger.IsAttached)
            Debugger.Break();

        context.RegisterPostInitializationOutput(
            static postInitCtx =>
                postInitCtx.AddSource(
                    "StarKid_Attributes.g.cs",
                    SourceText.From(_attributeCode, Encoding.UTF8)
                )
        );

        var langVersionSource
            = context
                .CompilationProvider
                .Select(
                    (comp, _) => (comp as CSharpCompilation)?.LanguageVersion ?? LanguageVersion.Default
                );

        #if DEBUG
            var fawmnWarmup
                = context
                    .SyntaxProvider
                    .ForAttributeWithMetadataName(
                        _groupAttributeName,
                        (node, _) => node is ClassDeclarationSyntax,
                        (_, _) => 0
                    )
                    .Collect()
                    .WithTrackingName("fawmn_warmup");

            context.RegisterSourceOutput(fawmnWarmup, (_, _) => { });
        #endif

        var usingsSource = GetUsedNamespacesProvider(context).Collect().WithTrackingName("starkid_usings");

        GetParsedAttributeProviders(context,
            out var groupAttrProvider,
            out var cmdAttrProvider,
            out var optAttrProvider,
            out var parseWithAttrProvider,
            out var validateWithAttrProvider,
            out var validatePropAttrProvider
        );
    }

    static void GetParsedAttributeProviders(
        IncrementalGeneratorInitializationContext context,
        out IncrementalValuesProvider<DataOrDiagnostics<(GeneratorAttributeSyntaxContext, CommandGroupAttribute)>> groupAttrProvider,
        out IncrementalValuesProvider<DataOrDiagnostics<(GeneratorAttributeSyntaxContext, CommandAttribute)>> cmdAttrProvider,
        out IncrementalValuesProvider<DataOrDiagnostics<(GeneratorAttributeSyntaxContext, OptionAttribute)>> optAttrProvider,
        out IncrementalValuesProvider<DataOrDiagnostics<(GeneratorAttributeSyntaxContext, ParseWithAttribute)>> parseWithAttrProvider,
        out IncrementalValuesProvider<DataOrDiagnostics<(GeneratorAttributeSyntaxContext, ValidateWithAttribute)>> validateWithAttrProvider,
        out IncrementalValuesProvider<DataOrDiagnostics<(GeneratorAttributeSyntaxContext, ValidatePropAttribute)>> validatePropAttrProvider
    ) {
        var isClass = static (SyntaxNode node, CancellationToken _)
            => node is ClassDeclarationSyntax;

        var isMethod = static (SyntaxNode node, CancellationToken _)
            => node is MethodDeclarationSyntax;

        var isMemberOrParam = static (SyntaxNode node, CancellationToken _)
            => node is PropertyDeclarationSyntax
                    or FieldDeclarationSyntax
                    or ParameterSyntax;

        groupAttrProvider
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _groupAttributeName,
                    isClass,
                    static (ctx, _) => DataOrDiagnostics.From(addDiagnostics => {
                        var isValid = new AttributeParser(addDiagnostics).TryParseGroupAttrib(ctx.Attributes[0], out var groupAttr);
                        return isValid ? (ctx, groupAttr!) : default;
                    })
                );

        cmdAttrProvider
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _cmdAttributeName,
                    isMethod,
                    static (ctx, _) => DataOrDiagnostics.From(addDiagnostics => {
                        var isValid = new AttributeParser(addDiagnostics).TryParseCmdAttrib(ctx.Attributes[0], out var cmdAttr);
                        return isValid ? (ctx, cmdAttr!) : default;
                    })
                );

        optAttrProvider
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _optAttributeName,
                    isMemberOrParam,
                    static (ctx, _) => DataOrDiagnostics.From(addDiagnostics => {
                        var isValid = new AttributeParser(addDiagnostics).TryParseOptAttrib(ctx.Attributes[0], out var optAttr);
                        return isValid ? (ctx, optAttr!) : default;
                    })
                );

        parseWithAttrProvider
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _parseWithAttributeName,
                    isMemberOrParam,
                    static (ctx, _) => DataOrDiagnostics.From(addDiagnostics => {
                        var isValid = new AttributeParser(addDiagnostics).TryParseParseAttrib(ctx.Attributes[0], out var parseAttr);
                        return isValid ? (ctx, parseAttr!) : default;
                    })
                );

        validateWithAttrProvider
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _validateWithAttributeName,
                    isMemberOrParam,
                    static (ctx, _) => DataOrDiagnostics.From(addDiagnostics => {
                        var isValid = new AttributeParser(addDiagnostics).TryParseValidateAttrib(ctx.Attributes[0], out var validateAttr);
                        return isValid ? (ctx, validateAttr!) : default;
                    })
                );

        validatePropAttrProvider
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _validatePropAttributeName,
                    isMemberOrParam,
                    static (ctx, _) => DataOrDiagnostics.From(addDiagnostics => {
                        var isValid = new AttributeParser(addDiagnostics).TryParseValidatePropAttrib(ctx.Attributes[0], out var validatePropAttr);
                        return isValid ? (ctx, validatePropAttr!) : default;
                    })
                );
    }

    internal static Group? CreateGroup(INamedTypeSymbol classSymbol, SemanticModel model, Action<Diagnostic> addDiagnostic) {
        var attrListBuilder = new AttributeListBuilder(addDiagnostic);

        static Group? bail() {
            SymbolInfoCache.FullReset();
            return null;
        }

        if (!GroupBuilder.TryCreateGroupFrom(classSymbol, attrListBuilder, model.Compilation, addDiagnostic, out var group))
            return bail();

        return group;
    }

    internal static Group? BindGroups(IEnumerable<Group?> groups, Action<Diagnostic> addDiagnostic) {
        var classNames = new Dictionary<string, Group>(7); // ¯\_(ツ)_/¯

        // collect the names of the each group's class
        foreach (var group in groups) {
            if (group is null)
                return null;

            // if we have duplicate class names, it means an attribute
            // was repeated multiple times on the same class. That is pretty
            // likely to happen in user code, since we should assume it is
            // invalid most of the time; therefore might as well take the small
            //perf hit of an additional lookup (since we don't have Dictionary<T,U>.TryAdd
            // on ns2.0), and just ignore it, since we'd still like to bind as many
            // group as possible in any case
            if (classNames.ContainsKey(group.FullClassName))
                continue;

            classNames.Add(group.FullClassName, group);
        }

        // if there wasn't any actual groups
        if (classNames.Count == 0)
            return null;

        Group? rootGroup = null;

        // for each group, find the group in which it was
        // contained and then add it as a subgroup to that parent

        foreach (var group in classNames.Values) {
            // a group is the root group if:
            //      (1) It is not a nested class
            //              => group.ParentClassFullName is null
            //   OR
            //      (2) Its parent class isn't marked with group
            //              => group.ParentClassFullName is not in classNames

            if (group.ParentClassFullName is null || !classNames.ContainsKey(group.ParentClassFullName)) {
                if (rootGroup is null) {
                    rootGroup = group;
                    continue;
                }

                // if there already was a root group, then
                addDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.TooManyRootGroups,
                        Location.None,
                        rootGroup.FullClassName, group.FullClassName
                    )
                );

                return null;
            }

            classNames[group.ParentClassFullName].AddSubgroup(group);
        }

        Debug.Assert(rootGroup is not null);

        if (!ValidateOptionTree(rootGroup!, addDiagnostic))
            return null;

        return rootGroup;
    }

    // todo: make those local functions static
    internal static bool ValidateOptionTree(Group rootGroup, Action<Diagnostic> addDiagnostic) {
        return validate(rootGroup, [], []);

        bool validate(Group group, HashSet<string> globalNames, HashSet<char> globalAliases) {
            var localGlobalNames = new HashSet<string>(globalNames);
            var localGlobalAliases = new HashSet<char>(globalAliases);

            if (!validateCore(group.OptionsAndFlags, localGlobalNames, localGlobalAliases))
                return false;

            foreach (var sub in group.SubGroups) {
                // yes, we have to create new ones each time,
                // cause otherwise the .Add()s would carry
                // over between subs
                if (!validate(sub, localGlobalNames, localGlobalAliases))
                    return false;
            }

            foreach (var cmd in group.Commands) {
                // commands shouldn't have any global options, so no need
                // to allocate new registries
                if (!validateCore(cmd.OptionsAndFlags, localGlobalNames, localGlobalAliases))
                    return false;
            }

            return true;
        }

        bool validateCore(IEnumerable<Option> opts, HashSet<string> globalNames, HashSet<char> globalAliases) {
            var localNames = new HashSet<string>();
            var localAliases = new HashSet<char>();

            foreach (var option in opts) {
                // register it locally and check if it's an existing global option
                // if it's new global option, register it in globalNames
                var longNameExists
                    =  !localNames.Add(option.Name)
                    || (option.IsGlobal ? !globalNames.Add(option.Name) : globalNames.Contains(option.Name));

                if (longNameExists) {
                    addDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.OptNameAlreadyExists,
                            option.GetLocation(),
                            option.Name
                        )
                    );

                    return false;
                }

                if (option.Alias != default(char)) {
                    var aliasExists
                        =  !localAliases.Add(option.Alias)
                        || (option.IsGlobal ? !globalAliases.Add(option.Alias) : globalAliases.Contains(option.Alias));

                    if (aliasExists) {
                        addDiagnostic(
                            Diagnostic.Create(
                                Diagnostics.OptAliasAlreadyExists,
                                option.GetLocation(),
                                option.Alias
                            )
                        );

                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Creates a new IVsP<string> listing all the namespaces used
    /// </summary>
    private IncrementalValuesProvider<string> GetUsedNamespacesProvider(IncrementalGeneratorInitializationContext context) {
        var groupUsings
            = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    _groupAttributeName, // fixme: we also need to check commands
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
}
