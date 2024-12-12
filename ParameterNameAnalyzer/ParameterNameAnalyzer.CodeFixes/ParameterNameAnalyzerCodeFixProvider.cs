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
            var span = diagnostic.Location.SourceSpan;

            var argumentNode = root.FindNode(span);
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

            // Try to find the call (invocation or constructor)
            var invocation = argument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var objectCreation = argument.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>();

            var callNode = (SyntaxNode)invocation ?? objectCreation;
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
            var argIndex = arguments.IndexOf(argument);
            if (argIndex < 0 || argIndex >= methodSymbol.Parameters.Length)
                return document;

            var parameter = methodSymbol.Parameters[argIndex];
            if (argument.NameColon != null)
            {
                // Already named
                return document;
            }

            if (parameter.IsParams)
            {
                // Handle params
                var paramsArgs = arguments.Skip(argIndex).ToList();
                ExpressionSyntax newExpression;
                if (paramsArgs.Count == 1)
                {
                    newExpression = paramsArgs[0].Expression;
                }
                else
                {
                    var elementType = ((IArrayTypeSymbol)parameter.Type).ElementType;
                    var elementTypeSyntax = SyntaxFactory.ParseTypeName(elementType.ToDisplayString());
                    var arrayInitializer = SyntaxFactory.InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SyntaxFactory.SeparatedList(paramsArgs.Select(a => a.Expression)));

                    newExpression = SyntaxFactory.ArrayCreationExpression(
                        SyntaxFactory.ArrayType(elementTypeSyntax)
                            .WithRankSpecifiers(SyntaxFactory.SingletonList(
                                SyntaxFactory.ArrayRankSpecifier(
                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                        SyntaxFactory.OmittedArraySizeExpression())))),
                        arrayInitializer);
                }

                var namedArgument = SyntaxFactory.Argument(
                    SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(parameter.Name)),
                    default,
                    newExpression)
                    .WithTriviaFrom(paramsArgs.First());

                var newArgs = arguments.Take(argIndex).Append(namedArgument);
                var newArgList = argumentList.WithArguments(SyntaxFactory.SeparatedList(newArgs));
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
