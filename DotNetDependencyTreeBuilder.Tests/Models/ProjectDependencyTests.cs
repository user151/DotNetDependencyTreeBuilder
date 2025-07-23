using DotNetDependencyTreeBuilder.Models;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Models;

public class ProjectDependencyTests
{
    [Fact]
    public void ProjectDependency_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var dependency = new ProjectDependency();

        // Assert
        dependency.ReferencedProjectPath.Should().BeEmpty();
        dependency.ReferencedProjectName.Should().BeEmpty();
        dependency.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void ProjectDependency_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var dependency = new ProjectDependency();

        // Act
        dependency.ReferencedProjectPath = "/path/to/referenced.csproj";
        dependency.ReferencedProjectName = "ReferencedProject";
        dependency.IsResolved = true;

        // Assert
        dependency.ReferencedProjectPath.Should().Be("/path/to/referenced.csproj");
        dependency.ReferencedProjectName.Should().Be("ReferencedProject");
        dependency.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void ProjectDependency_ShouldHandleUnresolvedDependency()
    {
        // Arrange & Act
        var dependency = new ProjectDependency
        {
            ReferencedProjectPath = "../MissingProject/MissingProject.csproj",
            ReferencedProjectName = "MissingProject",
            IsResolved = false
        };

        // Assert
        dependency.ReferencedProjectPath.Should().Be("../MissingProject/MissingProject.csproj");
        dependency.ReferencedProjectName.Should().Be("MissingProject");
        dependency.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void ProjectDependency_ShouldHandleResolvedDependency()
    {
        // Arrange & Act
        var dependency = new ProjectDependency
        {
            ReferencedProjectPath = "/full/path/to/ExistingProject.csproj",
            ReferencedProjectName = "ExistingProject",
            IsResolved = true
        };

        // Assert
        dependency.ReferencedProjectPath.Should().Be("/full/path/to/ExistingProject.csproj");
        dependency.ReferencedProjectName.Should().Be("ExistingProject");
        dependency.IsResolved.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "", false)]
    [InlineData("path.csproj", "", false)]
    [InlineData("", "Name", false)]
    [InlineData("path.csproj", "Name", false)]
    [InlineData("path.csproj", "Name", true)]
    public void ProjectDependency_ShouldHandleVariousPropertyCombinations(
        string path, string name, bool isResolved)
    {
        // Arrange & Act
        var dependency = new ProjectDependency
        {
            ReferencedProjectPath = path,
            ReferencedProjectName = name,
            IsResolved = isResolved
        };

        // Assert
        dependency.ReferencedProjectPath.Should().Be(path);
        dependency.ReferencedProjectName.Should().Be(name);
        dependency.IsResolved.Should().Be(isResolved);
    }

    [Fact]
    public void ProjectDependency_ShouldSupportRelativePaths()
    {
        // Arrange & Act
        var dependency = new ProjectDependency
        {
            ReferencedProjectPath = "../Common/Common.csproj",
            ReferencedProjectName = "Common",
            IsResolved = true
        };

        // Assert
        dependency.ReferencedProjectPath.Should().Be("../Common/Common.csproj");
        dependency.ReferencedProjectName.Should().Be("Common");
        dependency.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void ProjectDependency_ShouldSupportAbsolutePaths()
    {
        // Arrange & Act
        var dependency = new ProjectDependency
        {
            ReferencedProjectPath = "C:\\Projects\\Solution\\Library\\Library.csproj",
            ReferencedProjectName = "Library",
            IsResolved = true
        };

        // Assert
        dependency.ReferencedProjectPath.Should().Be("C:\\Projects\\Solution\\Library\\Library.csproj");
        dependency.ReferencedProjectName.Should().Be("Library");
        dependency.IsResolved.Should().BeTrue();
    }
}