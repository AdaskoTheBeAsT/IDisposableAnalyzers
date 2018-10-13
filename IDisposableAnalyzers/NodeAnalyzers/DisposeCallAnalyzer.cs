namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DisposeCallAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP007DontDisposeInjected.Descriptor,
            IDISP016DontUseDisposedInstance.Descriptor,
            IDISP017PreferUsing.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.InvocationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is InvocationExpressionSyntax invocation &&
                DisposeCall.IsIDisposableDispose(invocation, context.SemanticModel, context.CancellationToken) &&
                !invocation.TryFirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>(out _) &&
                DisposeCall.TryGetDisposedRootMember(invocation, context.SemanticModel, context.CancellationToken, out var root))
            {
                if (Disposable.IsCachedOrInjected(root, invocation, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP007DontDisposeInjected.Descriptor, invocation.FirstAncestorOrSelf<StatementSyntax>()?.GetLocation() ?? invocation.GetLocation()));
                }
                else if (context.SemanticModel.TryGetSymbol(root, context.CancellationToken, out ILocalSymbol local))
                {
                    if (IsUsedAfter(local, invocation, context, out var locations))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP016DontUseDisposedInstance.Descriptor, invocation.FirstAncestorOrSelf<StatementSyntax>()?.GetLocation() ?? invocation.GetLocation(), additionalLocations: locations));
                    }

                    if (IsPreferUsing(local, invocation, context))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP017PreferUsing.Descriptor, invocation.GetLocation()));
                    }
                }
            }
        }

        private static bool IsUsedAfter(ILocalSymbol local, InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context, out IReadOnlyList<Location> locations)
        {
            if (local.TrySingleDeclaration(context.CancellationToken, out var declaration) &&
                declaration.TryFirstAncestor(out BlockSyntax block))
            {
                List<Location> temp = null;
                using (var walker = IdentifierNameWalker.Borrow(block))
                {
                    foreach (var identifierName in walker.IdentifierNames)
                    {
                        if (identifierName.Identifier.ValueText == local.Name &&
                            invocation.IsExecutedBefore(identifierName) == true &&
                            context.SemanticModel.TryGetSymbol(identifierName, context.CancellationToken, out ILocalSymbol candidate) &&
                            local.Equals(candidate) &&
                            !IsAssigned(identifierName) &&
                            !IsReassigned(identifierName))
                        {
                            if (temp == null)
                            {
                                temp = new List<Location>();
                            }

                            temp.Add(identifierName.GetLocation());
                        }
                    }

                    locations = temp;
                    return locations != null;
                }
            }

            locations = null;
            return false;

            bool IsAssigned(IdentifierNameSyntax identifier)
            {
                switch (identifier.Parent)
                {
                    case AssignmentExpressionSyntax assignment:
                        return assignment.Left == identifier;
                    case ArgumentSyntax argument when argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword):
                        return true;
                }

                return false;
            }

            bool IsReassigned(SyntaxNode location)
            {
                using (var walker = AssignedValueWalker.Borrow(local, context.SemanticModel, context.CancellationToken))
                {
                    foreach (var value in walker)
                    {
                        if (invocation.IsExecutedBefore(value) != false &&
                            value.IsExecutedBefore(location) != false)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private static bool IsPreferUsing(ILocalSymbol local, InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
        {
            return local.TrySingleDeclaration(context.CancellationToken, out var declaration) &&
                   declaration is VariableDeclaratorSyntax declarator &&
                   declaration.TryFirstAncestor(out LocalDeclarationStatementSyntax localDeclarationStatement) &&
                   invocation.TryFirstAncestor(out ExpressionStatementSyntax expressionStatement) &&
                   localDeclarationStatement.Parent == expressionStatement.Parent &&
                   Disposable.IsCreation(declarator.Initializer?.Value, context.SemanticModel, context.CancellationToken) == Result.Yes &&
                   !IsMutated();

            bool IsMutated()
            {
                using (var walker = MutationWalker.For(local, context.SemanticModel, context.CancellationToken))
                {
                    return walker.All().Any();
                }
            }
        }
    }
}
