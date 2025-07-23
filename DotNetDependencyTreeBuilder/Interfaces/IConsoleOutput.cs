using DotNetDependencyTreeBuilder.Models;

namespace DotNetDependencyTreeBuilder.Interfaces;

/// <summary>
/// Interface for console and file output operations
/// </summary>
public interface IConsoleOutput
{
    /// <summary>
    /// Outputs the build order to console or file
    /// </summary>
    /// <param name="buildOrder">The build order to output</param>
    /// <param name="outputPath">Optional file path for output</param>
    /// <returns>Task representing the async operation</returns>
    Task OutputBuildOrderAsync(BuildOrder buildOrder, string? outputPath = null);

    /// <summary>
    /// Outputs error messages
    /// </summary>
    /// <param name="message">The error message to output</param>
    void OutputError(string message);

    /// <summary>
    /// Outputs informational messages
    /// </summary>
    /// <param name="message">The information message to output</param>
    void OutputInfo(string message);
}