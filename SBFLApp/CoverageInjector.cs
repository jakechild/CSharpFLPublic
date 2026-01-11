using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SBFLApp
{
    // Rewrites C# syntax tree nodes to inject coverage logging statements
    internal class CoverageInjector : CSharpSyntaxRewriter
    {
        private readonly string? _targetMethodName;
        private readonly string? _coverageFileName;
        private readonly List<string>? _guidCollector;
        private readonly string? _sourceFilePath;
        private readonly Stack<string> _namespaceStack = new();
        private readonly Stack<string> _typeStack = new();
        private string _currentMethodName = "";

        public CoverageInjector(string? methodName = null, string? coverageFileName = null, List<string>? guidCollector = null, string? sourceFilePath = null)
        {
            _targetMethodName = methodName;
            _coverageFileName = coverageFileName;
            _guidCollector = guidCollector;
            _sourceFilePath = sourceFilePath;
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            _namespaceStack.Push(node.Name.ToString());
            var result = base.VisitNamespaceDeclaration(node);
            _namespaceStack.Pop();
            return result ?? node;
        }

        public override SyntaxNode VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            _namespaceStack.Push(node.Name.ToString());
            var result = base.VisitFileScopedNamespaceDeclaration(node);
            _namespaceStack.Pop();
            return result ?? node;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _typeStack.Push(node.Identifier.Text);
            var result = base.VisitClassDeclaration(node);
            _typeStack.Pop();
            return result ?? node;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            _typeStack.Push(node.Identifier.Text);
            var result = base.VisitStructDeclaration(node);
            _typeStack.Pop();
            return result ?? node;
        }

        /// <summary>
        /// Visit method declarations, process only the target method if specified.
        /// </summary>
        /// <param name="node">The node to get the method name from.</param>
        /// <returns>The node that has been proessed.</returns>
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (_targetMethodName != null && node.Identifier.Text != _targetMethodName)
                return node;

            _currentMethodName = node.Identifier.Text;
            return base.VisitMethodDeclaration(node) ?? node;
        }

        /// <summary>
        /// Visit blocks and inject logging statements before each original statement
        /// </summary>
        /// <param name="node">The block of code to modify.</param>
        /// <returns>Returns the instrumented block of code.</returns>
        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            var newStatements = new List<StatementSyntax>();

            // Go through all the statements in the node.
            foreach (var statement in node.Statements)
            {
                // Skip instrumentation if this statement is already a logging statement
                var statementText = statement.ToString();
                if (ContainsCoverageLogger(statementText))
                {
                    //TODO: Does this GUID need to be added to the _guidCollector?
                    newStatements.Add(statement);
                    continue;
                }

                // Generate a GUID for the instrumentation statement and add it to the list of GUIDs.
                var guid = Guid.NewGuid().ToString();
                _guidCollector?.Add(guid);

                var coverageFilePath = _coverageFileName ?? $"{_currentMethodName}.coverage";

                // Create the statement to be added to the code.
                var logStatement = SyntaxFactory.ParseStatement(
                    $"SBFLApp.CoverageLogger.Log(\"{EscapeString(coverageFilePath)}\", \"{guid}\");"
                );

                // Add the guid, qualified method name, and the source file name to the GUID mapping store.
                var qualifiedName = GetQualifiedMethodName();
                if (!string.IsNullOrEmpty(qualifiedName))
                {
                    var sourceFileName = string.IsNullOrWhiteSpace(_sourceFilePath)
                        ? null
                        : Path.GetFileName(_sourceFilePath);
                    GuidMappingStore.AddMapping(guid, qualifiedName, sourceFileName);
                }

                // Add the new log statement.
                newStatements.Add(logStatement);

                // Add the current statement after the log statement.
                var visitedStatement = (StatementSyntax)Visit(statement);
                newStatements.Add(visitedStatement);
            }

            // Update the node with the new statements.  This creates a new node.
            return node.WithStatements(SyntaxFactory.List(newStatements));
        }

        /// <summary>
        /// Get the qualified name of the method.
        /// </summary>
        /// <returns></returns>
        private string GetQualifiedMethodName()
        {
            if (string.IsNullOrEmpty(_currentMethodName))
            {
                return string.Empty;
            }

            var namespacePrefix = _namespaceStack.Count > 0
                ? string.Join('.', _namespaceStack.Reverse()) + "."
                : string.Empty;

            var typePrefix = _typeStack.Count > 0
                ? string.Join('.', _typeStack.Reverse()) + "."
                : string.Empty;

            return $"{namespacePrefix}{typePrefix}{_currentMethodName}";
        }

        /// <summary>
        /// Change escape strings {\\} to {\\\\} and {\"} to {\\\"} in the given string.
        /// </summary>
        /// <param name="value">The string to update.</param>
        /// <returns>The modified string.</returns>
        private static string EscapeString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static bool ContainsCoverageLogger(string statementText)
        {
            return statementText.Contains("SBFLApp.CoverageLogger.Log", StringComparison.Ordinal)
                || statementText.Contains("System.IO.File.AppendAllText", StringComparison.Ordinal);
        }
    }
}
