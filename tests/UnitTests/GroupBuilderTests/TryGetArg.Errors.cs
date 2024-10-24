using StarKid.Generator;
using StarKid.Generator.SymbolModel;
using StarKid.Generator.CommandModel;

namespace StarKid.Tests;

public static partial class GroupBuilderTests
{
    public partial class TryGetArg
    {
        [Fact]
        public void DoesntCrashOnInvalidParams() {
            var source = """
                class C {
                    public void M(params int arg1) {}
                }
                """;

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (_, gb) = GetBuilder(comp);

            gb.TryGetArg_(param, out Argument _); // checks no crash
            Assert.Single(
                comp.GetDeclarationDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error),
                d => d.Id is "CS0225" // params must be arrays
            );
        }

        [Fact]
        /// <summary>
        ///     Guards against an arg being treated like a repeatable option
        /// </summary>
        public void DoesntAutoParseArray() {
            var source = """
            class C {
                public void M(int[] arg1) {}
            }
            """;

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            gb.TryGetArg_(param, out Argument _);

            Assert.Single(
                diags,
                d => d.Descriptor == Diagnostics.NoAutoParserForArrayArg
            );
        }
    }
}