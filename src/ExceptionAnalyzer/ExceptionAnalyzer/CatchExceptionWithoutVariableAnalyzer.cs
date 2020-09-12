using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExceptionAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CatchExceptionWithoutVariableAnalyzer : AnalyzerBase
    {
        public const string DiagnosticId = "EA008";
        internal const string Title = "Do not catch exception without variable declareation.";
        internal const string MessageFormat = "Catch exception without variable declareation lost the exception itself.";
        internal const string Category = "CodeSmell";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override SyntaxKind TargetSyntaxKind => SyntaxKind.CatchClause;

        protected override void Analyze(SyntaxNodeAnalysisContext context)
        {
            var catchBlock = context.Node as CatchClauseSyntax;
            
            if (catchBlock?.Declaration == null)
            {
                return;
            }

            if (catchBlock.Declaration.Identifier.IsKind(SyntaxKind.None))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, catchBlock.Declaration.GetLocation()));
            }
        }
    }
}