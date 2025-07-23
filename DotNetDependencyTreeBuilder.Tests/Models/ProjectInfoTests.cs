using DotNetDependencyTreeBuilder.Models;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Models;

public class ProjectInfoTests
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
    public void ProjectInfo_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var projectInfo = new ProjectInfo();
        var projectDependency = new ProjectDependency
        {
            ReferencedProjectPath = "dependency.csproj",
            ReferencedProjectName = "Dependency",
            IsResolved = true
        };
        var packageReference = new PackageReference
        {
            PackageName = "Newtonsoft.Json",
            Version = "13.0.1"
        };

        // Act
        projectInfo.FilePath = "/path/to/project.csproj";
        projectInfo.ProjectName = "TestProject";
        projectInfo.Type = ProjectType.VisualBasic;
        projectInfo.ProjectReferences.Add(projectDependency);
        projectInfo.PackageReferences.Add(packageReference);
        projectInfo.TargetFramework = "net6.0";

        // Assert
        projectInfo.FilePath.Should().Be("/path/to/project.csproj");
        projectInfo.ProjectName.Should().Be("TestProject");
        projectInfo.Type.Should().Be(ProjectType.VisualBasic);
        projectInfo.ProjectReferences.Should().HaveCount(1);
        projectInfo.ProjectReferences[0].Should().Be(projectDependency);
        projectInfo.PackageReferences.Should().HaveCount(1);
        projectInfo.PackageReferences[0].Should().Be(packageReference);
        projectInfo.TargetFramework.Should().Be("net6.0");
    }

    [Fact]
    public void ProjectInfo_ShouldAllowMultipleProjectReferences()
    {
        // Arrange
        var projectInfo = new ProjectInfo();
        var dependency1 = new ProjectDependency
        {
            ReferencedProjectPath = "dependency1.csproj",
            ReferencedProjectName = "Dependency1",
            IsResolved = true
        };
        var dependency2 = new ProjectDependency
        {
            ReferencedProjectPath = "dependency2.csproj",
            ReferencedProjectName = "Dependency2",
            IsResolved = false
        };

        // Act
        projectInfo.ProjectReferences.Add(dependency1);
        projectInfo.ProjectReferences.Add(dependency2);

        // Assert
        projectInfo.ProjectReferences.Should().HaveCount(2);
        projectInfo.ProjectReferences.Should().Contain(dependency1);
        projectInfo.ProjectReferences.Should().Contain(dependency2);
    }

    [Fact]
    public void ProjectInfo_ShouldAllowMultiplePackageReferences()
    {
        // Arrange
        var projectInfo = new ProjectInfo();
        var package1 = new PackageReference
        {
            PackageName = "Microsoft.Extensions.Logging",
            Version = "6.0.0"
        };
        var package2 = new PackageReference
        {
            PackageName = "System.CommandLine",
            Version = "2.0.0-beta4.22272.1"
        };

        // Act
        projectInfo.PackageReferences.Add(package1);
        projectInfo.PackageReferences.Add(package2);

        // Assert
        projectInfo.PackageReferences.Should().HaveCount(2);
        projectInfo.PackageReferences.Should().Contain(package1);
        projectInfo.PackageReferences.Should().Contain(package2);
    }

    [Theory]
    [InlineData(ProjectType.CSharp)]
    [InlineData(ProjectType.VisualBasic)]
    public void ProjectInfo_ShouldSupportAllProjectTypes(ProjectType projectType)
    {
        // Arrange
        var projectInfo = new ProjectInfo();

        // Act
        projectInfo.Type = projectType;

        // Assert
        projectInfo.Type.Should().Be(projectType);
    }

    [Fact]
    public void ProjectInfo_ShouldHandleEmptyCollections()
    {
        // Arrange & Act
        var projectInfo = new ProjectInfo
        {
            FilePath = "test.csproj",
            ProjectName = "Test"
        };

        // Assert
        projectInfo.ProjectReferences.Should().BeEmpty();
        projectInfo.PackageReferences.Should().BeEmpty();
    }
}