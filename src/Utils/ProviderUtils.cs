namespace StarKid.Generator.Utils;

internal static class ProviderUtils
{
    public static IncrementalValuesProvider<T> Zip<T>(this IncrementalValuesProvider<T> ivp1, IncrementalValueProvider<T> ivp2)
        => ivp1
            .Collect()
            .Combine(ivp2)
            .SelectMany((t, _) => t.Left.Append(t.Right));

    public static IncrementalValuesProvider<T> Concat<T>(this IncrementalValuesProvider<T> ivp1, IncrementalValuesProvider<T> ivp2)
        => ivp1
            .Collect()
            .Combine(ivp2.Collect())
            .SelectMany((t, _) => t.Left.Concat(t.Right));

    public static IncrementalValuesProvider<T> Distinct<T>(this IncrementalValuesProvider<T> ivp)
        => ivp.Distinct(null);
    public static IncrementalValuesProvider<T> Distinct<T>(this IncrementalValuesProvider<T> ivp, IEqualityComparer<T>? comparer)
        => ivp
            .Collect()
            .SelectMany((t, _) => t.Distinct(comparer));

    public static IncrementalValuesProvider<T> DistinctBy<T, TKey>(this IncrementalValuesProvider<T> ivp, Func<T, TKey> keySelector)
        => ivp.DistinctBy(keySelector, null);
    public static IncrementalValuesProvider<T> DistinctBy<T, TKey>(this IncrementalValuesProvider<T> ivp, Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer)
        => ivp
            .Collect()
            .SelectMany((t, _) => t.DistinctBy(keySelector, keyComparer));

    public static IncrementalValueProvider<DataOrDiagnostics<TResult>> MapData<TInput, TResult>(this IncrementalValueProvider<DataOrDiagnostics<TInput>> ivp, Func<TInput, CancellationToken, TResult> transform)
        => ivp.Select((dataOrDiag, token) => dataOrDiag.Map(data => transform(data, token)));
    public static IncrementalValueProvider<DataOrDiagnostics<TResult>> Select<TInput, TResult>(this IncrementalValueProvider<DataOrDiagnostics<TInput>> ivp, Func<Action<Diagnostic>, TInput, CancellationToken, TResult> transform)
        => ivp.Select((dataOrDiag, token) => dataOrDiag.Map((addDiag, data) => transform(addDiag, data, token)));
    public static IncrementalValuesProvider<DataOrDiagnostics<TResult>> MapData<TInput, TResult>(this IncrementalValuesProvider<DataOrDiagnostics<TInput>> ivp, Func<TInput, CancellationToken, TResult> transform)
        => ivp.Select((dataOrDiag, token) => dataOrDiag.Map(data => transform(data, token)));
    public static IncrementalValuesProvider<DataOrDiagnostics<TResult>> Select<TInput, TResult>(this IncrementalValuesProvider<DataOrDiagnostics<TInput>> ivp, Func<Action<Diagnostic>, TInput, CancellationToken, TResult> transform)
        => ivp.Select((dataOrDiag, token) => dataOrDiag.Map((addDiag, data) => transform(addDiag, data, token)));

    public static void RegisterDiagnosticSource<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValueProvider<DataOrDiagnostics<T>> wrapperProvider)
        => ctx.RegisterSourceOutput(
            wrapperProvider,
            static (spc, wrappers) => {
                foreach (var wrapper in wrappers.Diagnostics)
                    spc.ReportDiagnostic(wrapper);
            }
        );
    public static void RegisterDiagnosticSource<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValuesProvider<DataOrDiagnostics<T>> wrapperProvider)
        => ctx.RegisterSourceOutput(
            wrapperProvider,
            static (spc, wrappers) => {
                foreach (var wrapper in wrappers.Diagnostics)
                    spc.ReportDiagnostic(wrapper);
            }
        );
}