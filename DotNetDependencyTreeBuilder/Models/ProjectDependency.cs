namespace DotNetDependencyTreeBuilder.Models;

/// <summary>
/// Represents a dependency relationship between projects
/// </summary>
public class ProjectDependency
{
    /// <summary>
    /// Path to the referenced project file
    /// </summary>
    public string ReferencedProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the referenced project
    /// </summary>
    public string ReferencedProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the dependency has been resolved to an actual project
    /// </summary>
    public bool IsResolved { get; set; }
}