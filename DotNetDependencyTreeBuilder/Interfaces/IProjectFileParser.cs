using DotNetDependencyTreeBuilder.Models;

namespace DotNetDependencyTreeBuilder.Interfaces;

/// <summary>
/// Interface for parsing project files to extract dependency information
/// </summary>
public interface IProjectFileParser
{
    /// <summary>
    /// Parses a project file to extract project information and dependencies
    /// </summary>
    /// <param name="projectFilePath">Path to the project file</param>
    /// <returns>Parsed project information</returns>
    Task<ProjectInfo> ParseProjectFileAsync(string projectFilePath);

    /// <summary>
    /// Determines if this parser can handle the specified file type
    /// </summary>
    /// <param name="filePath">Path to the project file</param>
    /// <returns>True if this parser can handle the file, false otherwise</returns>
    bool CanParse(string filePath);
}