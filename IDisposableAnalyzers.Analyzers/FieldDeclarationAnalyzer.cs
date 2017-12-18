﻿namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FieldDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP002DisposeMember.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleField, SyntaxKind.FieldDeclaration);
        }

        private static void HandleField(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.ContainingSymbol is IFieldSymbol field &&
                !field.IsStatic &&
                !field.IsConst &&
                Disposable.IsAssignedWithCreatedAndNotCachedOrInjected(field, context.SemanticModel, context.CancellationToken))
            {
                if (Disposable.IsMemberDisposed(field, context.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken)
                              .IsEither(Result.No, Result.AssumeNo, Result.Unknown))
                {
                    if (TestFixture.IsAssignedAndDisposedInSetupAndTearDown(field, context.Node.FirstAncestor<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken))
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(IDISP002DisposeMember.Descriptor, context.Node.GetLocation()));
                }
            }
        }
    }
}