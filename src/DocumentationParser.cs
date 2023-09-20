using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace StarKid.Generator;

public record DocumentationInfo(
    string? Summary,
    string? Remarks,
    IReadOnlyDictionary<string, string?> ParamSummaries
);

public static class DocumentationParser
{
    public static DocumentationInfo? ParseDocumentationInfoFrom(string xml) {
        if (String.IsNullOrWhiteSpace(xml))
            return null;

        using var reader = new StringReader(xml);
        return ParseDocumentationInfoFrom(reader);
    }

    public static DocumentationInfo? ParseDocumentationInfoFrom(TextReader reader) {
        XDocument document;

        try {
            document = XDocument.Load(reader);
        } catch (XmlException) {
            return null;
        }

        var memberTags = document.Element("member")?.Descendants();

        if (memberTags is null)
            return null;

        string? summary = null;
        string? remarks = null;
        var paramSummaries = new Dictionary<string, string?>();

        foreach (var tag in memberTags) {
            // no need to set anything if the tags are empty in the end
            if (String.IsNullOrEmpty(tag.Value))
                continue;

            switch (tag?.Name.LocalName) {
                case "summary":
                    summary = FormatTag(tag);
                    break;
                case "remarks":
                    remarks = FormatTag(tag);
                    break;
                case "param":
                    var name = tag.Attribute("name")!.Value;
                    var desc = FormatTag(tag);
                    paramSummaries.Add(name, desc);
                    break;
            }
        }

        return new(
            summary,
            remarks,
            new ReadOnlyDictionary<string, string?>(paramSummaries)
        );
    }

    static string FormatTag(XElement tag) {
        if (!tag.HasElements)
            return TrimAndJoin(tag.Value);

        var nodes = tag.Nodes();

        return String.Concat(tag.Nodes().Select(ExtractContent));
    }

    static string ExtractContent(XNode n)
        =>
            n switch {
                XElement elem => elem.Name.LocalName.ToLowerInvariant() switch {
                    "br" => String.IsNullOrWhiteSpace(elem.Value)
                                ? "\n" // if it's just <br/> or similar
                                : "\n" + TrimAndJoin(elem.Value) + " ",
                    "para" => String.IsNullOrWhiteSpace(elem.Value)
                                ? "\n"
                                : "\n" + TrimAndJoin(elem.Value) + "\n",
                    _ => TrimAndJoin(elem.Value) + " ",
                },
                XText text => TrimAndJoin(text.Value) + " ",
                _ => "", // XComment, XDocument, XDocumentType, XProcessingInstruction
            };

    static string TrimAndJoin(string s)
        => String.Join(" ", TrimAndSplit(s));

    static readonly char[] __newLineArrayForSplit = new[] { '\n' };
    static readonly bool _doesFrameworkSupportTrimOption
        = typeof(StringSplitOptions).GetMember("TrimEntries") is not null;
    static IEnumerable<string> TrimAndSplit(string s)
        => _doesFrameworkSupportTrimOption
            ? s.Split(__newLineArrayForSplit, (StringSplitOptions)3) // StringSplitOptions.TrimEntries+RemoveEmptyEntries
            : s
                .Split(
                    __newLineArrayForSplit
                )
                .Select(
                    l => l.Trim('\n', ' ', '\t')
                )
                .Where(
                    s => s != ""
                );
}