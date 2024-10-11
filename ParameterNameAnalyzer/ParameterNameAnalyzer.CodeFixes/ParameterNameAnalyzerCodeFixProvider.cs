using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ParameterNameAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParameterNameCodeFixProvider)), Shared]
    public class ParameterNameCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add parameter name";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ParameterNameAnalyzer_Analyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the argument identified by the diagnostic.
            var argument = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ArgumentSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => AddParameterNameAsync(context.Document, argument, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private async Task<Document> AddParameterNameAsync(Document document, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var invocationExpression = argument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var methodSymbol = semanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;

            if (methodSymbol != null)
            {
                var parameterName = methodSymbol.Parameters[invocationExpression.ArgumentList.Arguments.IndexOf(argument)].Name;
                var newArgument = argument.WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(parameterName)));
                var newRoot = root.ReplaceNode(argument, newArgument);

                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }
    }
}
