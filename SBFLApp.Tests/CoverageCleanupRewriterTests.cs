using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SBFLApp;

namespace SBFLApp.Tests;

public class CoverageCleanupRewriterTests
{
    [Fact]
    public void VisitExpressionStatement_RemovesCoverageStatement()
    {
        const string statementText = "SBFLApp.CoverageLogger.Log(\"test.coverage.tmp\", \"data\");";
        var statement = SyntaxFactory.ParseStatement(statementText) as ExpressionStatementSyntax;
        Assert.NotNull(statement);

        var rewriter = new CoverageCleanupRewriter();
        var result = rewriter.Visit(statement!);

        Assert.Null(result);
    }

    [Fact]
    public void VisitExpressionStatement_RemovesLegacyAppendStatement()
    {
        const string statementText = "System.IO.File.AppendAllText(\"test.coverage.tmp\", \"data\");";
        var statement = SyntaxFactory.ParseStatement(statementText) as ExpressionStatementSyntax;
        Assert.NotNull(statement);

        var rewriter = new CoverageCleanupRewriter();
        var result = rewriter.Visit(statement!);

        Assert.Null(result);
    }

    [Fact]
    public void VisitExpressionStatement_PreservesNonCoverageStatement()
    {
        const string statementText = "Console.WriteLine(\"Hello\");";
        var statement = SyntaxFactory.ParseStatement(statementText) as ExpressionStatementSyntax;
        Assert.NotNull(statement);

        var rewriter = new CoverageCleanupRewriter();
        var result = rewriter.Visit(statement!);

        Assert.NotNull(result);
        Assert.Equal(statementText, result!.ToString());
    }
}
