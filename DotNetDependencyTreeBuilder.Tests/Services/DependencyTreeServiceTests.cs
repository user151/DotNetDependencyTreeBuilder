using DotNetDependencyTreeBuilder.Exceptions;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetDependencyTreeBuilder.Tests.Services;

public class DependencyTreeServiceTests : IDisposable
{
    private readonly Mock<IProjectDiscoveryService> _mockProjectDiscoveryService;
    private readonly Mock<IDependencyAnalysisService> _mockDependencyAnalysisService;
    private readonly Mock<ILogger<DependencyTreeService>> _mockLogger;
    private readonly DependencyTreeService _service;
    private readonly string _tempDirectory;

    public DependencyTreeServiceTests()
    {
        _mockProjectDiscoveryService = new Mock<IProjectDiscoveryService>();
        _mockDependencyAnalysisService = new Mock<IDependencyAnalysisService>();
        _mockLogger = new Mock<ILogger<DependencyTreeService>>();
        
        _service = new DependencyTreeService(
            _mockProjectDiscoveryService.Object,
            _mockDependencyAnalysisService.Object,
            _mockLogger.Object);

        // Create temporary directory for tests
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithValidDirectory_ReturnsSuccessExitCode()
    {
        // Arrange
        var projects = CreateSampleProjects();
        var dependencyGraph = CreateSampleDependencyGraph();
        var buildOrder = CreateSampleBuildOrder();

        _mockProjectDiscoveryService
            .Setup(x => x.DiscoverProjectsAsync(_tempDirectory))
            .ReturnsAsync(projects);

        _mockDependencyAnalysisService
            .Setup(x => x.AnalyzeDependenciesAsync(projects))
            .ReturnsAsync(dependencyGraph);

        _mockDependencyAnalysisService
            .Setup(x => x.GenerateBuildOrder(dependencyGraph))
            .Returns(buildOrder);

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(0);
        
        // Verify all services were called
        _mockProjectDiscoveryService.Verify(x => x.DiscoverProjectsAsync(_tempDirectory), Times.Once);
        _mockDependencyAnalysisService.Verify(x => x.AnalyzeDependenciesAsync(projects), Times.Once);
        _mockDependencyAnalysisService.Verify(x => x.GenerateBuildOrder(dependencyGraph), Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithEmptySourceDirectory_ReturnsErrorExitCode()
    {
        // Act
        var result = await _service.AnalyzeDependenciesAsync("");

        // Assert
        result.Should().Be(1);
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Source directory cannot be null or empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithNonExistentDirectory_ReturnsErrorExitCode()
    {
        // Arrange
        var nonExistentDirectory = @"C:\NonExistentDirectory";

        // Act
        var result = await _service.AnalyzeDependenciesAsync(nonExistentDirectory);

        // Assert
        result.Should().Be(1);
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Source directory does not exist")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithNoProjectsFound_ReturnsSuccessExitCode()
    {
        // Arrange
        var emptyProjects = new List<ProjectInfo>();

        _mockProjectDiscoveryService
            .Setup(x => x.DiscoverProjectsAsync(_tempDirectory))
            .ReturnsAsync(emptyProjects);

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(0);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No projects found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithCircularDependencies_ReturnsWarningExitCode()
    {
        // Arrange
        var projects = CreateSampleProjects();
        var dependencyGraph = CreateSampleDependencyGraph();
        var buildOrderWithCircularDeps = CreateBuildOrderWithCircularDependencies();

        _mockProjectDiscoveryService
            .Setup(x => x.DiscoverProjectsAsync(_tempDirectory))
            .ReturnsAsync(projects);

        _mockDependencyAnalysisService
            .Setup(x => x.AnalyzeDependenciesAsync(projects))
            .ReturnsAsync(dependencyGraph);

        _mockDependencyAnalysisService
            .Setup(x => x.GenerateBuildOrder(dependencyGraph))
            .Returns(buildOrderWithCircularDeps);

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(1);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Circular dependencies detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithProjectDiscoveryException_ReturnsCriticalErrorExitCode()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");

        _mockProjectDiscoveryService
            .Setup(x => x.DiscoverProjectsAsync(_tempDirectory))
            .ThrowsAsync(exception);

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(2);
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Critical error during dependency analysis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithDependencyAnalysisException_ReturnsCriticalErrorExitCode()
    {
        // Arrange
        var projects = CreateSampleProjects();
        var exception = new ProjectParsingException(@"C:\TestProjects\App.csproj", "Failed to parse project file");

        _mockProjectDiscoveryService
            .Setup(x => x.DiscoverProjectsAsync(_tempDirectory))
            .ReturnsAsync(projects);

        _mockDependencyAnalysisService
            .Setup(x => x.AnalyzeDependenciesAsync(projects))
            .ThrowsAsync(exception);

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(2);
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Critical error during dependency analysis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithVerboseLogging_LogsDetailedStatistics()
    {
        // Arrange
        var projects = CreateSampleProjects();
        var dependencyGraph = CreateSampleDependencyGraph();
        var buildOrder = CreateSampleBuildOrder();

        _mockProjectDiscoveryService
            .Setup(x => x.DiscoverProjectsAsync(_tempDirectory))
            .ReturnsAsync(projects);

        _mockDependencyAnalysisService
            .Setup(x => x.AnalyzeDependenciesAsync(projects))
            .ReturnsAsync(dependencyGraph);

        _mockDependencyAnalysisService
            .Setup(x => x.GenerateBuildOrder(dependencyGraph))
            .Returns(buildOrder);

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory, verbose: true);

        // Assert
        result.Should().Be(0);
        
        // Verify detailed logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Analysis Summary")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Detailed Analysis Statistics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithOutputPath_PassesOutputPathToOutput()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "results.txt");
        var projects = CreateSampleProjects();
        var dependencyGraph = CreateSampleDependencyGraph();
        var buildOrder = CreateSampleBuildOrder();

        _mockProjectDiscoveryService
            .Setup(x => x.DiscoverProjectsAsync(_tempDirectory))
            .ReturnsAsync(projects);

        _mockDependencyAnalysisService
            .Setup(x => x.AnalyzeDependenciesAsync(projects))
            .ReturnsAsync(dependencyGraph);

        _mockDependencyAnalysisService
            .Setup(x => x.GenerateBuildOrder(dependencyGraph))
            .Returns(buildOrder);

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory, outputPath);

        // Assert
        result.Should().Be(0);
        
        // Verify all operations completed successfully
        _mockProjectDiscoveryService.Verify(x => x.DiscoverProjectsAsync(_tempDirectory), Times.Once);
        _mockDependencyAnalysisService.Verify(x => x.AnalyzeDependenciesAsync(projects), Times.Once);
        _mockDependencyAnalysisService.Verify(x => x.GenerateBuildOrder(dependencyGraph), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullProjectDiscoveryService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DependencyTreeService(null!, _mockDependencyAnalysisService.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullDependencyAnalysisService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DependencyTreeService(_mockProjectDiscoveryService.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DependencyTreeService(_mockProjectDiscoveryService.Object, _mockDependencyAnalysisService.Object, null!));
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_LogsProgressInformation()
    {
        // Arrange
        var projects = CreateSampleProjects();
        var dependencyGraph = CreateSampleDependencyGraph();
        var buildOrder = CreateSampleBuildOrder();

        _mockProjectDiscoveryService
            .Setup(x => x.DiscoverProjectsAsync(_tempDirectory))
            .ReturnsAsync(projects);

        _mockDependencyAnalysisService
            .Setup(x => x.AnalyzeDependenciesAsync(projects))
            .ReturnsAsync(dependencyGraph);

        _mockDependencyAnalysisService
            .Setup(x => x.GenerateBuildOrder(dependencyGraph))
            .Returns(buildOrder);

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(0);
        
        // Verify progress logging
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Discovering projects")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Analyzing project dependencies")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating build order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static List<ProjectInfo> CreateSampleProjects()
    {
        return new List<ProjectInfo>
        {
            new()
            {
                FilePath = @"C:\TestProjects\Core\Core.csproj",
                ProjectName = "Core",
                Type = ProjectType.CSharp
            },
            new()
            {
                FilePath = @"C:\TestProjects\App\App.csproj",
                ProjectName = "App",
                Type = ProjectType.CSharp,
                ProjectReferences = new List<ProjectDependency>
                {
                    new()
                    {
                        ReferencedProjectPath = @"C:\TestProjects\Core\Core.csproj",
                        ReferencedProjectName = "Core",
                        IsResolved = true
                    }
                }
            }
        };
    }

    private static DependencyGraph CreateSampleDependencyGraph()
    {
        var graph = new DependencyGraph();
        var projects = CreateSampleProjects();
        
        foreach (var project in projects)
        {
            graph.AddProject(project);
        }
        
        graph.AddDependency(@"C:\TestProjects\App\App.csproj", @"C:\TestProjects\Core\Core.csproj");
        
        return graph;
    }

    private static BuildOrder CreateSampleBuildOrder()
    {
        var projects = CreateSampleProjects();
        
        return new BuildOrder
        {
            BuildLevels = new List<List<ProjectInfo>>
            {
                new() { projects[0] }, // Core project (no dependencies)
                new() { projects[1] }  // App project (depends on Core)
            },
            CircularDependencies = new List<string>()
        };
    }

    private static BuildOrder CreateBuildOrderWithCircularDependencies()
    {
        var projects = CreateSampleProjects();
        
        return new BuildOrder
        {
            BuildLevels = new List<List<ProjectInfo>>(),
            CircularDependencies = new List<string>
            {
                @"C:\TestProjects\App\App.csproj",
                @"C:\TestProjects\Core\Core.csproj"
            }
        };
    }
}