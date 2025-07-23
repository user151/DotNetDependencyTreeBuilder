using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotNetDependencyTreeBuilder.Services;

/// <summary>
/// Service for analyzing project dependencies and building dependency graphs
/// </summary>
public class DependencyAnalysisService : IDependencyAnalysisService
{
    private readonly ILogger<DependencyAnalysisService> _logger;
    private readonly IEnumerable<IProjectFileParser> _projectFileParsers;

    /// <summary>
    /// Initializes a new instance of the DependencyAnalysisService class
    /// </summary>
    /// <param name="projectFileParsers">Collection of project file parsers</param>
    /// <param name="logger">Logger for dependency analysis operations</param>
    public DependencyAnalysisService(IEnumerable<IProjectFileParser> projectFileParsers, ILogger<DependencyAnalysisService> logger)
    {
        _projectFileParsers = projectFileParsers ?? throw new ArgumentNullException(nameof(projectFileParsers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Analyzes project dependencies and builds a dependency graph
    /// </summary>
    /// <param name="projects">Collection of projects to analyze</param>
    /// <returns>Dependency graph representing project relationships</returns>
    public async Task<DependencyGraph> AnalyzeDependenciesAsync(IEnumerable<ProjectInfo> projects)
    {
        if (projects == null)
        {
            _logger.LogError("Projects collection cannot be null");
            throw new ArgumentNullException(nameof(projects));
        }

        var projectList = projects.ToList();
        _logger.LogInformation("Starting dependency analysis for {ProjectCount} projects", projectList.Count);

        if (projectList.Count == 0)
        {
            _logger.LogWarning("No projects provided for dependency analysis");
            return new DependencyGraph();
        }

        var dependencyGraph = new DependencyGraph();
        var analysisErrors = new List<string>();

        try
        {
            // Parse project files to extract dependencies
            _logger.LogInformation("Parsing project files to extract dependencies");
            await ParseProjectFilesAsync(projectList);
            
            // Create a lookup dictionary for project resolution
            _logger.LogDebug("Creating project lookup dictionary");
            var projectLookup = CreateProjectLookup(projectList);
            _logger.LogDebug("Created lookup dictionary with {LookupCount} entries", projectLookup.Count);

            // Add all projects to the graph
            _logger.LogInformation("Adding {ProjectCount} projects to dependency graph", projectList.Count);
            var addedProjects = 0;
            foreach (var project in projectList)
            {
                try
                {
                    dependencyGraph.AddProject(project);
                    addedProjects++;
                    
                    if (addedProjects % 10 == 0 || addedProjects == projectList.Count)
                    {
                        _logger.LogDebug("Added {AddedProjects}/{TotalProjects} projects to graph", 
                            addedProjects, projectList.Count);
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Failed to add project '{project.ProjectName}' ({project.FilePath}) to dependency graph: {ex.Message}";
                    _logger.LogError(ex, errorMessage);
                    analysisErrors.Add(errorMessage);
                }
            }

            // Resolve dependencies and build the graph
            _logger.LogInformation("Resolving dependencies for {ProjectCount} projects", projectList.Count);
            var processedProjects = 0;
            var totalDependencies = 0;
            var resolvedDependencies = 0;
            var unresolvedDependencies = 0;

            foreach (var project in projectList)
            {
                try
                {
                    var dependencyCount = project.ProjectReferences.Count;
                    totalDependencies += dependencyCount;

                    if (dependencyCount > 0)
                    {
                        _logger.LogDebug("Resolving {DependencyCount} dependencies for project '{ProjectName}'", 
                            dependencyCount, project.ProjectName);
                    }

                    var (resolved, unresolved) = await ResolveDependenciesAsync(project, projectLookup, dependencyGraph);
                    resolvedDependencies += resolved;
                    unresolvedDependencies += unresolved;

                    processedProjects++;
                    
                    if (processedProjects % 5 == 0 || processedProjects == projectList.Count)
                    {
                        _logger.LogInformation("Processed dependencies for {ProcessedProjects}/{TotalProjects} projects", 
                            processedProjects, projectList.Count);
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Failed to resolve dependencies for project '{project.ProjectName}' ({project.FilePath}): {ex.Message}";
                    _logger.LogError(ex, errorMessage);
                    analysisErrors.Add(errorMessage);
                }
            }

            // Log summary statistics
            _logger.LogInformation("Dependency analysis completed. Projects: {ProjectCount}, " +
                "Total Dependencies: {TotalDependencies}, Resolved: {ResolvedDependencies}, " +
                "Unresolved: {UnresolvedDependencies}, Errors: {ErrorCount}",
                projectList.Count, totalDependencies, resolvedDependencies, 
                unresolvedDependencies, analysisErrors.Count);

            if (unresolvedDependencies > 0)
            {
                _logger.LogWarning("{UnresolvedCount} dependencies could not be resolved", unresolvedDependencies);
            }

            if (analysisErrors.Count > 0)
            {
                _logger.LogWarning("Dependency analysis completed with {ErrorCount} errors", analysisErrors.Count);
                foreach (var error in analysisErrors.Take(5)) // Log first 5 errors
                {
                    _logger.LogWarning("Analysis error: {Error}", error);
                }
                
                if (analysisErrors.Count > 5)
                {
                    _logger.LogWarning("... and {AdditionalErrors} more errors", analysisErrors.Count - 5);
                }
            }

            return dependencyGraph;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during dependency analysis");
            throw new InvalidOperationException("Dependency analysis failed due to critical error", ex);
        }
    }

    /// <summary>
    /// Generates build order from dependency graph
    /// </summary>
    /// <param name="dependencyGraph">The dependency graph to analyze</param>
    /// <returns>Build order with project groupings</returns>
    public BuildOrder GenerateBuildOrder(DependencyGraph dependencyGraph)
    {
        if (dependencyGraph == null)
        {
            _logger.LogError("Dependency graph cannot be null");
            throw new ArgumentNullException(nameof(dependencyGraph));
        }

        _logger.LogInformation("Generating build order from dependency graph with {ProjectCount} projects", 
            dependencyGraph.Projects.Count);

        var buildOrder = new BuildOrder();

        try
        {
            // Detect circular dependencies
            _logger.LogDebug("Detecting circular dependencies");
            var circularDependencies = dependencyGraph.DetectCircularDependencies();
            buildOrder.CircularDependencies = circularDependencies;

            if (circularDependencies.Count > 0)
            {
                _logger.LogWarning("Found {CircularCount} projects involved in circular dependencies", 
                    circularDependencies.Count);
                
                foreach (var circularProject in circularDependencies.Take(5))
                {
                    _logger.LogWarning("Circular dependency detected for project: {ProjectPath}", circularProject);
                }
                
                if (circularDependencies.Count > 5)
                {
                    _logger.LogWarning("... and {AdditionalCircular} more projects with circular dependencies", 
                        circularDependencies.Count - 5);
                }
            }
            else
            {
                _logger.LogDebug("No circular dependencies detected");
            }

            // Get topological order
            _logger.LogDebug("Computing topological order");
            var topologicalOrder = dependencyGraph.GetTopologicalOrder();
            _logger.LogDebug("Computed topological order with {LevelCount} build levels", topologicalOrder.Count);

            // Convert project paths to ProjectInfo objects
            var totalProjectsInOrder = 0;
            for (int levelIndex = 0; levelIndex < topologicalOrder.Count; levelIndex++)
            {
                var level = topologicalOrder[levelIndex];
                var projectLevel = new List<ProjectInfo>();
                
                foreach (var projectPath in level)
                {
                    if (dependencyGraph.Projects.TryGetValue(projectPath, out var project))
                    {
                        projectLevel.Add(project);
                        totalProjectsInOrder++;
                    }
                    else
                    {
                        _logger.LogWarning("Project path '{ProjectPath}' in topological order not found in dependency graph", 
                            projectPath);
                    }
                }

                if (projectLevel.Any())
                {
                    buildOrder.BuildLevels.Add(projectLevel);
                    _logger.LogDebug("Build level {LevelIndex}: {ProjectCount} projects can be built in parallel", 
                        levelIndex + 1, projectLevel.Count);
                }
            }

            _logger.LogInformation("Build order generated successfully. " +
                "Total projects: {TotalProjects}, Build levels: {BuildLevels}, " +
                "Projects in order: {ProjectsInOrder}, Circular dependencies: {CircularCount}",
                dependencyGraph.Projects.Count, buildOrder.BuildLevels.Count, 
                totalProjectsInOrder, circularDependencies.Count);

            return buildOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating build order from dependency graph");
            throw new InvalidOperationException("Failed to generate build order", ex);
        }
    }

    /// <summary>
    /// Creates a lookup dictionary for project resolution
    /// </summary>
    /// <param name="projects">List of discovered projects</param>
    /// <returns>Dictionary mapping various project identifiers to ProjectInfo</returns>
    private Dictionary<string, ProjectInfo> CreateProjectLookup(List<ProjectInfo> projects)
    {
        var lookup = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var project in projects)
        {
            // Add by full path
            lookup[project.FilePath] = project;
            
            // Add by project name
            lookup[project.ProjectName] = project;
            
            // Add by filename
            var fileName = Path.GetFileName(project.FilePath);
            lookup[fileName] = project;
            
            // Add by relative path variations
            var directory = Path.GetDirectoryName(project.FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                // Try different relative path combinations
                var relativePaths = GenerateRelativePaths(project.FilePath, projects);
                foreach (var relativePath in relativePaths)
                {
                    lookup[relativePath] = project;
                }
            }
        }
        
        return lookup;
    }

    /// <summary>
    /// Generates possible relative paths for a project file
    /// </summary>
    /// <param name="projectPath">Full path to the project file</param>
    /// <param name="allProjects">All discovered projects for context</param>
    /// <returns>List of possible relative paths</returns>
    private List<string> GenerateRelativePaths(string projectPath, List<ProjectInfo> allProjects)
    {
        var relativePaths = new List<string>();
        var projectDir = Path.GetDirectoryName(projectPath);
        var fileName = Path.GetFileName(projectPath);
        
        if (string.IsNullOrEmpty(projectDir))
            return relativePaths;
        
        // Find common base directories
        var allDirectories = allProjects
            .Select(p => Path.GetDirectoryName(p.FilePath))
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct()
            .ToList();
        
        foreach (var baseDir in allDirectories)
        {
            if (string.IsNullOrEmpty(baseDir))
                continue;
                
            try
            {
                var relativePath = Path.GetRelativePath(baseDir, projectPath);
                if (!relativePath.StartsWith("..") && relativePath != projectPath)
                {
                    relativePaths.Add(relativePath);
                    relativePaths.Add(relativePath.Replace('\\', '/'));
                    relativePaths.Add(relativePath.Replace('/', '\\'));
                }
            }
            catch
            {
                // Ignore path resolution errors
            }
        }
        
        return relativePaths;
    }

    /// <summary>
    /// Resolves dependencies for a project and updates the dependency graph
    /// </summary>
    /// <param name="project">Project to resolve dependencies for</param>
    /// <param name="projectLookup">Lookup dictionary for project resolution</param>
    /// <param name="dependencyGraph">Dependency graph to update</param>
    /// <returns>Tuple containing count of resolved and unresolved dependencies</returns>
    private async Task<(int resolved, int unresolved)> ResolveDependenciesAsync(ProjectInfo project, Dictionary<string, ProjectInfo> projectLookup, DependencyGraph dependencyGraph)
    {
        var resolvedCount = 0;
        var unresolvedCount = 0;

        foreach (var dependency in project.ProjectReferences)
        {
            try
            {
                var resolvedProject = ResolveProjectReference(dependency, project, projectLookup);
                
                if (resolvedProject != null)
                {
                    // Mark dependency as resolved
                    dependency.IsResolved = true;
                    dependency.ReferencedProjectName = resolvedProject.ProjectName;
                    dependency.ReferencedProjectPath = resolvedProject.FilePath;
                    
                    // Add dependency to graph
                    dependencyGraph.AddDependency(project.FilePath, resolvedProject.FilePath);
                    resolvedCount++;
                    
                    _logger.LogDebug("Resolved dependency: '{SourceProject}' -> '{TargetProject}'", 
                        project.ProjectName, resolvedProject.ProjectName);
                }
                else
                {
                    // Flag as missing dependency
                    dependency.IsResolved = false;
                    unresolvedCount++;
                    
                    _logger.LogWarning("Could not resolve dependency '{DependencyPath}' for project '{ProjectName}'", 
                        dependency.ReferencedProjectPath, project.ProjectName);
                }
            }
            catch (Exception ex)
            {
                dependency.IsResolved = false;
                unresolvedCount++;
                
                _logger.LogError(ex, "Error resolving dependency '{DependencyPath}' for project '{ProjectName}': {ErrorMessage}", 
                    dependency.ReferencedProjectPath, project.ProjectName, ex.Message);
            }
        }
        
        await Task.CompletedTask; // For async consistency
        return (resolvedCount, unresolvedCount);
    }

    /// <summary>
    /// Resolves a project reference to an actual project
    /// </summary>
    /// <param name="dependency">Project dependency to resolve</param>
    /// <param name="sourceProject">Project that contains the reference</param>
    /// <param name="projectLookup">Lookup dictionary for project resolution</param>
    /// <returns>Resolved ProjectInfo or null if not found</returns>
    private ProjectInfo? ResolveProjectReference(ProjectDependency dependency, ProjectInfo sourceProject, Dictionary<string, ProjectInfo> projectLookup)
    {
        var referencePath = dependency.ReferencedProjectPath;
        
        // Try direct lookup first
        if (projectLookup.TryGetValue(referencePath, out var directMatch))
        {
            return directMatch;
        }
        
        // Try resolving relative path from source project directory
        var sourceDir = Path.GetDirectoryName(sourceProject.FilePath);
        if (!string.IsNullOrEmpty(sourceDir))
        {
            try
            {
                var absolutePath = Path.GetFullPath(Path.Combine(sourceDir, referencePath));
                if (projectLookup.TryGetValue(absolutePath, out var absoluteMatch))
                {
                    return absoluteMatch;
                }
            }
            catch
            {
                // Ignore path resolution errors
            }
        }
        
        // Try by project name
        var projectName = Path.GetFileNameWithoutExtension(referencePath);
        if (projectLookup.TryGetValue(projectName, out var nameMatch))
        {
            return nameMatch;
        }
        
        // Try by filename
        var fileName = Path.GetFileName(referencePath);
        if (projectLookup.TryGetValue(fileName, out var fileMatch))
        {
            return fileMatch;
        }
        
        // Try normalized path variations
        var normalizedPaths = new[]
        {
            referencePath.Replace('\\', '/'),
            referencePath.Replace('/', '\\'),
            referencePath.Replace("../", ""),
            referencePath.Replace("..\\", "")
        };
        
        foreach (var normalizedPath in normalizedPaths)
        {
            if (projectLookup.TryGetValue(normalizedPath, out var normalizedMatch))
            {
                return normalizedMatch;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Parses project files to extract dependencies
    /// </summary>
    /// <param name="projects">List of projects to parse</param>
    private async Task ParseProjectFilesAsync(List<ProjectInfo> projects)
    {
        var parsedCount = 0;
        var errorCount = 0;

        for (int i = 0; i < projects.Count; i++)
        {
            var project = projects[i];
            try
            {
                var parser = GetParserForProject(project);
                if (parser != null)
                {
                    _logger.LogDebug("Parsing project file: {ProjectPath}", project.FilePath);
                    var parsedProject = await parser.ParseProjectFileAsync(project.FilePath);
                    
                    // Update the project with parsed information
                    project.ProjectReferences = parsedProject.ProjectReferences;
                    project.PackageReferences = parsedProject.PackageReferences;
                    project.AssemblyReferences = parsedProject.AssemblyReferences;
                    project.TargetFramework = parsedProject.TargetFramework;
                    
                    parsedCount++;
                    
                    _logger.LogDebug("Parsed project '{ProjectName}': {DependencyCount} project references, {PackageCount} package references",
                        project.ProjectName, project.ProjectReferences.Count, project.PackageReferences.Count);
                }
                else
                {
                    _logger.LogWarning("No parser found for project type: {ProjectType} ({ProjectPath})", 
                        project.Type, project.FilePath);
                    errorCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse project file: {ProjectPath}", project.FilePath);
                errorCount++;
            }
        }

        _logger.LogInformation("Project file parsing completed. Parsed: {ParsedCount}, Errors: {ErrorCount}", 
            parsedCount, errorCount);
    }

    /// <summary>
    /// Gets the appropriate parser for a project
    /// </summary>
    /// <param name="project">Project to get parser for</param>
    /// <returns>Project file parser or null if not found</returns>
    private IProjectFileParser? GetParserForProject(ProjectInfo project)
    {
        return _projectFileParsers.FirstOrDefault(parser => parser.CanParse(project.FilePath));
    }
}