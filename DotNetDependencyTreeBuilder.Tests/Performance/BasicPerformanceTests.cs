using DotNetDependencyTreeBuilder.Services;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Parsers;
using DotNetDependencyTreeBuilder.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;

namespace DotNetDependencyTreeBuilder.Tests.Performance;

/// <summary>
/// Performance tests to ensure the application can handle reasonable workloads
/// Tests cover large project structures, deep nesting, and memory usage
/// </summary>
public class BasicPerformanceTests : IDisposable
{
    private readonly string _tempTestPath;
    private readonly Mock<ILogger<DependencyTreeService>> _mockLogger;
    private readonly List<string> _tempDirectories;

    public BasicPerformanceTests()
    {
        _tempTestPath = Path.Combine(Path.GetTempPath(), $"DependencyTreeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempTestPath);
        _mockLogger = new Mock<ILogger<DependencyTreeService>>();
        _tempDirectories = new List<string> { _tempTestPath };
    }

    [Fact]
    public async Task LargeProjectStructure_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        const int projectCount = 50;
        await CreateLargeProjectStructure(projectCount);
        
        var service = CreateDependencyTreeService();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(_tempTestPath);

        // Assert
        stopwatch.Stop();
        
        Assert.Equal(0, exitCode);
        
        // Should complete within 15 seconds for 50 projects
        Assert.True(stopwatch.ElapsedMilliseconds < 15000, 
            $"Analysis took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 15-second threshold");
        
        // Log performance metrics
        var projectsPerSecond = (double)projectCount / stopwatch.Elapsed.TotalSeconds;
        Assert.True(projectsPerSecond > 1, 
            $"Performance too slow: {projectsPerSecond:F2} projects per second");
    }

