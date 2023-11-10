using static StarKid.Generator.NameFormatter;
using static StarKid.Generator.NameCasing;

namespace StarKid.Tests;

public static class NamingFormatterTests
{
    public class ExtractPartsTests
    {
        [Fact]
        public void BaseTests() {
            Assert.Equal(
                new[] { "hello" },
                ExtractParts("hello")
            );

            Assert.Equal(
                new[] { "my", "Option" },
                ExtractParts("myOption")
            );

            Assert.Equal(
                new[] { "Some", "Longer", "Name" },
                ExtractParts("SomeLongerName")
            );

            Assert.Equal(
                new[] { "s", "OLD", "var" },
                ExtractParts("sOLD_var")
            );

            Assert.Empty(ExtractParts(""));
        }

        [Fact]
        public void ConsecutiveUpper() {
            Assert.Equal(
                new[] { "Remote", "URI", "Scheme" },
                ExtractParts("RemoteURIScheme")
            );

            Assert.Equal(
                new[] { "some", "URL", "As", "URI" },
                ExtractParts("someURLAsURI")
            );
        }

        [Fact]
        public void HandleSingleCharSegmentAtEndCorrectly() {
            Assert.Equal(
                new[] { "some", "Stuff", "A" },
                ExtractParts("someStuffA")
            );

            Assert.Equal(
                new[] { "hey", "DUDE" },
                ExtractParts("heyDUDE")
            );

            Assert.Equal(
                new[] { "i", "like", "underscores", "" },
                ExtractParts("i_like_underscores_")
            );

            Assert.Equal(
                new[] { "hey", "dude", "" },
                ExtractParts("hey-dude-")
            );
        }

        [Fact]
        public void SnakeSupport() {
            Assert.Equal(
                new[] { "hello", "world" },
                ExtractParts("hello_world")
            );

            var l = ExtractParts("safe_FTP_Getter_Method").ToArray();

            Assert.Equal(
                new[] { "safe", "FTP", "Getter", "Method" },
                l
            );
        }

        [Fact]
        public void KebabSupport() {
            Assert.Equal(
                new[] { "hello", "world" },
                ExtractParts("hello-world")
            );

            Assert.Equal(
                new[] { "safe", "FTP", "Getter", "Method" },
                ExtractParts("safe-FTP-Getter-Method")
            );
        }

        [Fact]
        public void MixedCaseSupport() {
            Assert.Equal(
                new[] { "safe", "FTP", "Getter", "Method", "URL", "Provider" },
                ExtractParts("safe-FTP-GetterMethod-URLProvider")
            );
        }
    }

    public class Format
    {
        [Fact]
        public void OriginalTest() {
            Assert.Equal(
                "helloWorld",
                Format("helloWorld", Original)
            );

            Assert.Equal(
                "URIHandler",
                Format("URIHandler", Original)
            );

            Assert.Equal(
                "safe-FTP-GetterMethodURLProvider",
                Format("safe-FTP-GetterMethodURLProvider", None)
            );
        }

        [Fact]
        public void CamelCaseTest() {
            Assert.Equal(
                "helloWorld",
                Format("helloWorld", CamelCase)
            );

            Assert.Equal(
                "uriHandler",
                Format("URIHandler", CamelCase)
            );

            Assert.Equal(
                "safeFTPGetterMethodURLProvider",
                Format("safe-FTP-GetterMethodURLProvider", CamelCase)
            );
        }

        [Fact]
        public void PascalCaseTest() {
            Assert.Equal(
                "HelloWorld",
                Format("helloWorld", PascalCase)
            );

            Assert.Equal(
                "URIHandler",
                Format("URIHandler", PascalCase)
            );

            Assert.Equal(
                "SafeFTPGetterMethodURLProvider",
                Format("safe-FTP-GetterMethodURLProvider", PascalCase)
            );
        }

        [Fact]
        public void KebabCaseTest() {
            Assert.Equal(
                "hello-world",
                Format("helloWorld", KebabCase)
            );

            Assert.Equal(
                "uri-handler",
                Format("URIHandler", KebabCase)
            );

            Assert.Equal(
                "safe-ftp-getter-method-url-provider",
                Format("safe-FTP-GetterMethodURLProvider", KebabCase)
            );
        }

        [Fact]
        public void SnakeCaseTest() {
            Assert.Equal(
                "hello_world",
                Format("hello_world", SnakeCase)
            );

            Assert.Equal(
                "uri_handler",
                Format("URIHandler", SnakeCase)
            );

            Assert.Equal(
                "safe_ftp_getter_method_url_provider",
                Format("safe-FTP-GetterMethod_URLProvider", SnakeCase)
            );
        }

        [Fact]
        public void AllCapsCaseTest() {
            Assert.Equal(
                "HELLO_WORLD",
                Format("helloWorld", AllCaps)
            );

            Assert.Equal(
                "URI_HANDLER",
                Format("URIHandler", AllCaps)
            );

            Assert.Equal(
                "SAFE_FTP_GETTER_METHOD_URL_PROVIDER",
                Format("safe-FTP-GetterMethodURLProvider", AllCaps)
            );
        }

        [Fact]
        public void TrainCaseTest() {
            Assert.Equal(
                "HELLO-WORLD",
                Format("helloWorld", TrainCase)
            );

            Assert.Equal(
                "URI-HANDLER",
                Format("URIHandler", TrainCase)
            );

            Assert.Equal(
                "SAFE-FTP-GETTER-METHOD-URL-PROVIDER",
                Format("safe-FTP-GetterMethodURLProvider", TrainCase)
            );
        }
    }
}
