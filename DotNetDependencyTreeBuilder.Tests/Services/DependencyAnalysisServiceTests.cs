using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetDependencyTreeBuilder.Tests.Services;

public class DependencyAnalysisServiceTests
{
    private readonly DependencyAnalysisService _service;
    private readonly Mock<ILogger<DependencyAnalysisService>> _mockLogger;
    private readonly List<IProjectFileParser> _mockParsers;

    public DependencyAnalysisServiceTests()
    {
        _mockLogger = new Mock<ILogger<DependencyAnalysisService>>();
        _mockParsers = new List<IProjectFileParser>();
        _service = new DependencyAnalysisService(_mockParsers, _mockLogger.Object);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithEmptyProjects_ReturnsEmptyGraph()
    {
        // Arrange
        var projects = new List<ProjectInfo>();

        // Act
        var result = await _service.AnalyzeDependenciesAsync(projects);

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().BeEmpty();
        result.AdjacencyList.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithSingleProject_ReturnsGraphWithOneProject()
    {
        // Arrange
        var projects = new List<ProjectInfo>
        {
            new()
            {
                FilePath = @"C:\Projects\App\App.csproj",
                ProjectName = "App",
                Type = ProjectType.CSharp
            }
        };

        // Act
        var result = await _service.AnalyzeDependenciesAsync(projects);

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().HaveCount(1);
        result.Projects.Should().ContainKey(@"C:\Projects\App\App.csproj");
        result.AdjacencyList.Should().ContainKey(@"C:\Projects\App\App.csproj");
        result.AdjacencyList[@"C:\Projects\App\App.csproj"].Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithResolvedDependencies_BuildsCorrectGraph()
    {
        // Arrange
        var coreProject = new ProjectInfo
        {
            FilePath = @"C:\Projects\Core\Core.csproj",
            ProjectName = "Core",
            Type = ProjectType.CSharp
        };

        var appProject = new ProjectInfo
        {
            FilePath = @"C:\Projects\App\App.csproj",
            ProjectName = "App",
            Type = ProjectType.CSharp,
            ProjectReferences = new List<ProjectDependency>
            {
                new()
                {
                    ReferencedProjectPath = @"..\Core\Core.csproj",
                    ReferencedProjectName = "Core",
                    IsResolved = false
                }
            }
        };

        var projects = new List<ProjectInfo> { coreProject, appProject };

        // Act
        var result = await _service.AnalyzeDependenciesAsync(projects);

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().HaveCount(2);
        
        // Check that dependency was resolved
        appProject.ProjectReferences[0].IsResolved.Should().BeTrue();
        appProject.ProjectReferences[0].ReferencedProjectPath.Should().Be(coreProject.FilePath);
        
        // Check graph structure
        result.AdjacencyList[appProject.FilePath].Should().Contain(coreProject.FilePath);
        result.AdjacencyList[coreProject.FilePath].Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithUnresolvedDependencies_FlagsAsMissing()
    {
        // Arrange
        var appProject = new ProjectInfo
        {
            FilePath = @"C:\Projects\App\App.csproj",
            ProjectName = "App",
            Type = ProjectType.CSharp,
            ProjectReferences = new List<ProjectDependency>
            {
                new()
                {
                    ReferencedProjectPath = @"..\MissingProject\MissingProject.csproj",
                    ReferencedProjectName = "MissingProject",
                    IsResolved = false
                }
            }
        };

        var projects = new List<ProjectInfo> { appProject };

        // Act
        var result = await _service.AnalyzeDependenciesAsync(projects);

        // Assert
        result.Should().NotBeNull();
        appProject.ProjectReferences[0].IsResolved.Should().BeFalse();
        result.AdjacencyList[appProject.FilePath].Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithComplexDependencyChain_ResolvesCorrectly()
    {
        // Arrange
        var coreProject = new ProjectInfo
        {
            FilePath = @"C:\Projects\Core\Core.csproj",
            ProjectName = "Core",
            Type = ProjectType.CSharp
        };

        var businessProject = new ProjectInfo
        {
            FilePath = @"C:\Projects\Business\Business.csproj",
            ProjectName = "Business",
            Type = ProjectType.CSharp,
            ProjectReferences = new List<ProjectDependency>
            {
                new()
                {
                    ReferencedProjectPath = @"..\Core\Core.csproj",
                    ReferencedProjectName = "Core",
                    IsResolved = false
                }
            }
        };

        var appProject = new ProjectInfo
        {
            FilePath = @"C:\Projects\App\App.csproj",
            ProjectName = "App",
            Type = ProjectType.CSharp,
            ProjectReferences = new List<ProjectDependency>
            {
                new()
                {
                    ReferencedProjectPath = @"..\Business\Business.csproj",
                    ReferencedProjectName = "Business",
                    IsResolved = false
                },
                new()
                {
                    ReferencedProjectPath = @"..\Core\Core.csproj",
                    ReferencedProjectName = "Core",
                    IsResolved = false
                }
            }
        };

        var projects = new List<ProjectInfo> { coreProject, businessProject, appProject };

        // Act
        var result = await _service.AnalyzeDependenciesAsync(projects);

        // Assert
        result.Should().NotBeNull();
        
        // All dependencies should be resolved
        businessProject.ProjectReferences[0].IsResolved.Should().BeTrue();
        appProject.ProjectReferences[0].IsResolved.Should().BeTrue();
        appProject.ProjectReferences[1].IsResolved.Should().BeTrue();
        
        // Check graph structure
        result.AdjacencyList[appProject.FilePath].Should().Contain(businessProject.FilePath);
        result.AdjacencyList[appProject.FilePath].Should().Contain(coreProject.FilePath);
        result.AdjacencyList[businessProject.FilePath].Should().Contain(coreProject.FilePath);
        result.AdjacencyList[coreProject.FilePath].Should().BeEmpty();
    }

    [Fact]
    public void GenerateBuildOrder_WithSimpleDependencies_ReturnsCorrectOrder()
    {
        // Arrange
        var dependencyGraph = new DependencyGraph();
        
        var coreProject = new ProjectInfo
        {
            FilePath = @"C:\Projects\Core\Core.csproj",
            ProjectName = "Core",
            Type = ProjectType.CSharp
        };
        
        var appProject = new ProjectInfo
        {
            FilePath = @"C:\Projects\App\App.csproj",
            ProjectName = "App",
            Type = ProjectType.CSharp
        };
        
        dependencyGraph.AddProject(coreProject);
        dependencyGraph.AddProject(appProject);
        dependencyGraph.AddDependency(appProject.FilePath, coreProject.FilePath);

        // Act
        var result = _service.GenerateBuildOrder(dependencyGraph);

        // Assert
        result.Should().NotBeNull();
        result.HasCircularDependencies.Should().BeFalse();
        result.BuildLevels.Should().HaveCount(2);
        
        // Core should be in first level (no dependencies)
        result.BuildLevels[0].Should().HaveCount(1);
        result.BuildLevels[0][0].ProjectName.Should().Be("Core");
        
        // App should be in second level (depends on Core)
        result.BuildLevels[1].Should().HaveCount(1);
        result.BuildLevels[1][0].ProjectName.Should().Be("App");
    }

    [Fact]
    public void GenerateBuildOrder_WithParallelProjects_GroupsCorrectly()
    {
        // Arrange
        var dependencyGraph = new DependencyGraph();
        
        var project1 = new ProjectInfo
        {
            FilePath = @"C:\Projects\Project1\Project1.csproj",
            ProjectName = "Project1",
            Type = ProjectType.CSharp
        };
        
        var project2 = new ProjectInfo
        {
            FilePath = @"C:\Projects\Project2\Project2.csproj",
            ProjectName = "Project2",
            Type = ProjectType.CSharp
        };
        
        dependencyGraph.AddProject(project1);
        dependencyGraph.AddProject(project2);

        // Act
        var result = _service.GenerateBuildOrder(dependencyGraph);

        // Assert
        result.Should().NotBeNull();
        result.HasCircularDependencies.Should().BeFalse();
        result.BuildLevels.Should().HaveCount(1);
        result.BuildLevels[0].Should().HaveCount(2);
        result.TotalProjects.Should().Be(2);
        result.TotalLevels.Should().Be(1);
    }

    [Fact]
    public void GenerateBuildOrder_WithCircularDependencies_DetectsCircles()
    {
        // Arrange
        var dependencyGraph = new DependencyGraph();
        
        var project1 = new ProjectInfo
        {
            FilePath = @"C:\Projects\Project1\Project1.csproj",
            ProjectName = "Project1",
            Type = ProjectType.CSharp
        };
        
        var project2 = new ProjectInfo
        {
            FilePath = @"C:\Projects\Project2\Project2.csproj",
            ProjectName = "Project2",
            Type = ProjectType.CSharp
        };
        
        dependencyGraph.AddProject(project1);
        dependencyGraph.AddProject(project2);
        dependencyGraph.AddDependency(project1.FilePath, project2.FilePath);
        dependencyGraph.AddDependency(project2.FilePath, project1.FilePath);

        // Act
        var result = _service.GenerateBuildOrder(dependencyGraph);

        // Assert
        result.Should().NotBeNull();
        result.HasCircularDependencies.Should().BeTrue();
        result.CircularDependencies.Should().HaveCount(2);
        result.CircularDependencies.Should().Contain(project1.FilePath);
        result.CircularDependencies.Should().Contain(project2.FilePath);
    }

    [Fact]

    public async Task AnalyzeDependenciesAsync_WithNullProjects_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AnalyzeDependenciesAsync(null!));
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Projects collection cannot be null")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithEmptyProjects_LogsWarning()
    {
        // Arrange
        var projects = new List<ProjectInfo>();

        // Act
        var result = await _service.AnalyzeDependenciesAsync(projects);

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().BeEmpty();
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No projects provided for dependency analysis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithValidProjects_LogsProgressInformation()
    {
        // Arrange
        var projects = new List<ProjectInfo>
        {
            new()
            {
                FilePath = @"C:\Projects\App\App.csproj",
                ProjectName = "App",
                Type = ProjectType.CSharp
            }
        };

        // Act
        var result = await _service.AnalyzeDependenciesAsync(projects);

        // Assert
        result.Should().NotBeNull();
        
        // Verify information logs were created
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting dependency analysis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Dependency analysis completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GenerateBuildOrder_WithNullDependencyGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.GenerateBuildOrder(null!));
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Dependency graph cannot be null")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GenerateBuildOrder_WithValidGraph_LogsInformation()
    {
        // Arrange
        var dependencyGraph = new DependencyGraph();
        var project = new ProjectInfo
        {
            FilePath = @"C:\Projects\App\App.csproj",
            ProjectName = "App",
            Type = ProjectType.CSharp
        };
        dependencyGraph.AddProject(project);

        // Act
        var result = _service.GenerateBuildOrder(dependencyGraph);

        // Assert
        result.Should().NotBeNull();
        
        // Verify information logs were created
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating build order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Build order generated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithUnresolvedDependencies_LogsWarnings()
    {
        // Arrange
        var appProject = new ProjectInfo
        {
            FilePath = @"C:\Projects\App\App.csproj",
            ProjectName = "App",
            Type = ProjectType.CSharp,
            ProjectReferences = new List<ProjectDependency>
            {
                new()
                {
                    ReferencedProjectPath = @"..\MissingProject\MissingProject.csproj",
                    ReferencedProjectName = "MissingProject",
                    IsResolved = false
                }
            }
        };

        var projects = new List<ProjectInfo> { appProject };

        // Act
        var result = await _service.AnalyzeDependenciesAsync(projects);

        // Assert
        result.Should().NotBeNull();
        appProject.ProjectReferences[0].IsResolved.Should().BeFalse();
        
        // Verify warning was logged for unresolved dependency
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not resolve dependency")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }}
