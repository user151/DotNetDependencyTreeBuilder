using DotNetDependencyTreeBuilder.Models;

namespace DotNetDependencyTreeBuilder.Interfaces;

/// <summary>
/// Service for analyzing project dependencies and building dependency graphs
/// </summary>
public interface IDependencyAnalysisService
{
    /// <summary>
    /// Analyzes project dependencies and builds a dependency graph
    /// </summary>
    /// <param name="projects">Collection of projects to analyze</param>
    /// <returns>Dependency graph representing project relationships</returns>
    Task<DependencyGraph> AnalyzeDependenciesAsync(IEnumerable<ProjectInfo> projects);

    /// <summary>
    /// Generates build order from dependency graph
    /// </summary>
    /// <param name="dependencyGraph">The dependency graph to analyze</param>
    /// <returns>Build order with project groupings</returns>
    BuildOrder GenerateBuildOrder(DependencyGraph dependencyGraph);
}