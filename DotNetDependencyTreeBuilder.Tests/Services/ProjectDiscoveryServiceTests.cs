using DotNetDependencyTreeBuilder.Exceptions;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetDependencyTreeBuilder.Tests.Services;

public class ProjectDiscoveryServiceTests
{
    private readonly Mock<IFileSystemService> _mockFileSystemService;
    private readonly Mock<ILogger<ProjectDiscoveryService>> _mockLogger;
    private readonly ProjectDiscoveryService _projectDiscoveryService;

    public ProjectDiscoveryServiceTests()
    {
        _mockFileSystemService = new Mock<IFileSystemService>();
        _mockLogger = new Mock<ILogger<ProjectDiscoveryService>>();
        _projectDiscoveryService = new ProjectDiscoveryService(_mockFileSystemService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task DiscoverProjectsAsync_WithValidDirectory_ReturnsProjects()
    {
        // Arrange
        var rootDirectory = "/test/root";
        var csharpProject = "/test/root/Project1.csproj";
        var vbProject = "/test/root/Project2.vbproj";

        _mockFileSystemService.Setup(x => x.DirectoryExists(rootDirectory)).Returns(true);
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.csproj")).Returns(new[] { csharpProject });
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.vbproj")).Returns(new[] { vbProject });
        _mockFileSystemService.Setup(x => x.GetDirectories(rootDirectory)).Returns(Array.Empty<string>());

        // Act
        var result = await _projectDiscoveryService.DiscoverProjectsAsync(rootDirectory);

        // Assert
        var projects = result.ToList();
        Assert.Equal(2, projects.Count);
        
        var csharpProjectInfo = projects.First(p => p.Type == ProjectType.CSharp);
        Assert.Equal("Project1", csharpProjectInfo.ProjectName);
        Assert.Equal(Path.GetFullPath(csharpProject), csharpProjectInfo.FilePath);
        
        var vbProjectInfo = projects.First(p => p.Type == ProjectType.VisualBasic);
        Assert.Equal("Project2", vbProjectInfo.ProjectName);
        Assert.Equal(Path.GetFullPath(vbProject), vbProjectInfo.FilePath);
    }

    [Fact]
    public async Task DiscoverProjectsAsync_WithNestedDirectories_ReturnsAllProjects()
    {
        // Arrange
        var rootDirectory = "/test/root";
        var subDirectory = "/test/root/subfolder";
        var rootProject = "/test/root/RootProject.csproj";
        var subProject = "/test/root/subfolder/SubProject.csproj";

        _mockFileSystemService.Setup(x => x.DirectoryExists(rootDirectory)).Returns(true);
        
        // Root directory setup
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.csproj")).Returns(new[] { rootProject });
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.vbproj")).Returns(Array.Empty<string>());
        _mockFileSystemService.Setup(x => x.GetDirectories(rootDirectory)).Returns(new[] { subDirectory });
        
        // Subdirectory setup
        _mockFileSystemService.Setup(x => x.GetFiles(subDirectory, "*.csproj")).Returns(new[] { subProject });
        _mockFileSystemService.Setup(x => x.GetFiles(subDirectory, "*.vbproj")).Returns(Array.Empty<string>());
        _mockFileSystemService.Setup(x => x.GetDirectories(subDirectory)).Returns(Array.Empty<string>());

        // Act
        var result = await _projectDiscoveryService.DiscoverProjectsAsync(rootDirectory);

        // Assert
        var projects = result.ToList();
        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, p => p.ProjectName == "RootProject");
        Assert.Contains(projects, p => p.ProjectName == "SubProject");
    }

    [Fact]
    public async Task DiscoverProjectsAsync_WithEmptyDirectory_ReturnsEmptyCollection()
    {
        // Arrange
        var rootDirectory = "/test/empty";

        _mockFileSystemService.Setup(x => x.DirectoryExists(rootDirectory)).Returns(true);
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.csproj")).Returns(Array.Empty<string>());
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.vbproj")).Returns(Array.Empty<string>());
        _mockFileSystemService.Setup(x => x.GetDirectories(rootDirectory)).Returns(Array.Empty<string>());

        // Act
        var result = await _projectDiscoveryService.DiscoverProjectsAsync(rootDirectory);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DiscoverProjectsAsync_WithNullDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _projectDiscoveryService.DiscoverProjectsAsync(null!));
    }

