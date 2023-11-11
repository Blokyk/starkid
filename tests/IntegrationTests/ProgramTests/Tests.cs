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
}