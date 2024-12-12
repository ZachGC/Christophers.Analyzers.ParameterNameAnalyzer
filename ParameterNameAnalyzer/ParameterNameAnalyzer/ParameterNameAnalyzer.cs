using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParameterNameAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ParameterNameAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PARAM001";
        private const string Title = "Parameter names should be explicitly specified";
        private const string MessageFormat = "Parameter name is missing for argument '{0}'";
        private const string Description = "All method calls should use explicit parameter names.";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
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
            if (context.Node is not InvocationExpressionSyntax invocation)
                return;

            var filePath = context.Node.SyntaxTree.FilePath;
            if (filePath != null && filePath.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar))
                return;

            if (invocation.ArgumentList is null)
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                return;

            ReportDiagnosticsForMissingNames(context, invocation.ArgumentList, methodSymbol);
        }

        private static void AnalyzeConstructorInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ObjectCreationExpressionSyntax objectCreation)
                return;

            var filePath = context.Node.SyntaxTree.FilePath;
            if (filePath != null && filePath.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar))
                return;

            if (objectCreation.ArgumentList is null)
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                return;

            ReportDiagnosticsForMissingNames(context, objectCreation.ArgumentList, methodSymbol);
        }

        private static void ReportDiagnosticsForMissingNames(SyntaxNodeAnalysisContext context, ArgumentListSyntax argumentList, IMethodSymbol methodSymbol)
        {
            var arguments = argumentList.Arguments;
            int paramCount = methodSymbol.Parameters.Length;

            // Handle normal parameters first
            for (int i = 0; i < arguments.Count && i < paramCount; i++)
            {
                var argument = arguments[i];
                var parameter = methodSymbol.Parameters[i];

                // If this parameter is params, handle differently after this loop
                if (!parameter.IsParams && argument.NameColon is null)
                {
                    var diag = Diagnostic.Create(Rule, argument.GetLocation(), argument.ToString());
                    context.ReportDiagnostic(diag);
                }
            }

            // Handle params parameter (if any)
            if (paramCount > 0 && methodSymbol.Parameters[paramCount - 1].IsParams)
            {
                var paramsIndex = paramCount - 1;
                var paramsParameter = methodSymbol.Parameters[paramsIndex];

                // All arguments from paramsIndex onward belong to the params parameter
                for (int i = paramsIndex; i < arguments.Count; i++)
                {
                    var argument = arguments[i];
                    if (argument.NameColon == null)
                    {
                        var diag = Diagnostic.Create(Rule, argument.GetLocation(), argument.ToString());
                        context.ReportDiagnostic(diag);
                    }
                }
            }
        }
    }
}
