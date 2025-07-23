namespace DotNetDependencyTreeBuilder.Interfaces;

/// <summary>
/// Main orchestration service that coordinates all operations
/// </summary>
public interface IDependencyTreeService
{
    /// <summary>
    /// Analyzes dependencies in the specified directory and generates build order
    /// </summary>
    /// <param name="sourceDirectory">Root directory to analyze</param>
    /// <param name="outputPath">Optional output file path</param>
    /// <param name="verbose">Enable verbose logging</param>
    /// <returns>Exit code (0 for success, non-zero for errors)</returns>
    Task<int> AnalyzeDependenciesAsync(string sourceDirectory, string? outputPath = null, bool verbose = false);
}