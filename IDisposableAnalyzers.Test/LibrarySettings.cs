namespace IDisposableAnalyzers.Test;

using Gu.Roslyn.Asserts;

internal static class LibrarySettings
{
    internal static readonly Settings Reactive = new(
        Settings.Default.CompilationOptions.WithSuppressedDiagnostics("CS1701"),
        Settings.Default.ParseOptions,
        new MetadataReferencesCollection(
            MetadataReferences.Transitive(
                typeof(System.Reactive.Disposables.Fluent.DisposableExtensions),
                typeof(Gu.Wpf.Reactive.ConditionRelayCommand))));

    internal static readonly Settings ReactiveUi = new(
        Settings.Default.CompilationOptions.WithSuppressedDiagnostics(),
        Settings.Default.ParseOptions,
        new MetadataReferencesCollection(
            MetadataReferences.Transitive(
                typeof(ReactiveUI.ReactiveCommand),
                typeof(System.Reactive.Disposables.CompositeDisposable))));

    internal static readonly Settings Linq = new(
        Settings.Default.CompilationOptions.WithSuppressedDiagnostics("CS1701"),
        Settings.Default.ParseOptions,
        new MetadataReferencesCollection(
            MetadataReferences.Transitive(
                typeof(System.Linq.Expressions.BinaryExpression),
                typeof(Moq.It))));
}
