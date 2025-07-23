namespace DotNetDependencyTreeBuilder.Interfaces;

/// <summary>
/// Abstraction for file system operations to enable testability
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Gets all directories in the specified path
    /// </summary>
    /// <param name="path">The directory path to search</param>
    /// <returns>Array of directory paths</returns>
    string[] GetDirectories(string path);

    /// <summary>
    /// Gets all files in the specified path with the given search pattern
    /// </summary>
    /// <param name="path">The directory path to search</param>
    /// <param name="searchPattern">The search pattern for files</param>
    /// <returns>Array of file paths</returns>
    string[] GetFiles(string path, string searchPattern);

    /// <summary>
    /// Checks if a directory exists
    /// </summary>
    /// <param name="path">The directory path to check</param>
    /// <returns>True if directory exists, false otherwise</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="path">The file path to check</param>
    /// <returns>True if file exists, false otherwise</returns>
    bool FileExists(string path);

    /// <summary>
    /// Reads all text from a file
    /// </summary>
    /// <param name="path">The file path to read</param>
    /// <returns>The file content as string</returns>
    Task<string> ReadAllTextAsync(string path);
}