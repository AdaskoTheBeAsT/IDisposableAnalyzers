namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposeCall
    {
        internal static bool TryGetDisposed(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol disposed)
        {
            disposed = null;
            return IsIDisposableDispose(disposeCall, semanticModel, cancellationToken) &&
                   MemberPath.TrySingle(disposeCall, out var expression) &&
                   semanticModel.TryGetSymbol(expression, cancellationToken, out disposed);
        }

        internal static bool TryGetDisposedRootMember(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, out IdentifierNameSyntax disposedMember)
        {
            if (MemberPath.TryFindRoot(disposeCall, out disposedMember))
            {
                var property = semanticModel.GetSymbolSafe(disposedMember, cancellationToken) as IPropertySymbol;
                if (property == null ||
                    property.IsAutoProperty())
                {
                    return true;
                }

                if (property.GetMethod == null)
                {
                    return false;
                }

                foreach (var reference in property.GetMethod.DeclaringSyntaxReferences)
                {
                    var node = reference.GetSyntax(cancellationToken);
                    using (var pooled = ReturnValueWalker.Borrow(node, ReturnValueSearch.TopLevel, semanticModel, cancellationToken))
                    {
                        if (pooled.Count == 0)
                        {
                            return true;
                        }

                        return pooled.TrySingle(out var expression) &&
                               MemberPath.TryFindRoot(expression, out disposedMember);
                    }
                }
            }

            return false;
        }

        internal static bool IsDisposing(InvocationExpressionSyntax disposeCall, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryGetDisposed(disposeCall, semanticModel, cancellationToken, out var disposed))
            {
                if (disposed.Equals(symbol))
                {
                    return true;
                }

                if (disposed is IPropertySymbol property &&
                    property.TrySingleDeclaration(cancellationToken, out var declaration))
                {
                    using (var walker = ReturnValueWalker.Borrow(declaration, ReturnValueSearch.TopLevel, semanticModel, cancellationToken))
                    {
                        return walker.TrySingle(out var returnValue) &&
                               MemberPath.TrySingle(returnValue, out var expression) &&
                               semanticModel.TryGetSymbol(expression, cancellationToken, out ISymbol nested) &&
                               nested.Equals(symbol);
                    }
                }
            }

            return false;
        }

        internal static bool IsIDisposableDispose(InvocationExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return candidate.TryGetMethodName(out var name) &&
                   name == "Dispose" &&
                   candidate.ArgumentList is ArgumentListSyntax argumentList &&
                   argumentList.Arguments.Count == 0 &&
                   !MemberPath.IsEmpty(candidate) &&
                   semanticModel.TryGetSymbol(candidate, cancellationToken, out var method) &&
                   method.ContainingType.IsAssignableTo(KnownSymbol.IDisposable, semanticModel.Compilation);
        }
    }
}
