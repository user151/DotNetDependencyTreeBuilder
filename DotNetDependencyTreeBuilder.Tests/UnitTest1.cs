using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetDependencyTreeBuilder.Tests;

public class CoreInterfacesTests
{
    [Fact]
    public void ProjectInfo_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var projectInfo = new ProjectInfo();

        // Assert
        projectInfo.FilePath.Should().BeEmpty();
        projectInfo.ProjectName.Should().BeEmpty();
        projectInfo.Type.Should().Be(ProjectType.CSharp);
        projectInfo.ProjectReferences.Should().NotBeNull().And.BeEmpty();
        projectInfo.PackageReferences.Should().NotBeNull().And.BeEmpty();
        projectInfo.TargetFramework.Should().BeEmpty();
    }

    [Fact]
    public void DependencyGraph_ShouldAddProjectsCorrectly()
    {
        // Arrange
        var graph = new DependencyGraph();
        var project = new ProjectInfo
        {
            FilePath = "test.csproj",
            ProjectName = "TestProject",
            Type = ProjectType.CSharp
        };

        // Act
        graph.AddProject(project);

        // Assert
        graph.Projects.Should().ContainKey("test.csproj");
        graph.Projects["test.csproj"].Should().Be(project);
        graph.AdjacencyList.Should().ContainKey("test.csproj");
        graph.AdjacencyList["test.csproj"].Should().BeEmpty();
    }

    [Fact]
    public void DependencyGraph_ShouldAddDependenciesCorrectly()
    {
        // Arrange
        var graph = new DependencyGraph();
        var project1 = new ProjectInfo { FilePath = "project1.csproj" };
        var project2 = new ProjectInfo { FilePath = "project2.csproj" };
        
        graph.AddProject(project1);
        graph.AddProject(project2);

        // Act
        graph.AddDependency("project1.csproj", "project2.csproj");

        // Assert
        graph.AdjacencyList["project1.csproj"].Should().Contain("project2.csproj");
        graph.AdjacencyList["project2.csproj"].Should().BeEmpty();
    }

    [Fact]
    public void FileSystemService_ShouldImplementInterface()
    {
        // Arrange & Act
        var mockLogger = new Mock<ILogger<FileSystemService>>();
        var fileSystemService = new FileSystemService(mockLogger.Object);

        // Assert
        fileSystemService.Should().NotBeNull();
        fileSystemService.Should().BeAssignableTo<DotNetDependencyTreeBuilder.Interfaces.IFileSystemService>();
    }

    [Fact]
    public void BuildOrder_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var buildOrder = new BuildOrder();

        // Assert
        buildOrder.BuildLevels.Should().NotBeNull().And.BeEmpty();
        buildOrder.CircularDependencies.Should().NotBeNull().And.BeEmpty();
        buildOrder.HasCircularDependencies.Should().BeFalse();
        buildOrder.TotalProjects.Should().Be(0);
        buildOrder.TotalLevels.Should().Be(0);
    }
}