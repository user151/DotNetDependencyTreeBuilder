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
         var targetFrameworkNode = GetXmlSingleNode(xmlDoc, "//TargetFramework", namespaceManager, "ms");
         if (targetFrameworkNode == null || targetFrameworkNode.InnerText.Trim() == string.Empty)
         {
            // If no single TargetFramework node found, try TargetFrameworks
            targetFrameworkNode = GetXmlSingleNode(xmlDoc, "//TargetFrameworks", namespaceManager, "ms");
         }
         if (targetFrameworkNode != null)
         {
            projectInfo.TargetFramework = targetFrameworkNode.InnerText.Trim();
         }

         // Extract project references - try both with and without namespace
         var projectReferenceNodes = GetXmlNodeList(xmlDoc, "//ProjectReference", namespaceManager, "ms");
         if (projectReferenceNodes != null && projectReferenceNodes.Count > 0)
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
         var packageReferenceNodes = GetXmlNodeList(xmlDoc, "//PackageReference", namespaceManager, "ms");
         if (packageReferenceNodes != null && packageReferenceNodes.Count > 0)
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

         // Extract assembly references - try both with and without namespace
         var assemblyReferenceNodes = GetXmlNodeList(xmlDoc, "//Reference", namespaceManager, "ms");
         if (assemblyReferenceNodes != null && assemblyReferenceNodes.Count > 0)
         {
                foreach (XmlNode node in assemblyReferenceNodes)
                {
                    var includeAttribute = node.Attributes?["Include"];
               if (includeAttribute != null)
                  {

                     // Extract assembly name (part before the comma)
                     var assemblyName = ExtractAssemblyName(includeAttribute.Value);

                     var dependency = new ProjectDependency
                     {
                        ReferencedProjectPath = assemblyName, // Will be resolved later
                        ReferencedProjectName = assemblyName,
                        IsResolved = false
                     };
                     projectInfo.ProjectReferences.Add(dependency);

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

   private static XmlNodeList? GetXmlNodeList(XmlDocument xmlDoc, string xpath, XmlNamespaceManager namespaceManager, string prefix)
   {
      
      if (xmlDoc == null || string.IsNullOrWhiteSpace(xpath))
         return null;
      var nodes = xmlDoc.SelectNodes(xpath);
      if (nodes == null || nodes.Count == 0)
      {
         var nsPrefix = namespaceManager.LookupPrefix(namespaceManager.DefaultNamespace);
         xpath = xpath.Replace("//", $"//{prefix}:");
         return xmlDoc.SelectNodes(xpath, namespaceManager); 
      }
      return nodes;
   }
   private static XmlNode? GetXmlSingleNode(XmlDocument xmlDoc, string xpath, XmlNamespaceManager namespaceManager, string prefix)
   {      
      if (xmlDoc == null || string.IsNullOrWhiteSpace(xpath))
         return null;
      
      var node = xmlDoc.SelectSingleNode(xpath); 
      if (node == null)
      {         
         xpath = xpath.Replace("//", $"//{prefix}:");
         return xmlDoc.SelectSingleNode(xpath, namespaceManager);
      }
      return node;
   }

   /// <summary>
   /// Determines if a Reference node could potentially point to a project based on assembly name
   /// </summary>
   /// <param name="includeValue">The Include attribute value from a Reference node</param>
   /// <param name="hintPath">The HintPath value (if any)</param>
   /// <returns>True if the reference could be a project reference</returns>
   private static bool CouldBeProjectReferenceByName(string includeValue, string hintPath)
    {
        if (string.IsNullOrWhiteSpace(includeValue))
            return false;

        // Extract assembly name (part before the comma)
        var assemblyName = ExtractAssemblyName(includeValue);
        
        // Skip obvious system/framework assemblies
        var systemPrefixes = new[] { "Microsoft", "System", "mscorlib", "netstandard", "Windows" };
        if (systemPrefixes.Any(prefix => assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return false;

        // If there's a HintPath, use the existing logic for path-based detection
        if (!string.IsNullOrEmpty(hintPath))
        {
            return CouldBeProjectReferenceByPath(hintPath);
        }

        // For references without HintPath, assume they could be project references
        // unless they look like system assemblies
        return true;
    }

    /// <summary>
    /// Determines if a HintPath could potentially point to a project output
    /// </summary>
    /// <param name="hintPath">The HintPath value from a Reference node</param>
    /// <returns>True if the path could be a project reference</returns>
    private static bool CouldBeProjectReferenceByPath(string hintPath)
    {
        if (string.IsNullOrWhiteSpace(hintPath))
            return false;

        // Check if it's a DLL file
        if (!hintPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return false;

        // Check if it's an absolute path to system directories - these are definitely not project references
        var systemPaths = new[] { "windows", "system32", "program files", "gac", "microsoft.net", "dotnet" };
        var lowerPath = hintPath.ToLowerInvariant();
        if (systemPaths.Any(sysPath => lowerPath.Contains(sysPath)))
            return false;

        // Check if it's an absolute path starting with C:\ or similar - likely external
        if (Path.IsPathRooted(hintPath) && !hintPath.StartsWith(".."))
        {
            // If it's a rooted path but contains bin/obj, it might still be a project reference
            if (hintPath.Contains("bin", StringComparison.OrdinalIgnoreCase) || 
                hintPath.Contains("obj", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false; // Other rooted paths are likely external assemblies
        }

        // Check if it contains relative path indicators and bin/obj (suggesting it's a project output)
        // But be more specific - it should look like a project structure (e.g., ../ProjectName/bin/Debug/ProjectName.dll)
        if (hintPath.Contains("bin", StringComparison.OrdinalIgnoreCase) || 
            hintPath.Contains("obj", StringComparison.OrdinalIgnoreCase))
        {
            // Additional check: the path should contain at least 3 segments to be a project reference
            // e.g., ../ProjectName/bin/Debug/ProjectName.dll or ProjectName/bin/Debug/ProjectName.dll
            var segments = hintPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var dotDotCount = segments.Count(s => s == "..");
            var nonDotDotSegments = segments.Length - dotDotCount;
            
            // Should have at least: ProjectName, bin/obj, configuration, dll
            if (nonDotDotSegments >= 3)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Extracts the assembly name (part before the comma) from the Include attribute
    /// </summary>
    /// <param name="includeValue">The full Include attribute value</param>
    /// <returns>Assembly name without version/culture information</returns>
    private static string ExtractAssemblyName(string includeValue)
    {
        if (string.IsNullOrWhiteSpace(includeValue))
            return string.Empty;

        var commaIndex = includeValue.IndexOf(',');
        return commaIndex > 0 ? includeValue.Substring(0, commaIndex).Trim() : includeValue.Trim();
    }

    /// <summary>
    /// Parses assembly name components from the Include attribute value
    /// </summary>
    /// <param name="assemblyRef">Assembly reference to populate with parsed components</param>
    private static void ParseAssemblyNameComponents(AssemblyReference assemblyRef)
    {
        var parts = assemblyRef.AssemblyName.Split(',');
        
        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            
            if (trimmedPart.StartsWith("Version=", StringComparison.OrdinalIgnoreCase))
            {
                assemblyRef.Version = trimmedPart.Substring(8);
            }
            else if (trimmedPart.StartsWith("Culture=", StringComparison.OrdinalIgnoreCase))
            {
                assemblyRef.Culture = trimmedPart.Substring(8);
            }
            else if (trimmedPart.StartsWith("processorArchitecture=", StringComparison.OrdinalIgnoreCase))
            {
                assemblyRef.ProcessorArchitecture = trimmedPart.Substring(22);
            }
        }
    }
}