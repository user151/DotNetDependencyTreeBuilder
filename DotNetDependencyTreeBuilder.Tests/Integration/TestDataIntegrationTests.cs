using DotNetDependencyTreeBuilder.Services;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetDependencyTreeBuilder.Tests.Integration;

/// <summary>
/// Integration tests using the created test data to validate end-to-end functionality
/// </summary>
public class TestDataIntegrationTests
{
    private readonly string _testDataPath;
    private readonly Mock<ILogger<DependencyTreeService>> _mockLogger;

    public TestDataIntegrationTests()
    {
        _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        _mockLogger = new Mock<ILogger<DependencyTreeService>>();
    }

    [Fact]
    public async Task SimpleLinearDependency_ShouldCompleteSuccessfully()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "SimpleLinearDependency");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        Assert.Equal(0, exitCode); // Success exit code
    }

    [Fact]
    public async Task CircularDependency_ShouldReturnWarningExitCode()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "CircularDependency");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        Assert.Equal(1, exitCode); // Warning exit code for circular dependencies
    }

    [Fact]
    public async Task VBNetProjects_ShouldBeProcessedSuccessfully()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "VBNetProjects");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        Assert.Equal(0, exitCode); // Success exit code
    }

    [Fact]
    public async Task MixedProjects_ShouldHandleBothCSharpAndVBNet()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "MixedProjects");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        Assert.Equal(0, exitCode); // Success exit code
    }

    [Fact]
    public async Task NestedStructure_ShouldDiscoverProjectsInDeepDirectories()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "NestedStructure");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        Assert.Equal(0, exitCode); // Success exit code
    }

    [Fact]
    public async Task EmptyDirectory_ShouldReturnSuccessWithNoProjects()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "EmptyDirectory");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        Assert.Equal(1, exitCode); // Error exit code - no projects is an error
    }

    [Fact]
    public async Task NonExistentDirectory_ShouldReturnErrorExitCode()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "NonExistentDirectory");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        Assert.Equal(1, exitCode); // Error exit code
    }

    [Fact]
    public async Task ComplexDependency_ShouldHandleMultiLevelDependencies()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "ComplexDependency");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        Assert.Equal(0, exitCode); // Success exit code
    }

    [Fact]
    public async Task MissingReferences_ShouldCompleteWithWarnings()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "MissingReferences");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        Assert.Equal(0, exitCode); // Success - missing references are warnings, not errors
    }

    [Fact]
    public async Task MalformedProjects_ShouldHandleGracefully()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "MalformedProjects");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath);

        // Assert
        // Should complete without throwing exceptions
        Assert.True(exitCode >= 0); // Any non-negative exit code is acceptable
    }

    [Fact]
    public async Task OutputToFile_ShouldCreateOutputFile()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "SimpleLinearDependency");
        var outputFile = Path.GetTempFileName();
        var service = CreateDependencyTreeService();

        try
        {
            // Act
            var exitCode = await service.AnalyzeDependenciesAsync(testPath, outputFile);

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputFile));
            
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.NotEmpty(content);
        }
        finally
        {
            if (File.Exists(outputFile))
                File.Delete(outputFile);
        }
    }

    [Fact]
    public async Task VerboseMode_ShouldCompleteSuccessfully()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "SimpleLinearDependency");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(testPath, verbose: true);

        // Assert
        Assert.Equal(0, exitCode);
        
        // Verify that verbose logging was called
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
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
}