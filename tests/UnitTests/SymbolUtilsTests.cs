using StarKid.Generator.Utils;

namespace StarKid.Tests;

public class SymbolUtilsTests
{
    public class GetErrorName
    {
        private static readonly RS.Compilation _comp = Compilation.From("""
            namespace Foo.Bar;

            class C {
                public int F;
                public string P { get; set; }
                public void M() {}
                public bool M2(string s2, out int i2) {}

                public T G<T>(T a) where T : struct {}

                class Boo<T> {}

                class Nested {}
            }

            interface I<in T1, out T2> {}
            """);

        [Theory]
        // types
        [InlineData("C", "C")]
        [InlineData("Nested", "C.Nested")]
        [InlineData("I", "I<T1, T2>")]
        [InlineData("Boo", "C.Boo<T>")]
        // namespaces
        [InlineData("Foo", "Foo")]
        [InlineData("Bar", "Bar")]
        // simple members
        [InlineData("F", "C.F")]
        [InlineData("P", "C.P")]
        // methods
        [InlineData("M", "C.M()")]
        [InlineData("M2", "C.M2(string, out int)")]
        [InlineData("G", "C.G<T>(T)")]
        public void Basics(string symbolName, string expected) {
            var s = _comp.GetSymbolsWithName(symbolName, SymbolFilter.All).FirstOrDefault()
                        ?? throw new Exception("Couldn't find symbol for '" + expected + "'");

            Assert.Equal(expected, SymbolUtils.GetErrorName(s));
        }

        [Fact]
        public void BoundGenericTypes() {
            var s = ((INamedTypeSymbol)_comp.GetSymbolsWithName("Boo").First())
                    .Construct(_comp.GetSpecialType(SpecialType.System_Int32));
            Assert.Equal("C.Boo<int>", SymbolUtils.GetErrorName(s));
        }

        [Fact]
        public void BoundGenericMethods() {
            var s = ((IMethodSymbol)_comp.GetSymbolsWithName("G").First())
                    .Construct(_comp.GetSpecialType(SpecialType.System_Int32));
            Assert.Equal("C.G<int>(int)", SymbolUtils.GetErrorName(s));
        }
    }
}