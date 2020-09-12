using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExceptionAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CatchSystemExceptionAnalyzer : AnalyzerBase
    {
        public const string DiagnosticId = "EA007";
        internal const string Title = "do not catch System.Exception";
        internal const string MessageFormat = "Catch System.Exception without suitable handler seems a bad idea.";
        internal const string Category = "CodeSmell";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override SyntaxKind TargetSyntaxKind => SyntaxKind.CatchClause;

        protected override void Analyze(SyntaxNodeAnalysisContext context)
        {
            var catchBlock = context.Node as CatchClauseSyntax;
            
            if (catchBlock == null || catchBlock.Declaration == null || !CatchIsTooGeneric(context, catchBlock.Declaration))
            {
                return;
            }
            
            StatementSyntax syntax = catchBlock.Block;
            var suitable = syntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(s =>
            {
                return Config.SuitableHandlers.Contains(s.ToString());
            });

            if (!suitable)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, catchBlock.GetLocation()));
            }
        }
        
        private static bool CatchIsTooGeneric(SyntaxNodeAnalysisContext context, CatchDeclarationSyntax declaration)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(declaration.Type);
            if (symbol.Symbol == null)
            {
                return false;
            }

            var exception = context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Exception).FullName);
            return Equals(symbol.Symbol, exception);
        }
    }
}