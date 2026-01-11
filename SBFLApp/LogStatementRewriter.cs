using System;
﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SBFLApp
{
    internal class LogStatementRewriter : CSharpSyntaxRewriter
    {
        private readonly string _coverageFileName;

        /// <summary>
        /// Main constructor used to specifiy the coverage file name.
        /// </summary>
        /// <param name="coverageFileName">The new coverage filename to use.</param>
        public LogStatementRewriter(string coverageFileName)
        {
            _coverageFileName = coverageFileName;
        }

        /// <summary>
        /// If the expression is a coverage statement, then update the expression with the new
        /// coverage file name.
        /// </summary>
        /// <param name="node">The <see cref="InvocationExpressionSyntax"/> node to check and update.</param>
        /// <returns>The updated <see cref="InvocationExpressionSyntax"/> node, or the original if no changes were made.</returns>
        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // If this isn't a coverage statement, then return the original node.
            if (!TryGetCoverageInvocationKind(node, out var invocationKind))
            {
                return base.VisitInvocationExpression(node) ?? node;
            }

            // Verify there are two arguments being passed into the coverage statement.
            var argumentList = node.ArgumentList;
            if (argumentList.Arguments.Count < 2)
            {
                return base.VisitInvocationExpression(node) ?? node;
            }

            var updatedArguments = argumentList.Arguments;

            var fileLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(_coverageFileName));

            var firstArgument = updatedArguments[0]
                .WithExpression(fileLiteral);

            updatedArguments = updatedArguments.Replace(updatedArguments[0], firstArgument);

            if (invocationKind == CoverageInvocationKind.AppendAllText)
            {
                var secondArgument = updatedArguments[1];
                if (!ContainsEnvironmentNewLine(secondArgument.Expression))
                {
                    var newlineExpression = SyntaxFactory.ParseExpression("System.Environment.NewLine");
                    var appendedExpression = SyntaxFactory.BinaryExpression(
                        SyntaxKind.AddExpression,
                        (ExpressionSyntax)secondArgument.Expression,
                        newlineExpression);

                    var updatedSecondArgument = secondArgument.WithExpression(appendedExpression);
                    updatedArguments = updatedArguments.Replace(secondArgument, updatedSecondArgument);
                }
            }

            var updatedArgumentList = argumentList.WithArguments(updatedArguments);

            return node.WithArgumentList(updatedArgumentList);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static bool TryGetCoverageInvocationKind(InvocationExpressionSyntax node, out CoverageInvocationKind invocationKind)
        {
            invocationKind = CoverageInvocationKind.CoverageLogger;

            if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            var target = memberAccess.ToString();
            return target switch
            {
                "System.IO.File.AppendAllText" => AssignKind(CoverageInvocationKind.AppendAllText, out invocationKind),
                "SBFLApp.CoverageLogger.Log" => AssignKind(CoverageInvocationKind.CoverageLogger, out invocationKind),
                "CoverageLogger.Log" => AssignKind(CoverageInvocationKind.CoverageLogger, out invocationKind),
                _ => false
            };
        }

        private static bool AssignKind(CoverageInvocationKind invocationKind, out CoverageInvocationKind output)
        {
            output = invocationKind;
            return true;
        }


        /// <summary>
        /// Checks to see if the given expression contains "Environment.NewLine"
        /// </summary>
        /// <param name="expression">The expression to search.</param>
        /// <returns>True if the expression contains "Environment.NewLine" and false otherwise.</returns>
        private static bool ContainsEnvironmentNewLine(ExpressionSyntax expression)
        {
            return expression.ToString().Contains("Environment.NewLine", StringComparison.Ordinal);
        }

        private enum CoverageInvocationKind
        {
            AppendAllText,
            CoverageLogger
        }
    }
}
