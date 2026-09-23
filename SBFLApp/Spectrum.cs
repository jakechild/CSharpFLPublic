using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SBFLApp
{
    internal class Spectrum
    {
        private static readonly CSharpParseOptions _parseOptions = new(
            documentationMode: DocumentationMode.None
        );

        /// <summary>
        /// Reads the source into a syntax tree representation.  Searches for the method to modify.
        /// If the method is located, then coverage data is written to each statement in the method.
        /// The source file is re-written using the modified syntax tree representation.
        /// </summary>
        /// <param name="filePath">The path to the file to be modified.</param>
        /// <param name="methodName">The name of the method to modify.</param>
        /// <param name="coverageFileName">The name of the coverage file to use in the modified statement.</param>
        public static void SpectrumMethod(string filePath, string methodName, string? coverageFileName = null)
        {
            // Read in the source code that contains the method.
            var sourceCode = File.ReadAllText(filePath);

            // Create an abstract syntax tree representation of the source code.
            var tree = CSharpSyntaxTree.ParseText(sourceCode, _parseOptions);
            var root = tree.GetRoot();

            // Search for the method to modify.
            var method = SyntaxTreeHelpers.FindMethod(root, methodName);

            // Method wasn't found, return;
            if (method == null)
            {
                Console.WriteLine($"Method '{methodName}' not found.");
                return;
            }

            // Inject the coverage statements into the method.
            var rewriter = new CoverageInjector(methodName, coverageFileName, null, filePath);
            if (rewriter.Visit(root) is not CompilationUnitSyntax rewrittenRoot)
            {
                Console.WriteLine($"Failed to rewrite method '{methodName}' in '{filePath}'.");
                return;
            }

            // Get the method from the rewritten/modified root.
            var updatedMethod = rewrittenRoot
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == methodName);

            if (updatedMethod is null)
            {
                Console.WriteLine($"Rewritten method '{methodName}' not found in '{filePath}'.");
                return;
            }

            // Write the modified file back to the file system.
            var newRoot = rewrittenRoot;
            File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
            GuidMappingStore.Flush();
            Console.WriteLine($"Injected logging into all statements in '{methodName}' in '{filePath}'.");
        }

        /// <summary>
        /// Inject coverage logging into all methods within the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        public static void SpectrumAll(string filePath, string? coverageFileName = null)
        {
            ConsoleLogger.Info($"Injecting logging into all methods in '{filePath}'.");

            // Read the file and convert to a syntax tree node.
            var sourceCode = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(sourceCode, _parseOptions);
            var root = tree.GetRoot();

            // Utilize a coverage injector object to inject coverage and then write the file back to the file system.
            var rewriter = new CoverageInjector(sourceFilePath: filePath, coverageFileName: coverageFileName);
            var newRoot = rewriter.Visit(root);
            File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
            GuidMappingStore.Flush();
        }

        /// <summary>
        /// Remove instrumentation data from specified files.
        /// </summary>
        /// <param name="sourceFiles">The files to remove instrumentation from.</param>
        public static void ResetInstrumentation(IReadOnlyList<string> sourceFiles)
        {
            var cleanupRewriter = new CoverageCleanupRewriter();
            var processedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int sourceFilesCleaned = 0;

            // Go through all of the paths to clean up.
            foreach (var path in sourceFiles)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Reset skipped missing file: {path}");
                    continue;
                }

                sourceFilesCleaned += RemoveInstrumentationFromFile(path, cleanupRewriter) ? 1 : 0;
            }

            GuidMappingStore.Clear();

            Console.WriteLine($"Instrumentation reset complete. Cleaned {sourceFilesCleaned} source files.");
        }

        // Instrument every C# file in the project for coverage and save modified copies to a coverage folder
        public static void SpectrumProject(string projectPath)
        {
            var coverageFolder = Path.Combine(projectPath, "coverage");
            Directory.CreateDirectory(coverageFolder);

            var coverageFolderFullPath = Path.GetFullPath(coverageFolder) + Path.DirectorySeparatorChar;

            var allCsFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f =>
                    !f.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase) &&
                    !f.Contains(@"\obj\") &&
                    !f.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) &&
                    !f.EndsWith("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) &&
                    !f.EndsWith("GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase) &&
                    !Path.GetFullPath(f).StartsWith(coverageFolderFullPath, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            foreach (var file in allCsFiles)
            {
                Console.WriteLine($"Processing file: {file}");

                var sourceCode = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(sourceCode, _parseOptions);
                var root = tree.GetRoot();

                var rewriter = new CoverageInjector(sourceFilePath: file);
                var newRoot = rewriter.Visit(root);

                var fileName = Path.GetFileName(file);
                var destFilePath = Path.Combine(coverageFolder, fileName);

                File.WriteAllText(destFilePath, newRoot.NormalizeWhitespace().ToFullString());

                Console.WriteLine($"Written instrumented file to: {destFilePath}");
                GuidMappingStore.Flush();
            }
        }

        private static bool RemoveInstrumentationFromFile(string file, CoverageCleanupRewriter rewriter)
        {

            var originalText = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(originalText, _parseOptions);
            var root = tree.GetRoot();
            SyntaxNode? cleanedRoot = rewriter.Visit(root);

            if (cleanedRoot == null || SyntaxFactory.AreEquivalent(root, cleanedRoot))
            {
                return false;
            }

            var newText = cleanedRoot.ToFullString();
            if (!string.Equals(originalText, newText, StringComparison.Ordinal))
            {
                File.WriteAllText(file, newText);
            }
            return true;
        }

        private static bool IsInIgnoredDirectory(string filePath)
        {
            var normalizedPath = filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return normalizedPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.Contains($"{Path.DirectorySeparatorChar}coverage{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.Contains($".coverage{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
        }
    }
}
