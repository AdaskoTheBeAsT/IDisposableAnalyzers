namespace IDisposableAnalyzers;

using System.Collections.Immutable;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class MethodReturnValuesAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.IDISP015DoNotReturnCachedAndCreated);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.MethodDeclaration, SyntaxKind.GetAccessorDeclaration);
    }

    private static void Handle(SyntaxNodeAnalysisContext context)
    {
        if (context.ContainingSymbol is IMethodSymbol method &&
            context.Node is MethodDeclarationSyntax methodDeclaration &&
            Disposable.IsAssignableFrom(method.ReturnType, context.Compilation))
        {
            using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.RecursiveInside, context.SemanticModel, context.CancellationToken);
            if (walker.Values.TryFirst(IsCreated, out _) &&
                walker.Values.TryFirst(x => IsCachedOrInjected(x) && !IsNop(x), out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP015DoNotReturnCachedAndCreated, methodDeclaration.Identifier.GetLocation()));
            }
        }

        bool IsCreated(ExpressionSyntax? expression)
        {
            if (expression is null)
            {
                return false;
            }

            return Disposable.IsCreation(expression, context.SemanticModel, context.CancellationToken);
        }

        bool IsCachedOrInjected(ExpressionSyntax? expression)
        {
            if (expression is null)
            {
                return false;
            }

            return Disposable.IsCachedOrInjected(expression, expression, context.SemanticModel, context.CancellationToken);
        }

        bool IsNop(ExpressionSyntax? expression)
        {
            if (expression is null)
            {
                return true;
            }

            return Disposable.IsNop(expression, context.SemanticModel, context.CancellationToken);
        }
    }
}
