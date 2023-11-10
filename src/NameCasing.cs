namespace StarKid.Generator;

public enum NameCasing {
    /// <summary>
    /// Original name
    /// </summary>
    Original,
    /// <inheritdoc cref="None"/>
    None = Original,

    /// <summary>
    /// <c>myArg</c> and <c>config</c>
    /// </summary>
    CamelCase,

    /// <summary>
    /// <c>MyArg</c> and <c>Config</c>
    /// </summary>
    PascalCase,

    /// <summary>
    /// <c>my-arg</c> and <c>config</c>
    /// </summary>
    KebabCase,

    /// <summary>
    /// <c>my_arg</c> and <c>config</c>
    /// </summary>
    SnakeCase,

    /// <summary>
    /// <c>MY_ARG</c> and <c>CONFIG</c>
    /// </summary>
    AllCaps,

    /// <summary>
    /// <c>MY-ARG</c> and <c>CONFIG</c>
    /// </summary>
    TrainCase,
}

internal static class NameCasingUtils
{
    public static bool TryParse(string? s, out NameCasing conv) {
        if (s is null) {
            conv = default;
            return false;
        }

        switch (s.ToLowerInvariant()) {
            case "original":
            case "none":
                conv = NameCasing.Original;
                return true;
            case "camelcase":
                conv = NameCasing.CamelCase;
                return true;
            case "pascalcase":
                conv = NameCasing.PascalCase;
                return true;
            case "kebabcase":
            case "kebab-case":
                conv = NameCasing.KebabCase;
                return true;
            case "snakecase":
            case "snake_case":
                conv = NameCasing.SnakeCase;
                return true;
            case "allcaps":
            case "all_caps":
                conv = NameCasing.AllCaps;
                return true;
            case "traincase":
            case "train-case":
                conv = NameCasing.TrainCase;
                return true;
            default:
                conv = default;
                return false;
        }
    }
}