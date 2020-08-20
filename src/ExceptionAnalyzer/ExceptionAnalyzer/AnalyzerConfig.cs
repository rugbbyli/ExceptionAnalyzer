using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace ExceptionAnalyzer
{
    public class AnalyzerConfig
    {
        public string Root { get; private set; }
        public HashSet<string> ExcludeFolders { get; private set; } = new HashSet<string>();
        private const string optionRoot = "--root";
        private const string optionExclude = "--exclude";

        public AnalyzerConfig Init(AdditionalText configFile)
        {
            var pathBase = Path.GetDirectoryName(configFile.Path);
            var content = configFile.GetText();

            foreach (var line in content.Lines)
            {
                var txt = line.ToString();
                var splits = txt.Split(':');
                if (splits[0] == optionExclude)
                {
                    ExcludeFolders.Add(Path.GetFullPath(Path.Combine(pathBase, splits[1])));
                }
                else if (splits[0] == optionRoot)
                {
                    Root = Path.GetFullPath(Path.Combine(pathBase, splits[1]));
                }
            }
            
            return this;
        }
    }
}