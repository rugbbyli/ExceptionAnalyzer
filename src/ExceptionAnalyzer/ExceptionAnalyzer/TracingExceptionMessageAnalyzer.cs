using ExceptionAnalyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ExceptionAnalyzer
{
    /// <summary>
    /// Checks that `catch` block uses `ex.Message`.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TracingExceptionMessageAnalyzer : AnalyzerBase
    {
        public const string DiagnosticId = "EA005";

        // TODO: extract all messages somewhere to be able to add errogant messages
        internal const string Title = "Tracing `ex.Message` considered harmful!";
        internal const string MessageFormat = "'{0}' contains a small portion of useful information. Observe whole exception instead!";
        internal const string Category = "CodeSmell";

        // It seems that System.ApplicationException is absent in Portable apps
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics  => ImmutableArray.Create(Rule);

        protected override SyntaxKind TargetSyntaxKind => SyntaxKind.IdentifierName;

        protected override void Analyze(SyntaxNodeAnalysisContext context)
        {
            // Casting Node object
            var identifier = context.Node as SimpleNameSyntax;

            // Not interesting with everything except "Message" names
            if (identifier.Identifier.Text != "Message")
            {
                return;
            }

            // Looking for the embracing catch clause
            var catchBlock = identifier.Parent.AncestorsAndSelf().OfType<CatchClauseSyntax>().FirstOrDefault();

            // Skipping empty catch blocks, catch blocks without any code, and catch blocks without exception identifiers!
            if (catchBlock == null || catchBlock.Block.Statements.Count == 0 ||
                catchBlock.Declaration == null || catchBlock.Declaration.Identifier.Kind() == SyntaxKind.None)
            {
                return;
            }

            // Interested only in generic exception handlers!
            if (CatchIsTooGeneric(context, catchBlock.Declaration))
            {
                // OK, there is no way to use SymbolFinder because that class requires solution, but solution is not awailable
                // at the analyzers level!
                // Looking for usages by hand!

                // Looking for all the usages of the Exception variable
                // Again, very naive approach! The same as in ThrowExAnalyzer!

                var exceptionDeclarationIdentifier = catchBlock.Declaration.Identifier;
                //var throwExIdentifierSymbol = context.SemanticModel.GetSymbolInfo(catchBlock.Declaration.Identifier.Parent);

                var usages = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Select(id => new { Symbol = context.SemanticModel.GetSymbolInfo(id), Id = id })
                    .Where(x => x.Symbol.Symbol != null && x.Symbol.Symbol.ExceptionFromCatchBlock())
                    .ToList();

                // First of all we should find all usages for ex.Message
                var messageUsages = usages
                    .Select(id => new { Parent = id.Id.Parent as MemberAccessExpressionSyntax, Id = id.Id })
                    .Where(x => x.Parent != null)
                    .ToList();

                if (messageUsages.Count == 0)
                {
                    // There would be no warnings! No ex.Message usages 
                    return;
                }

                bool wasObserved =
                    usages.Select(id => id.Id)
                    .Except(messageUsages.Select(x => x.Id))
                    .Any(u => u.Parent is ArgumentSyntax || // Exception object was used directly
                              u.Parent is EqualsValueClauseSyntax || // Was saved to field or local
                                                                     // or Inner exception was used
                              (u.Parent.As(x => x as MemberAccessExpressionSyntax)?.Name?.Identifier)?.Text == "InnerException");

                // If exception object was "observed" properly!
                if (wasObserved)
                {
                    return;
                }

                var location = identifier.GetLocation(); // Diagnostic should point to "Message" itself
                var text = identifier.Parent.GetText(); // whould be ex.Message

                var diagnostic = Diagnostic.Create(Rule, location, text);
                context.ReportDiagnostic(diagnostic);
            }
        }

        // TODO: copy-paste! Refactor if needed!
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
