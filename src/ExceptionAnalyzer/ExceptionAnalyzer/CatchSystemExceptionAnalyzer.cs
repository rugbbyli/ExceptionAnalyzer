using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExceptionAnalyzer
{
    public class CatchSystemExceptionAnalyzer : AnalyzerBase
    {
        public const string DiagnosticId = "EA007";
        internal const string Title = "do not catch System.Exception";
        internal const string MessageFormat = "Catch all exception type seems a bad idea.";
        internal const string Category = "CodeSmell";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override SyntaxKind TargetSyntaxKind => SyntaxKind.CatchClause;

        protected override void Analyze(SyntaxNodeAnalysisContext context)
        {
            var catchBlock = context.Node as CatchClauseSyntax;
            
            if (catchBlock == null || catchBlock.Declaration == null || (context.SemanticModel.GetTypeInfo(catchBlock.Declaration.Type).Type.ToDisplayString() != "System.Exception"))
            {
                return;
            }
            
            StatementSyntax syntax = catchBlock.Block;
            var source = syntax.GetText().ToString();
            if (Config.SuitableHandlers.Any(source.Contains)) return;
            
            //todo
        }
    }
}