    [Fact]
    public async Task DeepNestedStructure_ShouldHandleDeepDirectoryHierarchy()
    {
        // Arrange
        const int depth = 10;
        const int projectsPerLevel = 2;
        
        await CreateDeepNestedStructure(depth, projectsPerLevel);
        
        var service = CreateDependencyTreeService();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(_tempTestPath);

        // Assert
        stopwatch.Stop();
        
        Assert.Equal(0, exitCode);
        
        // Should complete within 10 seconds for deep nested structure
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
            $"Deep nested analysis took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 10-second threshold");
    }

    [Fact]
    public async Task MemoryUsage_ShouldNotExceedReasonableLimits()
    {
        // Arrange
        const int projectCount = 100;
        
        await CreateLargeProjectStructure(projectCount);
        
        var service = CreateDependencyTreeService();
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(_tempTestPath);

        // Assert
        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
        
        Assert.Equal(0, exitCode);
        
        // Should not use more than 50MB for 100 projects
        const long maxMemoryUsage = 50 * 1024 * 1024; // 50MB
        Assert.True(memoryUsed < maxMemoryUsage, 
            $"Memory usage {memoryUsed / (1024 * 1024)}MB exceeds the 50MB threshold");
    }

    [Fact]
    public async Task ComplexDependencyGraph_ShouldHandleMultipleLevels()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("ComplexGraph", new Dictionary<string, string>
        {
            // Level 1 - Foundation projects
            ["Foundation/Core/Core.csproj"] = TestDataManager.CreateBasicCSharpProject("Core"),
            ["Foundation/Utilities/Utilities.csproj"] = TestDataManager.CreateBasicCSharpProject("Utilities"),
            
            // Level 2 - Data and Business layers
            ["DataAccess/DataAccess.csproj"] = TestDataManager.CreateBasicCSharpProject("DataAccess", 
                new List<string> { "../Foundation/Core/Core.csproj", "../Foundation/Utilities/Utilities.csproj" }),
            ["BusinessLogic/BusinessLogic.csproj"] = TestDataManager.CreateBasicCSharpProject("BusinessLogic", 
                new List<string> { "../Foundation/Core/Core.csproj", "../DataAccess/DataAccess.csproj" }),
            
            // Level 3 - Application layers
            ["WebAPI/WebAPI.csproj"] = TestDataManager.CreateBasicCSharpProject("WebAPI", 
                new List<string> { "../BusinessLogic/BusinessLogic.csproj", "../Foundation/Core/Core.csproj" }),
            ["ConsoleApp/ConsoleApp.csproj"] = TestDataManager.CreateBasicCSharpProject("ConsoleApp", 
                new List<string> { "../BusinessLogic/BusinessLogic.csproj" })
        });

        var service = CreateDependencyTreeService();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        stopwatch.Stop();
        Assert.Equal(0, exitCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Complex dependency analysis took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 5-second threshold");
    }

    [Fact]
    public async Task ManyIndependentProjects_ShouldProcessInParallel()
    {
        // Arrange
        const int projectCount = 30;
        var projects = new Dictionary<string, string>();
        
        for (int i = 0; i < projectCount; i++)
        {
            var projectName = $"Independent{i:D2}";
            projects[$"{projectName}/{projectName}.csproj"] = TestDataManager.CreateBasicCSharpProject(projectName);
        }

        var tempPath = await CreateTemporaryTestScenario("IndependentProjects", projects);
        var service = CreateDependencyTreeService();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        stopwatch.Stop();
        Assert.Equal(0, exitCode);
        
        // Independent projects should be processed quickly
        Assert.True(stopwatch.ElapsedMilliseconds < 8000, 
            $"Independent projects analysis took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 8-second threshold");
        
        var projectsPerSecond = (double)projectCount / stopwatch.Elapsed.TotalSeconds;
        Assert.True(projectsPerSecond > 2, 
            $"Performance too slow for independent projects: {projectsPerSecond:F2} projects per second");
    }

    [Fact]
    public async Task LargeCircularDependency_ShouldDetectCyclesEfficiently()
    {
        // Arrange - Create a circular dependency chain with 10 projects
        const int chainLength = 10;
        var projects = new Dictionary<string, string>();
        
        for (int i = 0; i < chainLength; i++)
        {
            var projectName = $"Chain{i:D2}";
            var nextProject = $"Chain{((i + 1) % chainLength):D2}"; // Creates circular reference
            var nextProjectPath = $"../{nextProject}/{nextProject}.csproj";
            
            projects[$"{projectName}/{projectName}.csproj"] = TestDataManager.CreateBasicCSharpProject(projectName, 
                new List<string> { nextProjectPath });
        }

        var tempPath = await CreateTemporaryTestScenario("LargeCircular", projects);
        var service = CreateDependencyTreeService();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        stopwatch.Stop();
        Assert.Equal(1, exitCode); // Should detect circular dependency
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Circular dependency detection took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 5-second threshold");
    }

    [Fact]
    public async Task MixedProjectTypes_ShouldHandleVBAndCSharpEfficiently()
    {
        // Arrange
        const int csharpCount = 15;
        const int vbCount = 10;
        var projects = new Dictionary<string, string>();
        
        // Create C# projects
        for (int i = 0; i < csharpCount; i++)
        {
            var projectName = $"CSharp{i:D2}";
            projects[$"CSharp/{projectName}/{projectName}.csproj"] = TestDataManager.CreateBasicCSharpProject(projectName);
        }
        
        // Create VB.NET projects
        for (int i = 0; i < vbCount; i++)
        {
            var projectName = $"VB{i:D2}";
            projects[$"VB/{projectName}/{projectName}.vbproj"] = TestDataManager.CreateBasicVBProject(projectName);
        }

        var tempPath = await CreateTemporaryTestScenario("MixedTypes", projects);
        var service = CreateDependencyTreeService();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        stopwatch.Stop();
        Assert.Equal(0, exitCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 8000, 
            $"Mixed project types analysis took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 8-second threshold");
    }

    private async Task<string> CreateTemporaryTestScenario(string scenarioName, Dictionary<string, string> projectFiles)
    {
        var tempPath = await TestDataManager.CreateTemporaryTestScenario(scenarioName, projectFiles);
        _tempDirectories.Add(tempPath);
        return tempPath;
    }

    private async Task CreateLargeProjectStructure(int projectCount)
    {
        var random = new Random(42); // Fixed seed for reproducible tests
        var projectNames = new List<string>();
        
        // Create project names
        for (int i = 0; i < projectCount; i++)
        {
            projectNames.Add($"Project{i:D3}");
        }

        // Create projects with dependencies
        for (int i = 0; i < projectCount; i++)
        {
            var projectName = projectNames[i];
            var projectDir = Path.Combine(_tempTestPath, projectName);
            Directory.CreateDirectory(projectDir);

            var dependencies = new List<string>();
            var dependencyCount = random.Next(0, Math.Min(3, i)); // Max 3 dependencies
            
            for (int j = 0; j < dependencyCount; j++)
            {
                var depIndex = random.Next(0, i);
                var depName = projectNames[depIndex];
                if (!dependencies.Contains(depName))
                {
                    dependencies.Add(depName);
                }
            }

            var projectContent = CreateProjectFileContent(projectName, dependencies);
            var projectFile = Path.Combine(projectDir, $"{projectName}.csproj");
            await File.WriteAllTextAsync(projectFile, projectContent);
        }
    }

    private async Task CreateDeepNestedStructure(int depth, int projectsPerLevel)
    {
        var currentPath = _tempTestPath;
        
        for (int level = 0; level < depth; level++)
        {
            for (int project = 0; project < projectsPerLevel; project++)
            {
                var projectName = $"Level{level:D2}Project{project:D2}";
                var projectDir = Path.Combine(currentPath, projectName);
                Directory.CreateDirectory(projectDir);

                var dependencies = new List<string>();
                if (level > 0)
                {
                    // Add dependency to a project from the previous level
                    var prevLevelProject = $"Level{(level - 1):D2}Project{project % projectsPerLevel:D2}";
                    dependencies.Add(prevLevelProject);
                }

                var projectContent = CreateProjectFileContent(projectName, dependencies, level > 0);
                var projectFile = Path.Combine(projectDir, $"{projectName}.csproj");
                await File.WriteAllTextAsync(projectFile, projectContent);
            }
            
            if (projectsPerLevel > 0)
            {
                currentPath = Path.Combine(currentPath, $"Level{level:D2}Project0");
            }
        }
    }

    private string CreateProjectFileContent(string projectName, List<string> dependencies, bool useRelativePaths = false)
    {
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>";

        foreach (var dependency in dependencies)
        {
            var relativePath = useRelativePaths ? $"..\\{dependency}\\{dependency}.csproj" : $"..\\{dependency}\\{dependency}.csproj";
            content += $@"
    <ProjectReference Include=""{relativePath}"" />";
        }

        content += @"
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""6.0.0"" />
  </ItemGroup>

</Project>";

        return content;
    }

    private DependencyTreeService CreateDependencyTreeService()
    {
        var fileSystemService = new FileSystemService(Mock.Of<ILogger<FileSystemService>>());
        var projectDiscoveryService = new ProjectDiscoveryService(
            fileSystemService, 
            Mock.Of<ILogger<ProjectDiscoveryService>>());
        
        // Create parsers for the dependency analysis service
        var parsers = new List<IProjectFileParser>
        {
            new CSharpProjectParser(),
            new VBProjectParser()
        };
        
        var dependencyAnalysisService = new DependencyAnalysisService(
            parsers,
            Mock.Of<ILogger<DependencyAnalysisService>>());
        
        return new DependencyTreeService(
            projectDiscoveryService,
            dependencyAnalysisService,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        foreach (var tempDir in _tempDirectories)
        {
            TestDataManager.CleanupTemporaryPath(tempDir);
        }
    }
}