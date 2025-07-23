using DotNetDependencyTreeBuilder.Models;

namespace DotNetDependencyTreeBuilder.Interfaces;

/// <summary>
/// Service for discovering project files in directory structures
/// </summary>
public interface IProjectDiscoveryService
{
    /// <summary>
    /// Discovers all project files in the specified directory and its subdirectories
    /// </summary>
    /// <param name="rootDirectory">The root directory to start the search</param>
    /// <returns>Collection of discovered project information</returns>
    Task<IEnumerable<ProjectInfo>> DiscoverProjectsAsync(string rootDirectory);
}