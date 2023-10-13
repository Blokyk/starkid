using StarKid.Generator;

namespace StarKid.Tests;

public static class DocumentationParserTests
{
    // todo: <para> vs <br/>
    // todo: trimming
    // todo: unsupported tags

    public class ParamTests {
        [Fact]
        public void NoSummaryIsSameAsSummary() {
            var xml1 = AsMethodDoc(
                """
                <summary>hello :)))</summary>
                <param name="arg">hi!</param>
                """
            );

            var xml2 = AsMethodDoc(
                """
                <param name="arg">hi!</param>
                """
            );

            var doc1 = DocumentationParser.ParseDocumentationInfoFrom(xml1);
            var doc2 = DocumentationParser.ParseDocumentationInfoFrom(xml2);

            Assert.NotNull(doc1);

            Assert.Equal(doc1!.ParamSummaries, doc2?.ParamSummaries);
        }

        [Fact]
        public void Basic() {
            var xml = AsMethodDoc(
                """
                <param name="arg">hi</param>
                """
            );

            var paramInfos = DocumentationParser.ParseDocumentationInfoFrom(xml)?.ParamSummaries;

            Assert.Equivalent(new Dictionary<string, string?>() {
                {"arg", "hi"}
            }, paramInfos);
        }

        [Fact]
        public void EmptyTagIsIgnored() {
            var xml = AsMethodDoc(
                """
                <param name="arg"></param>
                """
            );

            var paramInfos = DocumentationParser.ParseDocumentationInfoFrom(xml)?.ParamSummaries;

            Assert.Empty(paramInfos!);
        }

        [Fact]
        public void MultiParam() {
            var xml = AsMethodDoc(
                """
                <summary>
                hey
                </summary>
                <param name="srcFile">the file to read from</param>
                <param name="outputFile">hello!!!</param>
                """
            );

            var paramInfos = DocumentationParser.ParseDocumentationInfoFrom(xml)?.ParamSummaries;

            Assert.Equivalent(new Dictionary<string, string?>() {
                { "srcFile", "the file to read from" },
                { "outputFile", "hello!!!" }
            }, paramInfos);
        }
    }

    private static string AsMethodDoc([StringSyntax(StringSyntaxAttribute.Xml)] string s)
        =>
        $"""
        <member name="M:A.B()">
        {s}
        </member>
        """;
}