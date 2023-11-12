using static StarKid.Generator.NameFormatter;
using static StarKid.Generator.NameCasing;

namespace StarKid.Tests;

public static class NamingFormatterTests
{
    public class ExtractPartsTests
    {
        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("myOption", "my", "Option")]
        [InlineData("SomeLongerName", "Some", "Longer", "Name")]
        [InlineData("sOLD_var", "s", "OLD", "var")]
        [InlineData("mi6SecretCupcake", "mi6", "Secret", "Cupcake")]
        [InlineData("")] // empty
        public void BaseTests(string str, params string[] parts)
            => Assert.Equal(parts, ExtractParts(str));

        [Theory]
        [InlineData("RemoteURIScheme", "Remote", "URI", "Scheme")]
        [InlineData("someURLAsURI", "some", "URL", "As", "URI")]
        public void ConsecutiveUpper(string str, params string[] parts)
            => Assert.Equal(parts, ExtractParts(str));

        [Theory]
        [InlineData("someStuffA", "some", "Stuff", "A")]
        [InlineData("heyDUDE", "hey", "DUDE")]
        [InlineData("i_like_underscores_", "i", "like", "underscores", "")]
        [InlineData("hey-dude-", "hey", "dude", "")]
        public void HandleSingleCharSegmentAtEndCorrectly(string str, params string[] parts)
            => Assert.Equal(parts, ExtractParts(str));

        [Theory]
        [InlineData("hello_world", "hello", "world")]
        [InlineData("safe_FTP_Getter_Methods", "safe", "FTP", "Getter", "Methods")]
        public void SnakeSupport(string str, params string[] parts)
            => Assert.Equal(parts, ExtractParts(str));

        [Theory]
        [InlineData("hello-world", "hello", "world")]
        [InlineData("safe-FTP-Getter-Methods", "safe", "FTP", "Getter", "Methods")]
        public void KebabSupport(string str, params string[] parts)
            => Assert.Equal(parts, ExtractParts(str));

        [Theory]
        [InlineData("safe-FTP_GetterMethod-URLProvider", "safe", "FTP", "Getter", "Method", "URL", "Provider")]
        public void MixedCaseSupport(string str, params string[] parts)
            => Assert.Equal(parts, ExtractParts(str));
    }

    public class Format
    {
        const string s1 = "helloWorld";
        const string
            s1_camel   = "helloWorld",  s1_pascal = "HelloWorld",
            s1_kebab   = "hello-world", s1_snake  = "hello_world",
            s1_allCaps = "HELLO_WORLD", s1_train  = "HELLO-WORLD";


        const string s2 = "URIHandler";
        const string
            s2_camel   = "uriHandler",  s2_pascal = "URIHandler",
            s2_kebab   = "uri-handler", s2_snake  = "uri_handler",
            s2_allCaps = "URI_HANDLER", s2_train  = "URI-HANDLER";

        const string s3 = "safe-FTP-GetterMethodURLProvider";
        const string
            s3_camel = "safeFTPGetterMethodURLProvider", s3_pascal = "SafeFTPGetterMethodURLProvider",
            s3_kebab = "safe-ftp-getter-method-url-provider", s3_snake = "safe_ftp_getter_method_url_provider",
            s3_allCaps = "SAFE_FTP_GETTER_METHOD_URL_PROVIDER", s3_train = "SAFE-FTP-GETTER-METHOD-URL-PROVIDER";

        [Theory]
        [InlineData(s1, s1)]
        [InlineData(s2, s2)]
        [InlineData(s3, s3)]
        public void OriginalTest(string str, string expected)
            => Assert.Equal(expected, Format(str, None));

        [Theory]
        [InlineData(s1, s1_camel)]
        [InlineData(s2, s2_camel)]
        [InlineData(s3, s3_camel)]
        public void CamelCaseTest(string str, string expected)
            => Assert.Equal(expected, Format(str, CamelCase));

        [Theory]
        [InlineData(s1, s1_pascal)]
        [InlineData(s2, s2_pascal)]
        [InlineData(s3, s3_pascal)]
        public void PascalCaseTest(string str, string expected)
            => Assert.Equal(expected, Format(str, PascalCase));

        [Theory]
        [InlineData(s1, s1_kebab)]
        [InlineData(s2, s2_kebab)]
        [InlineData(s3, s3_kebab)]
        public void KebabCaseTest(string str, string expected)
            => Assert.Equal(expected, Format(str, KebabCase));

        [Theory]
        [InlineData(s1, s1_snake)]
        [InlineData(s2, s2_snake)]
        [InlineData(s3, s3_snake)]
        public void SnakeCaseTest(string str, string expected)
            => Assert.Equal(expected, Format(str, SnakeCase));

        [Theory]
        [InlineData(s1, s1_allCaps)]
        [InlineData(s2, s2_allCaps)]
        [InlineData(s3, s3_allCaps)]
        public void AllCapsTest(string str, string expected)
            => Assert.Equal(expected, Format(str, AllCaps));

        [Theory]
        [InlineData(s1, s1_train)]
        [InlineData(s2, s2_train)]
        [InlineData(s3, s3_train)]
        public void TrainCaseTest(string str, string expected)
            => Assert.Equal(expected, Format(str, TrainCase));
    }
}
