namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsCachedOrInjected(ExpressionSyntax value, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetSymbolSafe(value, cancellationToken);
            if (IsInjectedCore(symbol).IsEither(Result.Yes, Result.AssumeYes))
            {
                return true;
            }

            if (symbol is IPropertySymbol property &&
                !property.IsAutoProperty())
            {
                using (var returnValues = ReturnValueWalker.Borrow(value, ReturnValueSearch.TopLevel, semanticModel, cancellationToken))
                {
                    using (var recursive = RecursiveValues.Borrow(returnValues, semanticModel, cancellationToken))
                    {
                        return IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes);
                    }
                }
            }

            return IsAssignedWithInjected(symbol, location, semanticModel, cancellationToken);
        }

        internal static Result IsAnyCachedOrInjected(RecursiveValues values, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (values.IsEmpty)
            {
                return Result.No;
            }

            var result = Result.No;
            values.Reset();
            while (values.MoveNext())
            {
                if (values.Current is ElementAccessExpressionSyntax elementAccess)
                {
                    var symbol = semanticModel.GetSymbolSafe(elementAccess.Expression, cancellationToken);
                    var isInjected = IsInjectedCore(symbol);
                    if (isInjected == Result.Yes)
                    {
                        return Result.Yes;
                    }

                    if (isInjected == Result.AssumeYes)
                    {
                        result = Result.AssumeYes;
                    }

                    using (var assignedValues = AssignedValueWalker.Borrow(values.Current, semanticModel, cancellationToken))
                    {
                        using (var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken))
                        {
                            isInjected = IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken);
                            if (isInjected == Result.Yes)
                            {
                                return Result.Yes;
                            }

                            if (isInjected == Result.AssumeYes)
                            {
                                result = Result.AssumeYes;
                            }
                        }
                    }
                }
                else if (semanticModel.TryGetSymbol(values.Current, cancellationToken, out var symbol))
                {
                    switch (IsInjectedCore(symbol))
                    {
                        case Result.Yes:
                            return Result.Yes;
                        case Result.AssumeYes:
                            result = Result.AssumeYes;
                            break;
                    }
                }
            }

            return result;
        }

        private static Result IsInjectedCore(ISymbol symbol)
        {
            if (symbol == null)
            {
                return Result.Unknown;
            }

            if (symbol is ILocalSymbol)
            {
                return Result.Unknown;
            }

            if (symbol is IParameterSymbol)
            {
                return Result.Yes;
            }

            if (symbol is IFieldSymbol field)
            {
                if (field.IsStatic ||
                    field.IsAbstract ||
                    field.IsVirtual)
                {
                    return Result.Yes;
                }

                if (field.IsReadOnly)
                {
                    return Result.No;
                }

                return field.DeclaredAccessibility != Accessibility.Private
                           ? Result.AssumeYes
                           : Result.No;
            }

            if (symbol is IPropertySymbol property)
            {
                if (property.IsStatic ||
                    property.IsVirtual ||
                    property.IsAbstract)
                {
                    return Result.Yes;
                }

                if (property.IsReadOnly ||
                    property.SetMethod == null)
                {
                    return Result.No;
                }

                return property.DeclaredAccessibility != Accessibility.Private &&
                       property.SetMethod.DeclaredAccessibility != Accessibility.Private
                           ? Result.AssumeYes
                           : Result.No;
            }

            return Result.No;
        }

        private static bool IsAssignedWithInjected(ISymbol symbol, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var assignedValues = AssignedValueWalker.Borrow(symbol, location, semanticModel, cancellationToken))
            {
                using (var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken))
                {
                    return IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes);
                }
            }
        }
    }
}
