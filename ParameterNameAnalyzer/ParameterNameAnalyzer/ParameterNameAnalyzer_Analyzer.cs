using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParameterNameAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ParameterNameAnalyzer_Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PARAM001";
        private static readonly LocalizableString Title = "Parameter names should be explicitly specified";
        private static readonly LocalizableString MessageFormat = "Parameter name is missing for argument '{0}'";
        private static readonly LocalizableString Description = "All method calls should use explicit parameter names.";
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeConstructorInvocation, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            // Get the method being invoked
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;

            if (methodSymbol is null) return;

            // Check arguments in the method call
            foreach (var argument in invocationExpression.ArgumentList.Arguments)
            {
                // Check if argument has no explicit parameter name (NameColon)
                if (argument.NameColon is null)
                {
                    var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), argument.ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeConstructorInvocation(SyntaxNodeAnalysisContext context)
        {
            var objectCreationExpression = (ObjectCreationExpressionSyntax)context.Node;

            // Same logic you use for methods, but applied to constructor arguments
            foreach (var argument in objectCreationExpression.ArgumentList.Arguments)
            {
                if (argument.NameColon is null)
                {
                    var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), argument.ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
