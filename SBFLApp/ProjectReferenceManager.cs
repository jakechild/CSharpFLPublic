using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SBFLApp
{
    internal static class ProjectReferenceManager
    {
        private const string ProjectReferenceElementName = "ProjectReference";
        private const string IncludeAttributeName = "Include";

        public static void EnsureCoverageLoggerReference(string projectFilePath, string? coverageLoggerProjectPath = null)
        {
            if (string.IsNullOrWhiteSpace(projectFilePath) || !File.Exists(projectFilePath))
            {
                return;
            }

            coverageLoggerProjectPath ??= Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "SBFLApp", "SBFLApp.csproj"));

            if (!File.Exists(coverageLoggerProjectPath))
            {
                ConsoleLogger.Warning($"Coverage logger project not found at '{coverageLoggerProjectPath}'. Skipping project reference update.");
                return;
            }

            XDocument projectDocument;

            try
            {
                projectDocument = XDocument.Load(projectFilePath);
            }
            catch (Exception ex)
            {
                ConsoleLogger.Warning($"Failed to load project file '{projectFilePath}': {ex.Message}");
                return;
            }

            if (ProjectReferenceExists(projectDocument, projectFilePath, coverageLoggerProjectPath))
            {
                return;
            }

            AddProjectReference(projectDocument, projectFilePath, coverageLoggerProjectPath);
        }

        private static bool ProjectReferenceExists(XDocument projectDocument, string projectFilePath, string targetProjectPath)
        {
            var projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
            var normalizedTarget = Path.GetFullPath(targetProjectPath);

            return projectDocument
                .Descendants(ProjectReferenceElementName)
                .Select(reference => reference.Attribute(IncludeAttributeName)?.Value)
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Any(include =>
                {
                    string includeValue = include!;
                    string includeFileName = GetCrossPlatformFileName(includeValue);

                    if (string.Equals(includeFileName, Path.GetFileName(targetProjectPath), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    string normalizedInclude = NormalizeSeparators(includeValue);
                    string includeFullPath = Path.GetFullPath(Path.Combine(projectDirectory, normalizedInclude));
                    return string.Equals(includeFullPath, normalizedTarget, StringComparison.OrdinalIgnoreCase);
                });
        }

        private static string GetCrossPlatformFileName(string includeValue)
        {
            return Path.GetFileName(NormalizeSeparators(includeValue));
        }

        private static string NormalizeSeparators(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        private static void AddProjectReference(XDocument projectDocument, string projectFilePath, string targetProjectPath)
        {
            string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
            string relativePath = Path.GetRelativePath(projectDirectory, targetProjectPath);

            var referenceElement = new XElement(
                ProjectReferenceElementName,
                new XAttribute(IncludeAttributeName, relativePath));

            var itemGroup = projectDocument
                .Root?
                .Elements("ItemGroup")
                .FirstOrDefault(group => group.Elements(ProjectReferenceElementName).Any());

            if (itemGroup == null)
            {
                itemGroup = new XElement("ItemGroup");
                projectDocument.Root?.Add(itemGroup);
            }

            itemGroup.Add(referenceElement);
            projectDocument.Save(projectFilePath);

            ConsoleLogger.Info($"Added SBFLApp project reference to '{projectFilePath}'.");
        }
    }
}
