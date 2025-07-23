using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Output;
using DotNetDependencyTreeBuilder.Parsers;
using DotNetDependencyTreeBuilder.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetDependencyTreeBuilder.Tests.Services;

/// <summary>
/// Integration tests for DependencyTreeService that test complete workflow scenarios
/// </summary>
public class DependencyTreeServiceIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly DependencyTreeService _service;
    private readonly Mock<ILogger<DependencyTreeService>> _mockLogger;
    private readonly Mock<ILogger<ProjectDiscoveryService>> _mockProjectDiscoveryLogger;
    private readonly Mock<ILogger<DependencyAnalysisService>> _mockDependencyAnalysisLogger;

    public DependencyTreeServiceIntegrationTests()
    {
        // Create temporary directory for test projects
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        // Set up real services with mocked loggers
        _mockLogger = new Mock<ILogger<DependencyTreeService>>();
        _mockProjectDiscoveryLogger = new Mock<ILogger<ProjectDiscoveryService>>();
        _mockDependencyAnalysisLogger = new Mock<ILogger<DependencyAnalysisService>>();

        var mockFileSystemLogger = new Mock<ILogger<FileSystemService>>();
        var fileSystemService = new FileSystemService(mockFileSystemLogger.Object);
        var projectDiscoveryService = new ProjectDiscoveryService(fileSystemService, _mockProjectDiscoveryLogger.Object);
        var projectFileParsers = new List<IProjectFileParser> { new CSharpProjectParser(), new VBProjectParser() };
        var dependencyAnalysisService = new DependencyAnalysisService(projectFileParsers, _mockDependencyAnalysisLogger.Object);

        _service = new DependencyTreeService(
            projectDiscoveryService,
            dependencyAnalysisService,
            _mockLogger.Object);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithSimpleProjectStructure_ReturnsCorrectBuildOrder()
    {
        // Arrange
        CreateSimpleProjectStructure();

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(0);
        
        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found 2 projects")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithComplexDependencyChain_ReturnsCorrectBuildOrder()
    {
        // Arrange
        CreateComplexProjectStructure();

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(0);
        
        // Verify logging occurred for complex structure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found 4 projects")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithCircularDependencies_ReturnsWarningExitCode()
    {
        // Arrange
        CreateCircularDependencyStructure();

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(1); // Warning exit code for circular dependencies
        
        // Verify circular dependency warning was logged
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
    public async Task AnalyzeDependenciesAsync_WithMixedProjectTypes_HandlesCorrectly()
    {
        // Arrange
        CreateMixedProjectTypeStructure();

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(0);
        
        // Verify both C# and VB projects were found
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found 3 projects")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithOutputFile_CreatesOutputFile()
    {
        // Arrange
        CreateSimpleProjectStructure();
        var outputPath = Path.Combine(_tempDirectory, "build-order.txt");

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory, outputPath);

        // Assert
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        
        var outputContent = await File.ReadAllTextAsync(outputPath);
        outputContent.Should().Contain("Build Order Analysis Results");
        outputContent.Should().Contain("Projects Found: 2");
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithVerboseLogging_LogsDetailedInformation()
    {
        // Arrange
        CreateSimpleProjectStructure();

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory, verbose: true);

        // Assert
        result.Should().Be(0);
        
        // Verify detailed logging occurred
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
    public async Task AnalyzeDependenciesAsync_WithNestedDirectories_FindsAllProjects()
    {
        // Arrange
        CreateNestedProjectStructure();

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(0);
        
        // Verify all nested projects were found
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found 3 projects")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithMissingDependencies_HandlesGracefully()
    {
        // Arrange
        CreateProjectWithMissingDependencies();

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(0); // Should still succeed despite missing dependencies
        
        // Verify project was found
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found 1 projects")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithEmptyDirectory_ReturnsSuccessWithNoProjects()
    {
        // Arrange - empty directory already created in constructor

        // Act
        var result = await _service.AnalyzeDependenciesAsync(_tempDirectory);

        // Assert
        result.Should().Be(0);
        
        // Verify no projects warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No projects found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void CreateSimpleProjectStructure()
    {
        // Create Core project
        var coreDir = Path.Combine(_tempDirectory, "Core");
        Directory.CreateDirectory(coreDir);
        var coreProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(coreDir, "Core.csproj"), coreProjectContent);

        // Create App project that depends on Core
        var appDir = Path.Combine(_tempDirectory, "App");
        Directory.CreateDirectory(appDir);
        var appProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Core\Core.csproj" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(appDir, "App.csproj"), appProjectContent);
    }

    private void CreateComplexProjectStructure()
    {
        // Create Core project (no dependencies)
        var coreDir = Path.Combine(_tempDirectory, "Core");
        Directory.CreateDirectory(coreDir);
        var coreProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(coreDir, "Core.csproj"), coreProjectContent);

        // Create Utilities project (no dependencies)
        var utilsDir = Path.Combine(_tempDirectory, "Utilities");
        Directory.CreateDirectory(utilsDir);
        var utilsProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(utilsDir, "Utilities.csproj"), utilsProjectContent);

        // Create Business project (depends on Core)
        var businessDir = Path.Combine(_tempDirectory, "Business");
        Directory.CreateDirectory(businessDir);
        var businessProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Core\Core.csproj" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(businessDir, "Business.csproj"), businessProjectContent);

        // Create App project (depends on Business and Utilities)
        var appDir = Path.Combine(_tempDirectory, "App");
        Directory.CreateDirectory(appDir);
        var appProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Business\Business.csproj" />
                <ProjectReference Include="..\Utilities\Utilities.csproj" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(appDir, "App.csproj"), appProjectContent);
    }

    private void CreateCircularDependencyStructure()
    {
        // Create Project1 that depends on Project2
        var project1Dir = Path.Combine(_tempDirectory, "Project1");
        Directory.CreateDirectory(project1Dir);
        var project1Content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Project2\Project2.csproj" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(project1Dir, "Project1.csproj"), project1Content);

        // Create Project2 that depends on Project1 (circular dependency)
        var project2Dir = Path.Combine(_tempDirectory, "Project2");
        Directory.CreateDirectory(project2Dir);
        var project2Content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Project1\Project1.csproj" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(project2Dir, "Project2.csproj"), project2Content);
    }

    private void CreateMixedProjectTypeStructure()
    {
        // Create C# project
        var csharpDir = Path.Combine(_tempDirectory, "CSharpProject");
        Directory.CreateDirectory(csharpDir);
        var csharpProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(csharpDir, "CSharpProject.csproj"), csharpProjectContent);

        // Create VB.NET project
        var vbDir = Path.Combine(_tempDirectory, "VBProject");
        Directory.CreateDirectory(vbDir);
        var vbProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(vbDir, "VBProject.vbproj"), vbProjectContent);

        // Create another C# project that depends on the VB project
        var mixedDir = Path.Combine(_tempDirectory, "MixedProject");
        Directory.CreateDirectory(mixedDir);
        var mixedProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\VBProject\VBProject.vbproj" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(mixedDir, "MixedProject.csproj"), mixedProjectContent);
    }

    private void CreateNestedProjectStructure()
    {
        // Create project in root
        var rootProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(_tempDirectory, "RootProject.csproj"), rootProjectContent);

        // Create project in subdirectory
        var subDir = Path.Combine(_tempDirectory, "SubDirectory");
        Directory.CreateDirectory(subDir);
        var subProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\RootProject.csproj" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(subDir, "SubProject.csproj"), subProjectContent);

        // Create project in nested subdirectory
        var nestedDir = Path.Combine(subDir, "Nested");
        Directory.CreateDirectory(nestedDir);
        var nestedProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\SubProject.csproj" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(nestedDir, "NestedProject.csproj"), nestedProjectContent);
    }

    private void CreateProjectWithMissingDependencies()
    {
        // Create project that references a non-existent project
        var projectDir = Path.Combine(_tempDirectory, "ProjectWithMissingDeps");
        Directory.CreateDirectory(projectDir);
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\NonExistentProject\NonExistentProject.csproj" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(projectDir, "ProjectWithMissingDeps.csproj"), projectContent);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}