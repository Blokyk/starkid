namespace StarKid.Generator;

internal static class DataOrDiagProvidersExtensions
{
    public static IncrementalValueProvider<DataOrDiagnostics<TResult>> MapData<TInput, TResult>(this IncrementalValueProvider<DataOrDiagnostics<TInput>> ivp, Func<TInput, CancellationToken, TResult> transform)
        => ivp.Select((dataOrDiag, token) => dataOrDiag.Map(data => transform(data, token)));

    public static IncrementalValueProvider<DataOrDiagnostics<TResult>> Select<TInput, TResult>(this IncrementalValueProvider<DataOrDiagnostics<TInput>> ivp, Func<TInput, Action<Diagnostic>, CancellationToken, TResult?> transform)
        => ivp.Select((dataOrDiag, token) => dataOrDiag.Map((addDiag, data) => transform(data, addDiag, token)));

    public static IncrementalValuesProvider<DataOrDiagnostics<TResult>> SelectMany<TInput, TResult>(this IncrementalValueProvider<DataOrDiagnostics<TInput>> ivp, Func<TInput, Action<Diagnostic>, CancellationToken, IEnumerable<TResult>?> transform)
        => ivp.SelectMany((dataOrDiag, token) => {
            // if we don't have data to start with, just return the existing diags
            if (!dataOrDiag.TryGetData(out var oldData))
                return [new(dataOrDiag.Diagnostics)];

            var newCollectDiag = DataOrDiagnostics.From(addDiag => transform(oldData, addDiag, token));

            // if this produced diags, just return those
            if (!newCollectDiag.TryGetData(out var newDataArray))
                return [new(newCollectDiag.Diagnostics)];

            return newDataArray.Select(data => new DataOrDiagnostics<TResult>(data));
        });

    public static IncrementalValueProvider<DataOrDiagnostics<(T1 Left, T2 Right)>> Combine<T1, T2>(this IncrementalValueProvider<DataOrDiagnostics<T1>> ivp, IncrementalValueProvider<DataOrDiagnostics<T2>> ivp2)
        => IncrementalValueProviderExtensions.Combine(ivp, ivp2).Select((t, _) => {
            var (d1, d2) = t;
            if (d1.HasData && d2.HasData)
                return new DataOrDiagnostics<(T1, T2)>((d1.Data, d2.Data));
            else
                return new(d1.Diagnostics.Concat(d2.Diagnostics).ToImmutableArray());
        });




    public static IncrementalValueProvider<DataOrDiagnostics<(T1 Left, T2 Right)>> Combine<T1, T2>(this IncrementalValueProvider<DataOrDiagnostics<T1>> ivp, IncrementalValueProvider<T2> ivp2)
        => IncrementalValueProviderExtensions.Combine(ivp, ivp2).Select((t, _) => t.Left.Map(leftData => (leftData, t.Right)));

    public static IncrementalValuesProvider<DataOrDiagnostics<TResult>> MapData<TInput, TResult>(this IncrementalValuesProvider<DataOrDiagnostics<TInput>> ivp, Func<TInput, CancellationToken, TResult> transform)
        => ivp.Select((dataOrDiag, token) => dataOrDiag.Map(data => transform(data, token)));
    public static IncrementalValuesProvider<DataOrDiagnostics<TResult>> Select<TInput, TResult>(this IncrementalValuesProvider<DataOrDiagnostics<TInput>> ivp, Func<TInput, Action<Diagnostic>, CancellationToken, TResult?> transform)
        => ivp.Select((dataOrDiag, token) => dataOrDiag.Map((addDiag, data) => transform(data, addDiag, token)));

    public static IncrementalValuesProvider<DataOrDiagnostics<TResult>> SelectMany<TInput, TResult>(this IncrementalValuesProvider<DataOrDiagnostics<TInput>> ivp, Func<TInput, Action<Diagnostic>, CancellationToken, IEnumerable<TResult>?> transform)
        => ivp.SelectMany((dataOrDiag, token) => {
            // if we don't have data to start with, just return the existing diags
            if (!dataOrDiag.TryGetData(out var oldData))
                return [new(dataOrDiag.Diagnostics)];

            var newCollectDiag = DataOrDiagnostics.From(addDiag => transform(oldData, addDiag, token));

            // if this produced diags, just return those
            if (!newCollectDiag.TryGetData(out var newDataArray))
                return [new(newCollectDiag.Diagnostics)];

            return newDataArray.Select(data => new DataOrDiagnostics<TResult>(data));
        });

