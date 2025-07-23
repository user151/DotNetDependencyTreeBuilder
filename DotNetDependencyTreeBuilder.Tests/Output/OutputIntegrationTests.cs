using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Output;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Output;

public class OutputIntegrationTests
{
    [Fact]
    public async Task TextOutput_WithComplexBuildOrder_ShouldFormatCorrectly()
    {
        // Arrange
        var buildOrder = CreateComplexBuildOrder();
        var output = ConsoleOutputFactory.Create(OutputFormat.Text);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            await output.OutputBuildOrderAsync(buildOrder, tempFile);

            // Assert
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("Build Order Analysis Results");
            content.Should().Contain("Projects Found: 5");
            content.Should().Contain("Build Levels: 3");
            content.Should().Contain("Circular Dependencies: 1");
            content.Should().Contain("CIRCULAR DEPENDENCIES DETECTED:");
            content.Should().Contain("ProjectA -> ProjectB -> ProjectA");
            content.Should().Contain("Level 1:");
            content.Should().Contain("Level 2:");
            content.Should().Contain("Level 3:");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task JsonOutput_WithComplexBuildOrder_ShouldFormatCorrectly()
    {
        // Arrange
        var buildOrder = CreateComplexBuildOrder();
        var output = ConsoleOutputFactory.Create(OutputFormat.Json);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            await output.OutputBuildOrderAsync(buildOrder, tempFile);

            // Assert
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("\"projectsFound\": 5");
            content.Should().Contain("\"buildLevels\": 3");
            content.Should().Contain("\"hasCircularDependencies\": true");
            content.Should().Contain("\"circularDependencies\":");
            content.Should().Contain("ProjectA -\\u003E ProjectB -\\u003E ProjectA");
            content.Should().Contain("\"buildOrder\":");
            content.Should().Contain("\"level\": 1");
            content.Should().Contain("\"projects\":");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CompositeOutput_ShouldOutputToBothConsoleAndFile()
    {
        // Arrange
        var buildOrder = CreateSimpleBuildOrder();
        var tempFile = Path.GetTempFileName();
        var output = ConsoleOutputFactory.CreateConsoleAndFile(OutputFormat.Text, tempFile);

        try
        {
            // Act
            await output.OutputBuildOrderAsync(buildOrder);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("Build Order Analysis Results");
            content.Should().Contain("Projects Found: 2");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Factory_WithInvalidFormat_ShouldThrowException()
    {
        // Act & Assert
        var action = () => ConsoleOutputFactory.Create((OutputFormat)999);
        action.Should().Throw<ArgumentException>().WithParameterName("format");
    }

    private BuildOrder CreateComplexBuildOrder()
    {
        return new BuildOrder
        {
            BuildLevels = new List<List<ProjectInfo>>
            {
                new()
                {
                    CreateProjectInfo("Core.csproj"),
                    CreateProjectInfo("Utilities.csproj")
                },
                new()
                {
                    CreateProjectInfo("Business.csproj")
                },
                new()
                {
                    CreateProjectInfo("Web.csproj"),
                    CreateProjectInfo("Console.csproj")
                }
            },
            CircularDependencies = new List<string>
            {
                "ProjectA -> ProjectB -> ProjectA"
            }
        };
    }

    private BuildOrder CreateSimpleBuildOrder()
    {
        return new BuildOrder
        {
            BuildLevels = new List<List<ProjectInfo>>
            {
                new()
                {
                    CreateProjectInfo("Project1.csproj"),
                    CreateProjectInfo("Project2.csproj")
                }
            }
        };
    }

    private ProjectInfo CreateProjectInfo(string fileName)
    {
        return new ProjectInfo
        {
            FilePath = $"/path/to/{fileName}",
            ProjectName = Path.GetFileNameWithoutExtension(fileName),
            Type = ProjectType.CSharp,
            TargetFramework = "net6.0"
        };
    }
}