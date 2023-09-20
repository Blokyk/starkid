namespace StarKid.Generator.Model;

public sealed record DescriptionInfo(
    string? ShortDesc,
    string? Description,
    string? Remarks
) {
    public static DescriptionInfo? From(string? shortDesc, DocumentationInfo? info) {
        if (info is null && shortDesc is null)
            return null;

        return new(
            shortDesc,
            info?.Summary,
            info?.Remarks
        );
    }
}