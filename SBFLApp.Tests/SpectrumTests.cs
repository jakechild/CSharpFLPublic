using SBFLApp;

namespace SBFLApp.Tests;

public class SpectrumTests
{
    [Fact]
    public void ResetInstrumentation_RemovesCoverageStatements()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "Sample.cs");
            var originalContent = """
using System;

class Sample
{
    void Method()
    {
        SBFLApp.CoverageLogger.Log("test.coverage.tmp", "data");
        System.IO.File.AppendAllText("test.coverage.tmp", "legacy");
        Console.WriteLine("Hello");
    }
}
""";
            File.WriteAllText(filePath, originalContent);

            Spectrum.ResetInstrumentation(new[] { filePath });

            var updatedContent = File.ReadAllText(filePath);

            Assert.DoesNotContain("AppendAllText", updatedContent, StringComparison.Ordinal);
            Assert.DoesNotContain("CoverageLogger.Log", updatedContent, StringComparison.Ordinal);
            Assert.Contains("Console.WriteLine(\"Hello\");", updatedContent, StringComparison.Ordinal);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
