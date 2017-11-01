namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddToCompositeDisposableCodeFixProvider))]
    [Shared]
    internal class AddToCompositeDisposableCodeFixProvider : CodeFixProvider
    {
        private static readonly TypeSyntax CompositeDisposableType = SyntaxFactory.ParseTypeName("System.Reactive.Disposables.CompositeDisposable")
                                                                                  .WithAdditionalAnnotations(Simplifier.Annotation);

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP004DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == IDISP004DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId)
                {
                    var statement = node.FirstAncestorOrSelf<ExpressionStatementSyntax>();
                    if (statement != null)
                    {
                        if (TryGetField(statement, semanticModel, context.CancellationToken, out IFieldSymbol field))
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Add to CompositeDisposable.",
                                    cancellationToken => AddAsync(context.Document, statement, field, cancellationToken),
                                    nameof(AddToCompositeDisposableCodeFixProvider)),
                                diagnostic);
                        }
                        else
                        {
                            if (semanticModel.Compilation.ReferencedAssemblyNames.Any(
                                x => x.Name.Contains("System.Reactive")))
                            {
                                context.RegisterCodeFix(
                                    CodeAction.Create(
                                        "Add to new CompositeDisposable.",
                                        cancellationToken => CreateAndInitializeAsync(context.Document, statement, cancellationToken),
                                        nameof(AddToCompositeDisposableCodeFixProvider)),
                                    diagnostic);
                            }
                        }
                    }
                }
            }
        }

        private static async Task<Document> AddAsync(Document document, ExpressionStatementSyntax statement, IFieldSymbol field, CancellationToken cancellationToken)
        {
            bool TryGetPreviousStatement(StatementSyntax s, out StatementSyntax result)
            {
                result = null;
                if (s.Parent is BlockSyntax block)
                {
                    var index = block.Statements.IndexOf(statement);
                    if (index > 0)
                    {
                        result = block.Statements[index - 1];
                        return true;
                    }
                }

                return false;
            }

            bool TryGetCreateCompositeDisposable(StatementSyntax s, IFieldSymbol f, out ObjectCreationExpressionSyntax result)
            {
                if (s is ExpressionStatementSyntax expressionStatement &&
                    expressionStatement.Expression is AssignmentExpressionSyntax assignment &&
                    assignment.Right is ObjectCreationExpressionSyntax objectCreation)
                {
                    if ((assignment.Left is IdentifierNameSyntax identifierName &&
                         identifierName.Identifier.ValueText == f.Name) ||
                        (assignment.Left is MemberAccessExpressionSyntax memberAccess &&
                         memberAccess.Expression is ThisExpressionSyntax &&
                         memberAccess.Name.Identifier.ValueText == f.Name))
                    {
                        result = objectCreation;
                        return true;
                    }
                }

                result = null;
                return false;
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken)
                                             .ConfigureAwait(false);
            if (TryGetPreviousStatement(statement, out var previous) &&
                TryGetCreateCompositeDisposable(previous, field, out var compositeDisposableCreation))
            {
                editor.RemoveNode(statement);
                editor.AddItemToCollectionInitializer(
                    compositeDisposableCreation,
                    statement.Expression,
                    statement.GetTrailingTrivia());
                return editor.GetChangedDocument();
            }

            var usesUnderscoreNames = statement.UsesUnderscore(editor.SemanticModel, cancellationToken);
            var memberAccessExpressionSyntax = usesUnderscoreNames
                                                   ? (MemberAccessExpressionSyntax)editor.Generator.MemberAccessExpression(SyntaxFactory.IdentifierName(field.Name), "Add")
                                                   : (MemberAccessExpressionSyntax)editor.Generator.MemberAccessExpression(editor.Generator.MemberAccessExpression(SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName(field.Name)), "Add");

            editor.ReplaceNode(
                statement,
                SyntaxFactory.ExpressionStatement(
                    (InvocationExpressionSyntax)editor.Generator.InvocationExpression(
                        memberAccessExpressionSyntax,
                        statement.Expression)));
            return editor.GetChangedDocument();
        }

        private static async Task<Document> CreateAndInitializeAsync(Document document, ExpressionStatementSyntax statement, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken)
                                             .ConfigureAwait(false);
            var containingType = statement.FirstAncestor<TypeDeclarationSyntax>();
            var usesUnderscoreNames = statement.UsesUnderscore(editor.SemanticModel, cancellationToken);
            var field = editor.AddField(
                containingType,
                usesUnderscoreNames
                    ? "_disposable"
                    : "disposable",
                Accessibility.Private,
                DeclarationModifiers.ReadOnly,
                CompositeDisposableType,
                cancellationToken);

            var fieldAccess = usesUnderscoreNames
                                  ? SyntaxFactory.IdentifierName(field.Name())
                                  : SyntaxFactory.ParseExpression($"this.{field.Name()}");

            var trailingTrivia = statement.GetTrailingTrivia();
            if (trailingTrivia.Any(SyntaxKind.SingleLineCommentTrivia))
            {
                var padding = new string(' ', statement.GetLeadingTrivia().Span.Length);
                var code = StringBuilderPool.Borrow()
                                            .AppendLine($"{padding}{fieldAccess} = new System.Reactive.Disposables.CompositeDisposable")
                                            .AppendLine($"{padding}{{")
                                            .AppendLine($"    {statement.GetLeadingTrivia()}{statement.Expression},{trailingTrivia.ToString().Trim('\r', '\n')}")
                                            .AppendLine($"{padding}}};")
                                            .Return();

                editor.ReplaceNode(
                    statement,
                    SyntaxFactory.ParseStatement(code)
                                 .WithSimplifiedNames());
            }
            else
            {
                editor.ReplaceNode(
                    statement,
                    SyntaxFactory.ParseStatement($"{fieldAccess} = new System.Reactive.Disposables.CompositeDisposable {{ {statement.Expression} }};")
                                 .WithAdditionalAnnotations(Formatter.Annotation)
                                 .WithSimplifiedNames());
            }

            return editor.GetChangedDocument();
        }

        private static bool TryGetField(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, out IFieldSymbol field)
        {
            field = null;
            var typeDeclaration = node.FirstAncestor<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
            {
                return false;
            }

            var type = semanticModel.GetDeclaredSymbolSafe(typeDeclaration, cancellationToken);
            if (type == null)
            {
                return false;
            }

            foreach (var member in type.GetMembers())
            {
                if (member is IFieldSymbol candidateField &&
                    candidateField.Type == KnownSymbol.CompositeDisposable)
                {
                    field = candidateField;
                    return true;
                }
            }

            return false;
        }
    }
}