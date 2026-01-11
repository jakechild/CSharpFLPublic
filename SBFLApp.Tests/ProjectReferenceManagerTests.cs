using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace SBFLApp.Tests
{
    public class ProjectReferenceManagerTests
    {
        [Fact]
        public void EnsureCoverageLoggerReference_AddsReferenceWhenMissing()
        {
            using var tempDirectory = new TempDirectory();
            string projectPath = Path.Combine(tempDirectory.Path, "Sample.csproj");
            string sbflProjectPath = Path.Combine(tempDirectory.Path, "SBFLApp.csproj");

            File.WriteAllText(projectPath, "<Project></Project>");
            File.WriteAllText(sbflProjectPath, "<Project></Project>");

            ProjectReferenceManager.EnsureCoverageLoggerReference(projectPath, sbflProjectPath);

            var document = XDocument.Load(projectPath);
            var reference = document.Descendants("ProjectReference").Single();

            Assert.Equal("SBFLApp.csproj", Path.GetFileName(reference.Attribute("Include")!.Value));
        }

        [Fact]
        public void EnsureCoverageLoggerReference_DoesNotDuplicateExistingReference()
        {
            using var tempDirectory = new TempDirectory();
            string projectPath = Path.Combine(tempDirectory.Path, "Sample.csproj");
            string sbflProjectPath = Path.Combine(tempDirectory.Path, "SBFLApp.csproj");
            string existingReference = "..\\SBFLApp\\SBFLApp.csproj";

            File.WriteAllText(projectPath, $"<Project><ItemGroup><ProjectReference Include=\"{existingReference}\" /></ItemGroup></Project>");
            File.WriteAllText(sbflProjectPath, "<Project></Project>");

            ProjectReferenceManager.EnsureCoverageLoggerReference(projectPath, sbflProjectPath);

            var document = XDocument.Load(projectPath);
            var references = document.Descendants("ProjectReference").ToList();

            Assert.Single(references);
            Assert.EndsWith("SBFLApp.csproj", references[0].Attribute("Include")!.Value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
