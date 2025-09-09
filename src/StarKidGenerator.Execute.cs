using System.Diagnostics;

using StarKid.Generator.AttributeModel;
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

    internal static Group? BindGroupTree(IEnumerable<Group?> groups, Action<Diagnostic> addDiagnostic) {
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
}