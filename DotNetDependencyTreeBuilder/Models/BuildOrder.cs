namespace DotNetDependencyTreeBuilder.Models;

/// <summary>
/// Represents the final build order with grouping information
/// </summary>
public class BuildOrder
{
    /// <summary>
    /// Projects grouped by dependency levels (level 1 can be built first, etc.)
    /// </summary>
    public List<List<ProjectInfo>> BuildLevels { get; set; } = new();

    /// <summary>
    /// List of projects involved in circular dependencies
    /// </summary>
    public List<string> CircularDependencies { get; set; } = new();

    /// <summary>
    /// Indicates whether circular dependencies were detected
    /// </summary>
    public bool HasCircularDependencies => CircularDependencies.Any();

    /// <summary>
    /// Total number of projects in the build order
    /// </summary>
    public int TotalProjects => BuildLevels.SelectMany(level => level).Count();

    /// <summary>
    /// Total number of dependency levels
    /// </summary>
    public int TotalLevels => BuildLevels.Count;
}