    public static IncrementalValueProvider<DataOrDiagnostics<ImmutableArray<TInput>>> Collect<TInput>(this IncrementalValuesProvider<DataOrDiagnostics<TInput>> ivp)
        => IncrementalValueProviderExtensions.Collect(ivp).Select((dataOrDiags, _) => {
            // if there aren't any diags, this will be pretty cheap,
            // if there *are* diags, then we need this anyway so we'd have to pay the cost either way
            var diags = dataOrDiags.SelectMany(d => d.Diagnostics).ToImmutableArray();
            if (diags.Length != 0)
                return new DataOrDiagnostics<ImmutableArray<TInput>>(diags);
            else
                // notnull: if there weren't any diags, then we know every data is valid
                return new(dataOrDiags.Select(d => d.Data!).ToImmutableArray());
        });

    public static IncrementalValuesProvider<DataOrDiagnostics<(T1 Left, T2 Right)>> Combine<T1, T2>(this IncrementalValuesProvider<DataOrDiagnostics<T1>> ivp1, IncrementalValueProvider<DataOrDiagnostics<T2>> ivp2)
        => IncrementalValueProviderExtensions.Combine(ivp1, ivp2).Select((t, _) => {
            var (d1, d2) = t;
            if (d1.HasData && d2.HasData)
                return new DataOrDiagnostics<(T1, T2)>((d1.Data, d2.Data));
            else
                return new(d1.Diagnostics.Concat(d2.Diagnostics).ToImmutableArray());
        });
    public static IncrementalValuesProvider<DataOrDiagnostics<(T1 Left, T2 Right)>> Combine<T1, T2>(this IncrementalValuesProvider<DataOrDiagnostics<T1>> ivp1, IncrementalValueProvider<T2> ivp2)
        => IncrementalValueProviderExtensions.Combine(ivp1, ivp2).Select((t, _) => t.Left.Map(leftData => (leftData, t.Right)));




    public static void RegisterDiagnosticSource<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValueProvider<DataOrDiagnostics<T>> wrapperProvider)
        => ctx.RegisterSourceOutput(
            wrapperProvider,
            static (spc, wrapper) => {
                foreach (var diag in wrapper.Diagnostics)
                    spc.ReportDiagnostic(diag);
            }
        );
    public static void RegisterDiagnosticSource<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValuesProvider<DataOrDiagnostics<T>> wrapperProvider)
        => ctx.RegisterSourceOutput(
            wrapperProvider,
            static (spc, wrapper) => {
                foreach (var diag in wrapper.Diagnostics)
                    spc.ReportDiagnostic(diag);
            }
        );

    public static void RegisterSourceOutput<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValueProvider<DataOrDiagnostics<T>> wrapperProvider, Action<SourceProductionContext, T> action)
        => ctx.RegisterSourceOutput(
            wrapperProvider,
            (spc, wrapper) => {
                if (wrapper.HasData)
                    action(spc, wrapper.Data);
            }
        );
    public static void RegisterSourceOutput<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValuesProvider<DataOrDiagnostics<T>> wrapperProvider, Action<SourceProductionContext, T> action)
        => ctx.RegisterSourceOutput(
            wrapperProvider,
            (spc, wrapper) => {
                if (wrapper.HasData)
                    action(spc, wrapper.Data);
            }
        );

    public static void RegisterImplementationSourceOutput<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValueProvider<DataOrDiagnostics<T>> wrapperProvider, Action<SourceProductionContext, T> action)
        => ctx.RegisterImplementationSourceOutput(
            wrapperProvider,
            (spc, wrapper) => {
                if (wrapper.HasData)
                    action(spc, wrapper.Data);
            }
        );
    public static void RegisterImplementationSourceOutput<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValuesProvider<DataOrDiagnostics<T>> wrapperProvider, Action<SourceProductionContext, T> action)
        => ctx.RegisterImplementationSourceOutput(
            wrapperProvider,
            (spc, wrapper) => {
                if (wrapper.HasData)
                    action(spc, wrapper.Data);
            }
        );
}