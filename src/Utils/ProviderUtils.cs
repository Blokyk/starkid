namespace StarKid.Generator.Utils;

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

    public static IncrementalValueProvider<T?> Data<T>(this IncrementalValueProvider<DataAndDiagnostics<T>> ivp)
        => ivp.Select((wrapper, _) => wrapper.Data);
    public static IncrementalValuesProvider<T?> Data<T>(this IncrementalValuesProvider<DataAndDiagnostics<T>> ivp)
        => ivp.Select((wrapper, _) => wrapper.Data);

    public static void RegisterDiagnosticSource<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValueProvider<DataAndDiagnostics<T>> wrapperProvider)
        => ctx.RegisterSourceOutput(
            wrapperProvider,
            static (spc, wrappers) => {
                foreach (var wrapper in wrappers.GetDiagnostics())
                    spc.ReportDiagnostic(wrapper);
            }
        );
    public static void RegisterDiagnosticSource<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValuesProvider<DataAndDiagnostics<T>> wrapperProvider)
        => ctx.RegisterSourceOutput(
            wrapperProvider,
            static (spc, wrappers) => {
                foreach (var wrapper in wrappers.GetDiagnostics())
                    spc.ReportDiagnostic(wrapper);
            }
        );
}