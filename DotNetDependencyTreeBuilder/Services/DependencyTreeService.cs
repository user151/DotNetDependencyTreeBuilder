using DotNetDependencyTreeBuilder.Exceptions;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Output;
using Microsoft.Extensions.Logging;

namespace DotNetDependencyTreeBuilder.Services;

/// <summary>
/// Main orchestration service that coordinates all operations
/// </summary>
public class DependencyTreeService : IDependencyTreeService
{
    private readonly IProjectDiscoveryService _projectDiscoveryService;
    private readonly IDependencyAnalysisService _dependencyAnalysisService;
    private readonly ILogger<DependencyTreeService> _logger;

    /// <summary>
    /// Initializes a new instance of the DependencyTreeService class
    /// </summary>
    /// <param name="projectDiscoveryService">Service for discovering projects</param>
    /// <param name="dependencyAnalysisService">Service for analyzing dependencies</param>
    /// <param name="logger">Logger for recording operations</param>
    public DependencyTreeService(
        IProjectDiscoveryService projectDiscoveryService,
        IDependencyAnalysisService dependencyAnalysisService,
        ILogger<DependencyTreeService> logger)
    {
        _projectDiscoveryService = projectDiscoveryService ?? throw new ArgumentNullException(nameof(projectDiscoveryService));
        _dependencyAnalysisService = dependencyAnalysisService ?? throw new ArgumentNullException(nameof(dependencyAnalysisService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes dependencies in the specified directory and generates build order
    /// </summary>
    /// <param name="sourceDirectory">Root directory to analyze</param>
    /// <param name="outputPath">Optional output file path</param>
    /// <param name="verbose">Enable verbose logging</param>
    /// <returns>Exit code (0 for success, non-zero for errors)</returns>
    public async Task<int> AnalyzeDependenciesAsync(string sourceDirectory, string? outputPath = null, bool verbose = false)
    {
        try
        {
            _logger.LogInformation("Starting dependency analysis for directory: {SourceDirectory}", sourceDirectory);

            // Validate input directory
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                _logger.LogError("Source directory cannot be null or empty");
                return 1;
            }

            if (!Directory.Exists(sourceDirectory))
            {
                _logger.LogError("Source directory does not exist: {SourceDirectory}", sourceDirectory);
                return 1;
            }

            // Step 1: Discover projects
            _logger.LogInformation("Discovering projects in directory structure...");
            var projects = await DiscoverProjectsWithErrorHandling(sourceDirectory);
            
            if (!projects.Any())
            {
                _logger.LogWarning("No projects found in directory: {SourceDirectory}", sourceDirectory);
                var output = ConsoleOutputFactory.Create(OutputFormat.Text, outputPath);
                output.OutputInfo("No projects found in the specified directory.");
                return 0;
            }

            _logger.LogInformation("Found {ProjectCount} projects", projects.Count());

            // Step 2: Analyze dependencies
            _logger.LogInformation("Analyzing project dependencies...");
            var dependencyGraph = await AnalyzeDependenciesWithErrorHandling(projects);

            // Step 3: Generate build order
            _logger.LogInformation("Generating build order...");
            var buildOrder = _dependencyAnalysisService.GenerateBuildOrder(dependencyGraph);

            // Step 4: Output results
            await OutputResultsWithSummary(buildOrder, outputPath, projects.Count(), dependencyGraph, verbose);

            // Step 5: Determine exit code based on results
            var exitCode = DetermineExitCode(buildOrder);
            
            _logger.LogInformation("Dependency analysis completed with exit code: {ExitCode}", exitCode);
            return exitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during dependency analysis");
            
            var output = ConsoleOutputFactory.Create(OutputFormat.Text, outputPath);
            output.OutputError($"Critical error: {ex.Message}");
            
            return 2; // Critical error exit code
        }
    }

    /// <summary>
    /// Discovers projects with comprehensive error handling
    /// </summary>
    private async Task<IEnumerable<ProjectInfo>> DiscoverProjectsWithErrorHandling(string sourceDirectory)
    {
        try
        {
            return await _projectDiscoveryService.DiscoverProjectsAsync(sourceDirectory);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while discovering projects in: {SourceDirectory}", sourceDirectory);
            throw new ProjectAnalysisException(sourceDirectory, $"Access denied to directory: {sourceDirectory}", ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Directory not found: {SourceDirectory}", sourceDirectory);
            throw new ProjectAnalysisException(sourceDirectory, $"Directory not found: {sourceDirectory}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during project discovery in: {SourceDirectory}", sourceDirectory);
            throw new ProjectAnalysisException(sourceDirectory, $"Failed to discover projects: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Analyzes dependencies with comprehensive error handling
    /// </summary>
    private async Task<DependencyGraph> AnalyzeDependenciesWithErrorHandling(IEnumerable<ProjectInfo> projects)
    {
        try
        {
            return await _dependencyAnalysisService.AnalyzeDependenciesAsync(projects);
        }
        catch (ProjectParsingException ex)
        {
            _logger.LogError(ex, "Failed to parse project file: {ProjectPath}", ex.ProjectPath);
            // Continue with other projects, but log the error
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during dependency analysis");
            throw new ProjectAnalysisException("", $"Failed to analyze dependencies: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Outputs results with comprehensive summary statistics
    /// </summary>
    private async Task OutputResultsWithSummary(BuildOrder buildOrder, string? outputPath, int projectCount, DependencyGraph dependencyGraph, bool verbose)
    {
        var output = ConsoleOutputFactory.Create(OutputFormat.Text, outputPath);

        // Output summary statistics
        var totalDependencies = CalculateTotalDependencies(dependencyGraph);
        
        _logger.LogInformation("Analysis Summary - Projects: {ProjectCount}, Dependencies: {DependencyCount}, Levels: {LevelCount}", 
            projectCount, totalDependencies, buildOrder.TotalLevels);

        if (verbose)
        {
            output.OutputInfo("=== Dependency Analysis Summary ===");
            output.OutputInfo($"Projects Found: {projectCount}");
            output.OutputInfo($"Dependencies Analyzed: {totalDependencies}");
            output.OutputInfo($"Build Levels: {buildOrder.TotalLevels}");
            
            if (buildOrder.HasCircularDependencies)
            {
                output.OutputInfo($"Circular Dependencies: {buildOrder.CircularDependencies.Count}");
            }
            else
            {
                output.OutputInfo("Circular Dependencies: None");
            }
            
            output.OutputInfo("=====================================");
            output.OutputInfo("");
        }

        // Output build order
        await output.OutputBuildOrderAsync(buildOrder, outputPath);

        // Log detailed statistics
        if (verbose)
        {
            LogDetailedStatistics(buildOrder, dependencyGraph);
        }
    }

    /// <summary>
    /// Calculates total number of dependencies in the graph
    /// </summary>
    private int CalculateTotalDependencies(DependencyGraph dependencyGraph)
    {
        return dependencyGraph.AdjacencyList.Values.Sum(dependencies => dependencies.Count);
    }

    /// <summary>
    /// Logs detailed statistics about the analysis
    /// </summary>
    private void LogDetailedStatistics(BuildOrder buildOrder, DependencyGraph dependencyGraph)
    {
        _logger.LogInformation("=== Detailed Analysis Statistics ===");
        _logger.LogInformation("Total Projects: {TotalProjects}", buildOrder.TotalProjects);
        _logger.LogInformation("Build Levels: {TotalLevels}", buildOrder.TotalLevels);
        
        for (int i = 0; i < buildOrder.BuildLevels.Count; i++)
        {
            _logger.LogInformation("Level {Level}: {ProjectCount} projects", i + 1, buildOrder.BuildLevels[i].Count);
        }

        if (buildOrder.HasCircularDependencies)
        {
            _logger.LogWarning("Circular Dependencies Detected:");
            foreach (var project in buildOrder.CircularDependencies)
            {
                _logger.LogWarning("  - {ProjectPath}", project);
            }
        }

        // Log projects with most dependencies
        var projectDependencyCounts = dependencyGraph.AdjacencyList
            .Where(kvp => kvp.Value.Any())
            .OrderByDescending(kvp => kvp.Value.Count)
            .Take(5);

        _logger.LogInformation("Projects with Most Dependencies:");
        foreach (var kvp in projectDependencyCounts)
        {
            _logger.LogInformation("  - {ProjectPath}: {DependencyCount} dependencies", 
                Path.GetFileName(kvp.Key), kvp.Value.Count);
        }

        _logger.LogInformation("====================================");
    }

    /// <summary>
    /// Determines the appropriate exit code based on analysis results
    /// </summary>
    private int DetermineExitCode(BuildOrder buildOrder)
    {
        if (buildOrder.HasCircularDependencies)
        {
            _logger.LogWarning("Circular dependencies detected - returning warning exit code");
            return 1; // Warning exit code for circular dependencies
        }

        if (buildOrder.TotalProjects == 0)
        {
            _logger.LogInformation("No projects found - returning success exit code");
            return 0; // Success - no projects is not an error
        }

        _logger.LogInformation("Analysis completed successfully");
        return 0; // Success
    }
}