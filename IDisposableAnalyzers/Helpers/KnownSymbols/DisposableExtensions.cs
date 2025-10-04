namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class DisposableExtensions : QualifiedType
{
    internal readonly QualifiedMethod DisposeWith;

    internal DisposableExtensions()
        : base("System.Reactive.Disposables.Fluent.DisposableExtensions")
    {
        this.DisposeWith = new QualifiedMethod(this, nameof(this.DisposeWith));
    }
}
