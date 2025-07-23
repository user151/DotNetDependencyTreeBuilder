using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotNetDependencyTreeBuilder.Services;

/// <summary>
/// Service for discovering project files in directory structures
/// </summary>
public class ProjectDiscoveryService : IProjectDiscoveryService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<ProjectDiscoveryService> _logger;

    public ProjectDiscoveryService(IFileSystemService fileSystemService, ILogger<ProjectDiscoveryService> logger)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProjectInfo>> DiscoverProjectsAsync(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            _logger.LogError("Root directory cannot be null or empty");
            throw new ArgumentException("Root directory cannot be null or empty", nameof(rootDirectory));
        }

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting project discovery in directory: {RootDirectory}", rootDirectory);

        try
        {
            if (!_fileSystemService.DirectoryExists(rootDirectory))
            {
                _logger.LogError("Root directory does not exist: {RootDirectory}", rootDirectory);
                throw new ProjectDiscoveryException(rootDirectory, $"Directory not found: {rootDirectory}");
            }

            var projects = new List<ProjectInfo>();
            var discoveryStats = new DiscoveryStatistics();
            
            await DiscoverProjectsRecursiveAsync(rootDirectory, projects, discoveryStats);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Project discovery completed. Found {ProjectCount} projects in {Duration:F2}ms. " +
                "Directories scanned: {DirectoriesScanned}, Directories skipped: {DirectoriesSkipped}, Errors: {ErrorCount}",
                projects.Count, duration.TotalMilliseconds, discoveryStats.DirectoriesScanned, 
                discoveryStats.DirectoriesSkipped, discoveryStats.ErrorCount);

            if (discoveryStats.ErrorCount > 0)
            {
                _logger.LogWarning("Project discovery completed with {ErrorCount} errors. Some projects may have been missed.", 
                    discoveryStats.ErrorCount);
            }

            return projects;
        }
        catch (Exception ex) when (!(ex is ProjectDiscoveryException || ex is ArgumentException))
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Critical error during project discovery in {RootDirectory} after {Duration:F2}ms", 
                rootDirectory, duration.TotalMilliseconds);
            throw new ProjectDiscoveryException(rootDirectory, $"Failed to discover projects: {ex.Message}", ex);
        }
    }

    private async Task DiscoverProjectsRecursiveAsync(string currentDirectory, List<ProjectInfo> projects, DiscoveryStatistics stats)
    {
        try
        {
            _logger.LogDebug("Scanning directory: {CurrentDirectory}", currentDirectory);
            stats.DirectoriesScanned++;

            // Find C# project files
            string[] csharpProjects;
            try
            {
                csharpProjects = _fileSystemService.GetFiles(currentDirectory, "*.csproj");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get C# project files from directory: {CurrentDirectory}", currentDirectory);
                csharpProjects = Array.Empty<string>();
                stats.ErrorCount++;
            }

            foreach (var projectFile in csharpProjects)
            {
                try
                {
                    var projectInfo = CreateProjectInfo(projectFile, ProjectType.CSharp);
                    projects.Add(projectInfo);
                    stats.CSharpProjectsFound++;
                    _logger.LogDebug("Found C# project: {ProjectFile}", projectFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create project info for C# project: {ProjectFile}", projectFile);
                    stats.ErrorCount++;
                }
            }

            // Find VB.NET project files
            string[] vbProjects;
            try
            {
                vbProjects = _fileSystemService.GetFiles(currentDirectory, "*.vbproj");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get VB.NET project files from directory: {CurrentDirectory}", currentDirectory);
                vbProjects = Array.Empty<string>();
                stats.ErrorCount++;
            }

            foreach (var projectFile in vbProjects)
            {
                try
                {
                    var projectInfo = CreateProjectInfo(projectFile, ProjectType.VisualBasic);
                    projects.Add(projectInfo);
                    stats.VBProjectsFound++;
                    _logger.LogDebug("Found VB.NET project: {ProjectFile}", projectFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create project info for VB.NET project: {ProjectFile}", projectFile);
                    stats.ErrorCount++;
                }
            }

            // Recursively search subdirectories
            string[] subdirectories;
            try
            {
                subdirectories = _fileSystemService.GetDirectories(currentDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get subdirectories from directory: {CurrentDirectory}", currentDirectory);
                subdirectories = Array.Empty<string>();
                stats.ErrorCount++;
            }

            foreach (var subdirectory in subdirectories)
            {
                await DiscoverProjectsRecursiveAsync(subdirectory, projects, stats);
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Access denied to directory: {CurrentDirectory}", currentDirectory);
            stats.DirectoriesSkipped++;
            stats.ErrorCount++;
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogWarning("Directory not found (may have been deleted): {CurrentDirectory}", currentDirectory);
            stats.DirectoriesSkipped++;
            stats.ErrorCount++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error scanning directory: {CurrentDirectory}", currentDirectory);
            stats.DirectoriesSkipped++;
            stats.ErrorCount++;
        }
    }

    private static ProjectInfo CreateProjectInfo(string projectFilePath, ProjectType projectType)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
        
        return new ProjectInfo
        {
            FilePath = Path.GetFullPath(projectFilePath),
            ProjectName = projectName,
            Type = projectType,
            ProjectReferences = new List<ProjectDependency>(),
            PackageReferences = new List<PackageReference>(),
            TargetFramework = string.Empty // Will be populated by project file parser
        };
    }
}