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

        public sealed override ImmutableArray<string> FixableDiagnosticIds => [ParameterNameAnalyzer_Analyzer.DiagnosticId];

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
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Try to get the invocation or object creation that this argument is part of
            var invocationExpression = argument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var objectCreationExpression = argument.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>();

            IMethodSymbol methodSymbol = null;
            SeparatedSyntaxList<ArgumentSyntax> arguments = default;

            if (invocationExpression is not null)
            {
                // Method invocation scenario
                var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
                methodSymbol = symbolInfo.Symbol as IMethodSymbol;

                if (invocationExpression.ArgumentList != null)
                {
                    arguments = invocationExpression.ArgumentList.Arguments;
                }
            }
            else if (objectCreationExpression is not null)
            {
                // Constructor invocation scenario
                var symbolInfo = semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken);
                methodSymbol = symbolInfo.Symbol as IMethodSymbol;

                if (objectCreationExpression.ArgumentList != null)
                {
                    arguments = objectCreationExpression.ArgumentList.Arguments;
                }
            }

            if (methodSymbol is null || arguments.Count == 0)
            {
                // Could not resolve the method symbol or no arguments found; no fix
                return document;
            }

            // Find the parameter name by matching the argument's index
            var argumentIndex = arguments.IndexOf(argument);
            if (argumentIndex < 0 || argumentIndex >= methodSymbol.Parameters.Length)
            {
                // For some reason, we can't map argument to a parameter
                return document;
            }

            var parameterName = methodSymbol.Parameters[argumentIndex].Name;

            // Create a new argument with the NameColon
            var newArgument = argument.WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(parameterName)));

            var newRoot = root.ReplaceNode(argument, newArgument);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
