using System.Xml;
using DotNetDependencyTreeBuilder.Exceptions;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;

namespace DotNetDependencyTreeBuilder.Parsers;

/// <summary>
/// Parser for Visual Basic project files (.vbproj)
/// </summary>
public class VBProjectParser : IProjectFileParser
{
    /// <summary>
    /// Determines if this parser can handle the specified file type
    /// </summary>
    /// <param name="filePath">Path to the project file</param>
    /// <returns>True if this parser can handle .vbproj files</returns>
    public bool CanParse(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".vbproj", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses a Visual Basic project file to extract project information and dependencies
    /// </summary>
    /// <param name="projectFilePath">Path to the .vbproj file</param>
    /// <returns>Parsed project information</returns>
    /// <exception cref="ArgumentException">Thrown when file path is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when project file doesn't exist</exception>
    /// <exception cref="ProjectParsingException">Thrown when project file cannot be parsed</exception>
    public async Task<ProjectInfo> ParseProjectFileAsync(string projectFilePath)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath))
            throw new ArgumentException("Project file path cannot be null or empty", nameof(projectFilePath));

        if (!File.Exists(projectFilePath))
            throw new FileNotFoundException($"Project file not found: {projectFilePath}");

        try
        {
            var projectInfo = new ProjectInfo
            {
                FilePath = Path.GetFullPath(projectFilePath),
                ProjectName = Path.GetFileNameWithoutExtension(projectFilePath),
                Type = ProjectType.VisualBasic
            };

            var xmlDoc = new XmlDocument();
            var content = await File.ReadAllTextAsync(projectFilePath);
            xmlDoc.LoadXml(content);

            // Create namespace manager for handling XML namespaces
            var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceManager.AddNamespace("ms", "http://schemas.microsoft.com/developer/msbuild/2003");

            // Extract target framework - try both with and without namespace
            var targetFrameworkNode = xmlDoc.SelectSingleNode("//TargetFramework") ?? 
                                    xmlDoc.SelectSingleNode("//TargetFrameworks") ??
                                    xmlDoc.SelectSingleNode("//ms:TargetFramework", namespaceManager) ??
                                    xmlDoc.SelectSingleNode("//ms:TargetFrameworks", namespaceManager);
            if (targetFrameworkNode != null)
            {
                projectInfo.TargetFramework = targetFrameworkNode.InnerText.Trim();
            }

            // Extract project references - try both with and without namespace
            var projectReferenceNodes = xmlDoc.SelectNodes("//ProjectReference") ??
                                      xmlDoc.SelectNodes("//ms:ProjectReference", namespaceManager);
            if (projectReferenceNodes != null)
            {
                foreach (XmlNode node in projectReferenceNodes)
                {
                    var includeAttribute = node.Attributes?["Include"];
                    if (includeAttribute != null)
                    {
                        var referencePath = includeAttribute.Value;
                        var dependency = new ProjectDependency
                        {
                            ReferencedProjectPath = referencePath,
                            ReferencedProjectName = Path.GetFileNameWithoutExtension(referencePath),
                            IsResolved = false
                        };
                        projectInfo.ProjectReferences.Add(dependency);
                    }
                }
            }

            // Extract package references - try both with and without namespace
            var packageReferenceNodes = xmlDoc.SelectNodes("//PackageReference") ??
                                      xmlDoc.SelectNodes("//ms:PackageReference", namespaceManager);
            if (packageReferenceNodes != null)
            {
                foreach (XmlNode node in packageReferenceNodes)
                {
                    var includeAttribute = node.Attributes?["Include"];
                    var versionAttribute = node.Attributes?["Version"];
                    
                    if (includeAttribute != null)
                    {
                        var packageRef = new PackageReference
                        {
                            PackageName = includeAttribute.Value,
                            Version = versionAttribute?.Value ?? string.Empty
                        };
                        projectInfo.PackageReferences.Add(packageRef);
                    }
                }
            }

            return projectInfo;
        }
        catch (XmlException ex)
        {
            throw new ProjectParsingException(projectFilePath, $"Failed to parse XML in project file: {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException || ex is ProjectParsingException))
        {
            throw new ProjectParsingException(projectFilePath, $"Unexpected error parsing project file: {ex.Message}", ex);
        }
    }
}