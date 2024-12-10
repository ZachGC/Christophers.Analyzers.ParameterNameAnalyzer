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
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

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
            if (context.Node is not InvocationExpressionSyntax invocationExpression)
                return;

            // Exclude files in the "Migrations" folder
            var filePath = context.Node.SyntaxTree.FilePath;
            if (filePath != null && filePath.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar))
                return;

            if (invocationExpression.ArgumentList is null)
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpression);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                return;

            // If EF expression trees or similar scenarios should be excluded, add logic here:
            if (MethodExpectsExpression(methodSymbol))
                return;

            foreach (var argument in invocationExpression.ArgumentList.Arguments)
            {
                if (argument.NameColon is null)
                {
                    var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), argument.ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeConstructorInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ObjectCreationExpressionSyntax objectCreationExpression)
                return;

            // Exclude files in the "Migrations" folder
            var filePath = context.Node.SyntaxTree.FilePath;
            if (filePath != null && filePath.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar))
                return;

            if (objectCreationExpression.ArgumentList is null)
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(objectCreationExpression);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                return;

            if (MethodExpectsExpression(methodSymbol))
                return;

            foreach (var argument in objectCreationExpression.ArgumentList.Arguments)
            {
                if (argument.NameColon is null)
                {
                    var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), argument.ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool MethodExpectsExpression(IMethodSymbol methodSymbol)
        {
            foreach (var param in methodSymbol.Parameters)
            {
                var typeName = param.Type.ToDisplayString();
                if (typeName.StartsWith("System.Linq.Expressions.Expression"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
