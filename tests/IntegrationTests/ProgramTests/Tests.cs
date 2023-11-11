using System.Reflection;
using System.Runtime.ExceptionServices;
using StarKid.Generated;

using static StarKid.Generated.StarKidProgram;

namespace StarKid.Tests.ProgramTests;

public class Tests
{
    public class AsBool {
        [Theory]
        [InlineData("true"), InlineData("True"), InlineData(null)]
        public void True(string? s) {
            Assert.True(pAsBool(s));
        }

        [Theory]
        [InlineData("false"), InlineData("False")]
        public void False(string? s) {
            Assert.False(pAsBool(s));
        }

        [Fact]
        public void Default() {
            Assert.True(pAsBool(null, true));
            Assert.True(pAsBool("true", true));
            Assert.False(pAsBool("false", true));

            Assert.False(pAsBool(null, false));
            Assert.False(pAsBool("false", false));
            Assert.True(pAsBool("true", false));
        }

        [Theory]
        [InlineData(""), InlineData("hello"), InlineData("TRUE"), InlineData("FALSE")]
        public void Throws(string? s) {
            Assert.Throws<FormatException>(() => pAsBool(s));
        }
    }

    public class GetFriendlyNameOf
    {
        [Theory]
        [InlineData(typeof(FileInfo), "FileInfo"), InlineData(typeof(Exception), "Exception")]
        [InlineData(typeof(Stream), "Stream"), InlineData(typeof(FileMode), "FileMode")]
        public void Basic(Type t, string expected) => Assert.Equal(expected, pGetFriendlyNameOf(t));

        [Theory]
        [InlineData(typeof(int), "int"), InlineData(typeof(short), "short")]
        [InlineData(typeof(bool), "bool"), InlineData(typeof(ulong), "ulong")]
        [InlineData(typeof(string), "string"), InlineData(typeof(double), "double")]
        public void PrimitiveTypes(Type t, string expected) => Assert.Equal(expected, pGetFriendlyNameOf(t));

        [Theory]
        [InlineData(typeof(int[]), "int[]"), InlineData(typeof(string[]), "string[]")]
        [InlineData(typeof(FileInfo[]), "FileInfo[]"), InlineData(typeof(bool[][]), "bool[][]")]
        [InlineData(typeof(int[,,]), "int[,,]")]
        [InlineData(typeof(Array), "Array")] // !
        public void Arrays(Type t, string expected) => Assert.Equal(expected, pGetFriendlyNameOf(t));

        [Theory]
        [InlineData(typeof(List<int>), "List<int>"), InlineData(typeof(Dictionary<string, int?>), "Dictionary<string, int?>")]
        [InlineData(typeof(Span<char[]>), "Span<char[]>")]
        [InlineData(typeof(HashSet<>), "HashSet<T>"), InlineData(typeof(KeyValuePair<,>), "KeyValuePair<TKey, TValue>")]
        [InlineData(typeof(Func<,,,,,,,,,,,,,,,,>), "Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>")]
        public void Generics(Type t, string expected) => Assert.Equal(expected, pGetFriendlyNameOf(t));

        [Theory]
        [InlineData(typeof(bool?), "bool?"), InlineData(typeof(List<string?>), "List<string>")] // can't use NRTs directly in typeof
        public void Nullable(Type t, string expected) => Assert.Equal(expected, pGetFriendlyNameOf(t));

        [Theory]
        [InlineData(typeof((int, bool)), "(int, bool)"), InlineData(typeof((string, short)), "(string, short)")]
        [InlineData(typeof((int, int?, string?)), "(int, int?, string)")]
        [InlineData(typeof((int, (string, bool))), "(int, (string, bool))")]
        [InlineData(
            typeof(ValueTuple<int, int, int, int, int, int, int, int>),
                  "(int, int, int, int, int, int, int, int)")]
        [InlineData(
            typeof(Tuple<object, string, FileInfo, int, int[], string, string[], Action>),
                  "(object, string, FileInfo, int, int[], string, string[], Action)")]
        [InlineData(typeof(Tuple<,>), "Tuple<T1, T2>")] // !
        public void Tuples(Type t, string expected) => Assert.Equal(expected, pGetFriendlyNameOf(t));

        private class NonGenericInner { }
        private class GenericInner<T> { public class NonGenericInnerInner {} public class GenericInnerInner<U, V> {} }

        [Theory]
        [InlineData(typeof(ParseEnum.Fruits), "Fruits")]
        [InlineData(typeof(NonGenericInner), "NonGenericInner")]
        [InlineData(typeof(GenericInner<>), "GenericInner<T>")]
        [InlineData(typeof(GenericInner<int>), "GenericInner<int>")]
        [InlineData(typeof(GenericInner<>.NonGenericInnerInner), "NonGenericInnerInner")]
        [InlineData(typeof(GenericInner<>.GenericInnerInner<,>), "GenericInnerInner<U, V>")]
        [InlineData(typeof(GenericInner<int>.GenericInnerInner<string, float>), "GenericInnerInner<string, float>")]
        public void Nested(Type t, string expected) => Assert.Equal(expected, pGetFriendlyNameOf(t));
    }
}