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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParameterNameCodeFixProvider))]
    [Shared]
    public class ParameterNameCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add parameter name";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ParameterNameAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the argument identified by the diagnostic.
            var argumentNode = root.FindNode(diagnosticSpan);
            var argument = argumentNode.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault();
            if (argument == null)
                return;

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

            // Identify the call node
            var callNode = (SyntaxNode)argument.FirstAncestorOrSelf<InvocationExpressionSyntax>()
                         ?? argument.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>();

            if (callNode == null)
                return document;

            var argumentList = callNode switch
            {
                InvocationExpressionSyntax inv => inv.ArgumentList,
                ObjectCreationExpressionSyntax obj => obj.ArgumentList,
                _ => null
            };

            if (argumentList == null)
                return document;

            // Get the method symbol
            IMethodSymbol methodSymbol = null;
            if (callNode is InvocationExpressionSyntax invExp)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(invExp, cancellationToken);
                methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            }
            else if (callNode is ObjectCreationExpressionSyntax objExp)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(objExp, cancellationToken);
                methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            }

            if (methodSymbol == null)
                return document;

            var arguments = argumentList.Arguments;
            var argumentIndex = arguments.IndexOf(argument);
            if (argumentIndex < 0 || argumentIndex >= methodSymbol.Parameters.Length)
                return document;

            var parameter = methodSymbol.Parameters[argumentIndex];
            if (argument.NameColon != null)
            {
                // Already named
                return document;
            }

            // Handle params parameter:
            if (parameter.IsParams)
            {
                var paramsArguments = arguments.Skip(argumentIndex).ToList();
                ExpressionSyntax newExpression;
                if (paramsArguments.Count == 1)
                {
                    newExpression = paramsArguments[0].Expression;
                }
                else
                {
                    var elementType = ((IArrayTypeSymbol)parameter.Type).ElementType;
                    var elementTypeSyntax = SyntaxFactory.ParseTypeName(elementType.ToDisplayString());
                    var arrayInitializer = SyntaxFactory.InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SyntaxFactory.SeparatedList(paramsArguments.Select(a => a.Expression)));

                    var arrayCreation = SyntaxFactory.ArrayCreationExpression(
                        SyntaxFactory.ArrayType(elementTypeSyntax)
                            .WithRankSpecifiers(SyntaxFactory.SingletonList(
                                SyntaxFactory.ArrayRankSpecifier(
                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                        SyntaxFactory.OmittedArraySizeExpression())))),
                        arrayInitializer);

                    newExpression = arrayCreation;
                }

                var namedArgument = SyntaxFactory.Argument(
                    SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(parameter.Name)),
                    default,
                    newExpression)
                    .WithTriviaFrom(paramsArguments.First());

                var newArguments = arguments.Take(argumentIndex).Append(namedArgument);
                var newArgList = argumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments));

                return document.WithSyntaxRoot(root.ReplaceNode(argumentList, newArgList));
            }
            else
            {
                // Normal parameter
                var newArgument = argument.WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(parameter.Name)))
                                          .WithTriviaFrom(argument);

                var newRoot = root.ReplaceNode(argument, newArgument);
                return document.WithSyntaxRoot(newRoot);
            }
        }
    }
}
