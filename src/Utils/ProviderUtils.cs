namespace StarKid.Generator;

internal static class ProviderUtils
{
    public static IncrementalValuesProvider<T> Append<T>(this IncrementalValuesProvider<T> ivp1, IncrementalValueProvider<T> ivp2)
        => ivp1
            .Collect()
            .Combine(ivp2)
            .SelectMany((t, _) => t.Left.Append(t.Right));

    public static IncrementalValuesProvider<T> Concat<T>(this IncrementalValuesProvider<T> ivp1, IncrementalValuesProvider<T> ivp2)
        => ivp1
            .Collect()
            .Combine(ivp2.Collect())
            .SelectMany((t, _) => t.Left.Concat(t.Right));
}