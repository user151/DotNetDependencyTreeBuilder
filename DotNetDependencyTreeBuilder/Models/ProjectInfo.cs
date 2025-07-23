namespace DotNetDependencyTreeBuilder.Models;

/// <summary>
/// Represents a discovered project with metadata and dependencies
/// </summary>
public class ProjectInfo
{
    /// <summary>
    /// Full path to the project file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the project (typically the filename without extension)
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the project (C# or VB.NET)
    /// </summary>
    public ProjectType Type { get; set; }

    /// <summary>
    /// List of project references (dependencies on other projects)
    /// </summary>
    public List<ProjectDependency> ProjectReferences { get; set; } = new();

    /// <summary>
    /// List of NuGet package references
    /// </summary>
    public List<PackageReference> PackageReferences { get; set; } = new();

    /// <summary>
    /// List of direct assembly references
    /// </summary>
    public List<AssemblyReference> AssemblyReferences { get; set; } = new();

    /// <summary>
    /// Target framework of the project
    /// </summary>
    public string TargetFramework { get; set; } = string.Empty;
}