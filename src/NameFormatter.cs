namespace StarKid.Generator;

internal static class NameFormatter
{
    public static string Format(string s, NameCasing casing)
        => casing switch {
                NameCasing.Original => s,
                NameCasing.CamelCase  => ToCamel(s),
                NameCasing.PascalCase => ToPascal(s),
                NameCasing.KebabCase  => ToKebab(s),
                NameCasing.SnakeCase  => ToSnake(s),
                NameCasing.AllCaps    => ToAllCaps(s),
                NameCasing.TrainCase  => ToTrain(s),
                _ => throw new InvalidOperationException($"Casing '{casing}' doesn't exist!")
        };

    /// <summary>
    /// Tries to split a (csharp-legal) name into its component parts.
    /// <br/>
    /// For example, "RemoteURIScheme" gets split into [ "Remote", "URI", "Scheme" ]
    /// </summary>
    internal static IEnumerable<string> ExtractParts(string s) {
        if (s.Length == 0)
            yield break;

        int partStart = 0;
        bool lastWasLower = MiscUtils.IsAsciiLetterLower(s[0]);

        for (int i = 1; i < s.Length; i++) {
            if (!MiscUtils.IsAsciiLetter(s[i])) {
                if (s[i] is not ('_' or '-'))
                    continue;

                yield return s[partStart..i];

                // pretend like we advanced in the string
                i++;
                partStart = i;
                if (i < s.Length) {
                    lastWasLower = MiscUtils.IsAsciiLetterLower(s[i]);
                }
                continue;
            }

            var currIsLower = MiscUtils.IsAsciiLetterLower(s[i]);

            if (lastWasLower) {
                if (currIsLower)
                    continue;

                yield return s[partStart..i];
                partStart = i;
                // lastWasLower = false;
            } else { // last was uppercase
                if (!currIsLower)
                    continue;

                // if this is a lowercase after a bunch of uppercase,
                // then jettison the chars BEFORE the last uppercase,

                // this could create an empty string in case we had a '_' before
                if (partStart != i-1)
                    yield return s[partStart..(i-1)];

                partStart = i-1;
                // lastWasLower = true;
            }

            lastWasLower = currIsLower;
        }

        yield return s[partStart..];
    }

    public static string ToCamel(string s)
        => String.Concat(
            ExtractParts(s)
                .Select(
                    (s, i) => i == 0 ? s.ToLowerInvariant() : MiscUtils.CapitalizeString(s)
                )
        );

    public static string ToPascal(string s)
        => String.Concat(ExtractParts(s).Select(MiscUtils.CapitalizeString));

    public static string ToKebab(string s)
        => String.Join("-", ExtractParts(s).Select(s => s.ToLowerInvariant()));

    public static string ToSnake(string s)
        => String.Join("_", ExtractParts(s).Select(s => s.ToLowerInvariant()));

    public static string ToAllCaps(string s)
        => String.Join("_", ExtractParts(s).Select(s => s.ToUpperInvariant()));

    public static string ToTrain(string s)
        => String.Join("-", ExtractParts(s).Select(s => s.ToUpperInvariant()));
}
