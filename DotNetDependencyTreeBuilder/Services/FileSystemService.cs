using DotNetDependencyTreeBuilder.Interfaces;
using Microsoft.Extensions.Logging;

namespace DotNetDependencyTreeBuilder.Services;

/// <summary>
/// Implementation of file system operations with comprehensive error handling
/// </summary>
public class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;

    public FileSystemService(ILogger<FileSystemService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string[] GetDirectories(string path)
    {
        try
        {
            _logger.LogDebug("Getting directories from path: {Path}", path);
            var directories = Directory.GetDirectories(path);
            _logger.LogDebug("Found {DirectoryCount} directories in {Path}", directories.Length, path);
            return directories;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied when getting directories from path: {Path}", path);
            throw;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Directory not found: {Path}", path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting directories from path: {Path}", path);
            throw;
        }
    }

    /// <inheritdoc />
    public string[] GetFiles(string path, string searchPattern)
    {
        try
        {
            _logger.LogDebug("Getting files from path: {Path} with pattern: {SearchPattern}", path, searchPattern);
            var files = Directory.GetFiles(path, searchPattern);
            _logger.LogDebug("Found {FileCount} files in {Path} matching pattern {SearchPattern}", 
                files.Length, path, searchPattern);
            return files;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied when getting files from path: {Path} with pattern: {SearchPattern}", 
                path, searchPattern);
            throw;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Directory not found when getting files: {Path}", path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting files from path: {Path} with pattern: {SearchPattern}", 
                path, searchPattern);
            throw;
        }
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        try
        {
            var exists = Directory.Exists(path);
            _logger.LogDebug("Directory exists check for {Path}: {Exists}", path, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if directory exists: {Path}", path);
            return false;
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        try
        {
            var exists = File.Exists(path);
            _logger.LogDebug("File exists check for {Path}: {Exists}", path, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if file exists: {Path}", path);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> ReadAllTextAsync(string path)
    {
        try
        {
            _logger.LogDebug("Reading all text from file: {Path}", path);
            var content = await File.ReadAllTextAsync(path);
            _logger.LogDebug("Successfully read {ContentLength} characters from file: {Path}", 
                content.Length, path);
            return content;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found when reading: {Path}", path);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when reading file: {Path}", path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error reading file: {Path}", path);
            throw;
        }
    }
}