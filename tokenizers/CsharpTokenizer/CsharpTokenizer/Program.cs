using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace CsharpTokenizer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage <projectsFolder> <outputFolder> true|false");
                return;
            }
            Parallel.ForEach(
                Directory.EnumerateDirectories(args[0]),
                d=>ExtractForProjectFolder(d, args[1], bool.Parse(args[2]), args[0])
            );
        }

        public static void ExtractForProjectFolder(string projectDir, string outputDir, bool onlyIdentifiers, string baseDir)
        {
            var allFiles = Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories);

            var projectDirName = Path.GetFileName(projectDir);

            using (var fileStream = File.Create(Path.Combine(outputDir, projectDirName + ".jsonl.gz")))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Compress, false))
            using (var textStream = new StreamWriter(gzipStream))
            {
                foreach (var fileJson in allFiles.AsParallel().Select(f => GetJsonForFile(f, onlyIdentifiers, baseDir)))
                {
                    textStream.WriteLine(fileJson);
                }
            }
        }

        private static string GetJsonForFile(string filepath, bool onlyIdentifiers, string baseDir)
        {
            var tokens = GetFileIdentifierTokens(filepath, onlyIdentifiers);
            Debug.Assert(filepath.StartsWith(baseDir));
            var relativePath = Path.GetRelativePath(baseDir, filepath);
            var tokenData = new TokenData()
            {
                tokens = tokens.ToArray(),
                filename = relativePath
            };

            return JsonConvert.SerializeObject(tokenData);
        }

        private static IEnumerable<string> GetFileIdentifierTokens(string filepath, bool onlyIdentifiers)
        {
            var tokens = GetASTFromFile(filepath).GetRoot().DescendantTokens();
            if (onlyIdentifiers) {
                tokens = tokens.Where(t => t.IsKind(SyntaxKind.IdentifierToken));
            }
            return tokens.Select(t => t.Text);
        }

        private static IEnumerable<SyntaxToken> GetFileTokens(string filepath) =>
           GetASTFromFile(filepath).GetRoot().DescendantTokens();

        private static SyntaxTree GetASTFromFile(string filePath)
        {
            using (var rawSource = new StreamReader(filePath))
            {
                return CSharpSyntaxTree.ParseText(rawSource.ReadToEnd());
            }
        }
    }

    public struct TokenData
    {
        public string filename;
        public string[] tokens;
    }
}