    [Fact]
    public async Task DiscoverProjectsAsync_WithEmptyStringDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _projectDiscoveryService.DiscoverProjectsAsync(string.Empty));
    }

    [Fact]
    public async Task DiscoverProjectsAsync_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentDirectory = "/test/nonexistent";
        _mockFileSystemService.Setup(x => x.DirectoryExists(nonExistentDirectory)).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ProjectDiscoveryException>(() => 
            _projectDiscoveryService.DiscoverProjectsAsync(nonExistentDirectory));
    }

    [Fact]
    public async Task DiscoverProjectsAsync_WithUnauthorizedAccess_ContinuesDiscovery()
    {
        // Arrange
        var rootDirectory = "/test/root";
        var accessibleSubDir = "/test/root/accessible";
        var restrictedSubDir = "/test/root/restricted";
        var accessibleProject = "/test/root/accessible/Project.csproj";

        _mockFileSystemService.Setup(x => x.DirectoryExists(rootDirectory)).Returns(true);
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.csproj")).Returns(Array.Empty<string>());
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.vbproj")).Returns(Array.Empty<string>());
        _mockFileSystemService.Setup(x => x.GetDirectories(rootDirectory))
            .Returns(new[] { accessibleSubDir, restrictedSubDir });

        // Accessible directory
        _mockFileSystemService.Setup(x => x.GetFiles(accessibleSubDir, "*.csproj"))
            .Returns(new[] { accessibleProject });
        _mockFileSystemService.Setup(x => x.GetFiles(accessibleSubDir, "*.vbproj"))
            .Returns(Array.Empty<string>());
        _mockFileSystemService.Setup(x => x.GetDirectories(accessibleSubDir))
            .Returns(Array.Empty<string>());

        // Restricted directory throws UnauthorizedAccessException
        _mockFileSystemService.Setup(x => x.GetFiles(restrictedSubDir, "*.csproj"))
            .Throws<UnauthorizedAccessException>();

        // Act
        var result = await _projectDiscoveryService.DiscoverProjectsAsync(rootDirectory);

        // Assert
        var projects = result.ToList();
        Assert.Single(projects);
        Assert.Equal("Project", projects.First().ProjectName);
    }

    [Fact]
    public void Constructor_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var mockLogger = new Mock<ILogger<ProjectDiscoveryService>>();
        Assert.Throws<ArgumentNullException>(() => new ProjectDiscoveryService(null!, mockLogger.Object));
    }

    [Fact]
    public async Task DiscoverProjectsAsync_CreatesCorrectProjectInfo()
    {
        // Arrange
        var rootDirectory = "/test/root";
        var projectPath = "/test/root/TestProject.csproj";

        _mockFileSystemService.Setup(x => x.DirectoryExists(rootDirectory)).Returns(true);
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.csproj")).Returns(new[] { projectPath });
        _mockFileSystemService.Setup(x => x.GetFiles(rootDirectory, "*.vbproj")).Returns(Array.Empty<string>());
        _mockFileSystemService.Setup(x => x.GetDirectories(rootDirectory)).Returns(Array.Empty<string>());

        // Act
        var result = await _projectDiscoveryService.DiscoverProjectsAsync(rootDirectory);

        // Assert
        var project = result.Single();
        Assert.Equal("TestProject", project.ProjectName);
        Assert.Equal(ProjectType.CSharp, project.Type);
        Assert.Equal(Path.GetFullPath(projectPath), project.FilePath);
        Assert.NotNull(project.ProjectReferences);
        Assert.NotNull(project.PackageReferences);
        Assert.Empty(project.ProjectReferences);
        Assert.Empty(project.PackageReferences);
    }
}