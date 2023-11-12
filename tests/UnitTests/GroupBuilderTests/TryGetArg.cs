using StarKid.Generator.SymbolModel;
using StarKid.Generator.CommandModel;

namespace StarKid.Tests;

public static partial class GroupBuilderTests
{
    public partial class TryGetArg
    {
        [Fact]
        public void SingleString() {
            var source = @"
class C {
    public void M(string arg1) {}
}
";

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            Assert.True(gb.TryGetArg_(param, out Argument arg));
            Assert.Empty(diags);

            Assert.Equivalent(
                new {
                    Type = new { FullName = "System.String" },
                    Name = "arg1",
                    Parser = ParserInfo.StringIdentity
                },
                arg
            );
        }

        [Theory]
        [InlineData("\"foo\"")] // inline
        [InlineData("arg1DefaultValue")] // private const field
        [InlineData("D.someConst")] // different class const field
        public void SingleStringDefaultValue(string defaultValExpr) {
            var source = @"
class C {
    private const string arg1DefaultValue = ""foo"";
    public void M(string arg1 = " + defaultValExpr + @") {}
}

class D {
    public const string someConst = ""foo"";
}
";

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            Assert.True(gb.TryGetArg_(param, out Argument arg));
            Assert.Empty(diags);

            Assert.Equivalent(
                new {
                    Type = CommonTypes.STR,
                    Name = "arg1",
                    Parser = ParserInfo.StringIdentity,
                    IsParams = false,
                    DefaultValueExpr = "\"foo\""
                },
                arg
            );
        }

        [Fact]
        public void SingleInt() {
            var source = @"
class C {
    public void M(int arg1) {}
}
";

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            Assert.True(gb.TryGetArg_(param, out Argument arg));
            Assert.Empty(diags);

            Assert.Equivalent(
                new {
                    Type = CommonTypes.INT32,
                    Name = "arg1",
                    Parser = new ParserInfo.DirectMethod("System.Int32.Parse", CommonTypes.INT32)
                },
                arg
            );
        }

        [Fact]
        public void SingleIntWithDefaultValue() {
            var source = @"
class C {
    public void M(int arg1 = 1234) {}
}
";

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            Assert.True(gb.TryGetArg_(param, out Argument arg));
            Assert.Empty(diags);

            Assert.Equivalent(
                new {
                    Type = CommonTypes.INT32,
                    Name = "arg1",
                    Parser = new ParserInfo.DirectMethod("System.Int32.Parse", CommonTypes.INT32),
                    DefaultValueExpr = "1234"
                },
                arg
            );
        }

        [Fact]
        public void SingleFloatWithDefaultValue() {
            var source = @"
class C {
    public void M(float arg1 = 10220.123f) {}
}
";

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            Assert.True(gb.TryGetArg_(param, out Argument arg));
            Assert.Empty(diags);

            Assert.Equivalent(
                new {
                    Type = CommonTypes.SINGLE,
                    Name = "arg1",
                    Parser = new ParserInfo.DirectMethod("System.Single.Parse", CommonTypes.SINGLE),
                    DefaultValueExpr = 10220.123f.ToString() + "f" // don't wanna mess with float rounding
                },
                arg
            );
        }

        [Fact]
        public void StringParamsArg() {
            var source = @"
class C {
    public void M(params string[] arg1) {}
}
";

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            Assert.True(gb.TryGetArg_(param, out Argument arg));
            Assert.Empty(diags);

            Assert.Equivalent(
                new {
                    Type = new { Name = "String[]" },
                    Name = "arg1",
                    Parser = ParserInfo.StringIdentity,
                    IsParams = true
                },
                arg
            );
        }

        [Fact]
        public void IntParamsArg() {
            var source = @"
class C {
    public void M(params int[] arg1) {}
}
";

            var comp = Compilation.From(source);
            var param = ((IMethodSymbol)comp.GetSymbolsWithName("M").First()).Parameters[0];

            var (diags, gb) = GetBuilder(comp);

            Assert.True(gb.TryGetArg_(param, out Argument arg));
            Assert.Empty(diags);

            Assert.Equivalent(
                new {
                    Type = new { Name = "Int32[]" },
                    Name = "arg1",
                    Parser = new ParserInfo.DirectMethod("System.Int32.Parse", CommonTypes.INT32),
                    IsParams = true
                },
                arg
            );
        }
    }
}