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
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
            isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Analyze invocation expressions (method calls)
            context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
            // Analyze object creation expressions (constructor calls)
            context.RegisterSyntaxNodeAction(AnalyzeConstructorInvocation, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = context.Node as InvocationExpressionSyntax;
            if (invocationExpression == null)
                return;

            // If the invocation doesn't have an argument list, nothing to do
            if (invocationExpression.ArgumentList == null)
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpression);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                return; // Symbol could not be resolved

            // Iterate over arguments and check for missing name colons
            foreach (var argument in invocationExpression.ArgumentList.Arguments)
            {
                if (argument.NameColon == null)
                {
                    var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), argument.ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeConstructorInvocation(SyntaxNodeAnalysisContext context)
        {
            var objectCreationExpression = context.Node as ObjectCreationExpressionSyntax;
            if (objectCreationExpression == null)
                return;

            // If the object creation doesn't have an argument list (e.g. `new MyClass` with no parentheses), no arguments to check
            if (objectCreationExpression.ArgumentList == null)
                return;

            // We could get symbol info to correlate arguments with parameters if needed, but 
            // since the original code just ensures that arguments have names, let's skip that for now.
            foreach (var argument in objectCreationExpression.ArgumentList.Arguments)
            {
                if (argument.NameColon == null)
                {
                    var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), argument.ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
