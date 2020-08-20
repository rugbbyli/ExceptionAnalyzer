using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionAnalyzer
{
    public abstract class AnalyzerBase : DiagnosticAnalyzer
    {
        private const string configFileName = "ExceptionAnalyzer.config";

        protected static AnalyzerConfig Config { get; private set; }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeInternal, TargetSyntaxKind);
        }

        protected abstract SyntaxKind TargetSyntaxKind { get; }

        private void AnalyzeInternal(SyntaxNodeAnalysisContext context)
        {
            if(Config == null)
            {
                Config = new AnalyzerConfig();
                
                var configFile = context.Options.AdditionalFiles.FirstOrDefault(f => Path.GetFileName(f.Path) == configFileName);
                if (configFile != null)
                {
                    Config.Init(configFile);
                }
            }

            //如果存在rootPath，则跳过不在rootPath下的
            //跳过在ignorePaths下的

            var loc = context.Node.GetLocation();
            if(loc.IsInSource)
            {
                var filePath = loc.SourceTree.FilePath;
                if (Config.Root != null && !filePath.StartsWith(Config.Root)) return;

                foreach(var path in Config.ExcludeFolders)
                {
                    if (filePath.StartsWith(path)) return;
                }
            }

            Analyze(context);
        }
        
        protected abstract void Analyze(SyntaxNodeAnalysisContext context);
    }
}
