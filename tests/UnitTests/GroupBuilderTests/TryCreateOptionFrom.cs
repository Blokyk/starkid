using StarKid.Generator;
using StarKid.Generator.SymbolModel;
using StarKid.Generator.CommandModel;

namespace StarKid.Tests;

public static partial class GroupBuilderTests
{
    public partial class TryCreateOptionFrom
    {
        [Fact]
        public void AutoParseRepeatable() {
            var source = """
            using StarKid;

            class C {
                public void M([Option("opt1")] int[] arg1) {}
            }
            """;

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            gb.TryCreateOptionFrom_(param, out Option opt);

            Assert.Empty(diags);
            Assert.True(opt.IsRepeatableOption());
            Assert.Equivalent(
                new {
                    Parser = _intParser,
                    Type = new { Name = "Int32[]" },
                    Name = "opt1",
                },
                opt
            );
        }

        [Fact]
        public void NamedParserRepeatable() {
            var source = """
            using StarKid;

            class C {
                public static C Foo(string s) => null!;

                public void M(
                    [Option("opt1")] [ParseWith(nameof(Foo))] C[] arg1
                ) {}
            }
            """;

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            gb.TryCreateOptionFrom_(param, out Option opt);

            Assert.Empty(diags);
            Assert.True(opt.IsRepeatableOption());
            Assert.Equivalent(
                new {
                    Parser = new { FullName = "C.Foo", TargetType = new { Name = "C" } },
                    Type = new { Name = "C[]" },
                    Name = "opt1",
                },
                opt
            );
        }

        [Fact]
        public void NamedParserNonRepeatable() {
            var source = """
            using StarKid;

            class C {
                public static C[] Bar(string s) => null!;

                public void M(
                    [Option("opt1")] [ParseWith(nameof(Bar))] C[] arg1
                ) {}
            }
            """;

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            gb.TryCreateOptionFrom_(param, out Option opt);

            Assert.Empty(diags);
            Assert.False(opt.IsRepeatableOption());
            Assert.Equivalent(
                new {
                    Parser = new { FullName = "C.Bar", TargetType = new { Name = "C[]" } },
                    Type = new { Name = "C[]" },
                    Name = "opt1",
                },
                opt
            );
        }
    }
}