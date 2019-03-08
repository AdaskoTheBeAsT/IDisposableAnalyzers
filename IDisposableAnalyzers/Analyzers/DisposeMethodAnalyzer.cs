namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DisposeMethodAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP009IsIDisposable.Descriptor,
            IDISP010CallBaseDispose.Descriptor,
            IDISP018CallSuppressFinalizeWhenFinalizer.Descriptor,
            IDISP019CallSuppressFinalizeWhenVirtualDispose.Descriptor,
            IDISP020SuppressFinalizeThis.Descriptor,
            IDISP021DisposeTrue.Descriptor,
            IDISP023ReferenceTypeInFinalizerContext.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.MethodDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.ContainingSymbol is IMethodSymbol method &&
                context.Node is MethodDeclarationSyntax methodDeclaration &&
                method.Name == "Dispose" &&
                method.ReturnsVoid)
            {
                if (method.Parameters.Length == 0 &&
                    !method.IsStatic &&
                    method.DeclaredAccessibility == Accessibility.Public &&
                    method.ReturnsVoid &&
                    method.OverriddenMethod == null &&
                    method.GetAttributes().Length == 0)
                {
                    if (!method.ExplicitInterfaceImplementations.Any() &&
                        !IsInterfaceImplementation(method))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP009IsIDisposable.Descriptor, methodDeclaration.Identifier.GetLocation()));
                    }

                    if (DisposeMethod.TryFindSuppressFinalizeCall(methodDeclaration, context.SemanticModel, context.CancellationToken, out var suppressFinalize))
                    {
                        if (suppressFinalize.ArgumentList is ArgumentListSyntax argumentList &&
                            argumentList.Arguments.TrySingle(out var argument) &&
                            !argument.Expression.IsKind(SyntaxKind.ThisExpression))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(IDISP020SuppressFinalizeThis.Descriptor, argument.GetLocation()));
                        }
                    }
                    else if (method.ContainingType.TryFindFirstMethod(x => x.MethodKind == MethodKind.Destructor, out _))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP018CallSuppressFinalizeWhenFinalizer.Descriptor, methodDeclaration.Identifier.GetLocation()));
                    }
                    else if (method.ContainingType.TryFindFirstMethod(x => DisposeMethod.IsVirtualDispose(x), out _))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP019CallSuppressFinalizeWhenVirtualDispose.Descriptor, methodDeclaration.Identifier.GetLocation()));
                    }

                    if (DisposeMethod.TryFindDisposeBoolCall(methodDeclaration, context.SemanticModel, context.CancellationToken, out _, out var isDisposing) &&
                        isDisposing.Expression?.IsKind(SyntaxKind.TrueLiteralExpression) != true)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP021DisposeTrue.Descriptor, isDisposing.GetLocation()));
                    }
                }

                if (method.Parameters.TrySingle(out var parameter) &&
                    parameter.Type == KnownSymbol.Boolean)
                {
                    if (ShouldCallBase(method, methodDeclaration, context))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP010CallBaseDispose.Descriptor, methodDeclaration.Identifier.GetLocation(), parameter.Name));
                    }

                    using (var walker = FinalizerContextWalker.Borrow(methodDeclaration, context.SemanticModel, context.CancellationToken))
                    {
                        foreach (var node in walker.UsedReferenceTypes)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(IDISP023ReferenceTypeInFinalizerContext.Descriptor, node.GetLocation()));
                        }
                    }
                }
            }
        }

        private static bool IsInterfaceImplementation(IMethodSymbol method)
        {
            if (method.ContainingType.TypeKind == TypeKind.Interface)
            {
                return true;
            }

            foreach (var @interface in method.ContainingType.AllInterfaces)
            {
                foreach (var member in @interface.GetMembers())
                {
                    if (member is IMethodSymbol candidate &&
                        candidate.Name == "Dispose" &&
                        candidate.ReturnsVoid &&
                        candidate.DeclaredAccessibility == Accessibility.Public &&
                        candidate.Parameters.Length == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ShouldCallBase(IMethodSymbol method, MethodDeclarationSyntax methodDeclaration, SyntaxNodeAnalysisContext context)
        {
            if (method.Parameters.TrySingle(out var parameter) &&
                parameter.Type == KnownSymbol.Boolean &&
                method.IsOverride &&
                method.OverriddenMethod is IMethodSymbol overridden &&
                !DisposeMethod.TryFindBaseCall(methodDeclaration, context.SemanticModel, context.CancellationToken, out _))
            {
                if (overridden.DeclaringSyntaxReferences.Length == 0)
                {
                    return true;
                }
                else
                {
                    using (var disposeWalker = DisposeWalker.Borrow(overridden, context.SemanticModel, context.CancellationToken))
                    {
                        foreach (var disposeCall in disposeWalker.Invocations)
                        {
                            if (DisposeCall.TryGetDisposed(disposeCall, context.SemanticModel, context.CancellationToken, out var disposed) &&
                                FieldOrProperty.TryCreate(disposed, out var fieldOrProperty) &&
                                !DisposableMember.IsDisposed(fieldOrProperty, method, context.SemanticModel, context.CancellationToken))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
