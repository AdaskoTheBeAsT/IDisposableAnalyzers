namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP011DontReturnDisposed
    {
        internal const string DiagnosticId = "IDISP011";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't return disposed instance.",
            messageFormat: "Don't return disposed instance.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't return disposed instance.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
