using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Output;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Output;

public class TextConsoleOutputTests : IDisposable
{
    private readonly TextConsoleOutput _output;
    private readonly StringWriter _consoleOutput;
    private readonly StringWriter _errorOutput;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;

    public TextConsoleOutputTests()
    {
        _output = new TextConsoleOutput();
        _consoleOutput = new StringWriter();
        _errorOutput = new StringWriter();
        _originalOut = Console.Out;
        _originalError = Console.Error;
        Console.SetOut(_consoleOutput);
        Console.SetError(_errorOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        _consoleOutput?.Dispose();
        _errorOutput?.Dispose();
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WithSimpleBuildOrder_ShouldFormatCorrectly()
    {
        // Arrange
        var buildOrder = CreateSampleBuildOrder();

        // Act
        await _output.OutputBuildOrderAsync(buildOrder);

        // Assert
        var result = _consoleOutput.ToString();
        result.Should().Contain("Build Order Analysis Results");
        result.Should().Contain("Projects Found: 4");
        result.Should().Contain("Build Levels: 2");
        result.Should().Contain("Circular Dependencies: None");
        result.Should().Contain("Build Order:");
        result.Should().Contain("Core.Library.csproj");
        result.Should().Contain("Business.Logic.csproj");
        result.Should().Contain("Web.API.csproj");
        // Should not contain level groupings
        result.Should().NotContain("Level 1:");
        result.Should().NotContain("Level 2:");
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WithCircularDependencies_ShouldShowCircularDependencies()
    {
        // Arrange
        var buildOrder = new BuildOrder
        {
            BuildLevels = new List<List<ProjectInfo>>
            {
                new() { CreateProjectInfo("Project1.csproj") }
            },
            CircularDependencies = new List<string> { "Project1 -> Project2 -> Project1" }
        };

        // Act
        await _output.OutputBuildOrderAsync(buildOrder);

        // Assert
        var result = _consoleOutput.ToString();
        result.Should().Contain("Circular Dependencies: 1");
        result.Should().Contain("CIRCULAR DEPENDENCIES DETECTED:");
        result.Should().Contain("Project1 -> Project2 -> Project1");
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WithParallelProjects_ShouldShowLinearOrder()
    {
        // Arrange
        var buildOrder = new BuildOrder
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

        // Act
        await _output.OutputBuildOrderAsync(buildOrder);

        // Assert
        var result = _consoleOutput.ToString();
        result.Should().Contain("Build Order:");
        result.Should().Contain("Project1.csproj");
        result.Should().Contain("Project2.csproj");
        // Should not contain level groupings or parallel indicators
        result.Should().NotContain("Level 1:");
        result.Should().NotContain("(Can be built in parallel)");
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WithOutputPath_ShouldWriteToFile()
    {
        // Arrange
        var buildOrder = CreateSampleBuildOrder();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            await _output.OutputBuildOrderAsync(buildOrder, tempFile);

            // Assert
            var fileContent = await File.ReadAllTextAsync(tempFile);
            fileContent.Should().Contain("Build Order Analysis Results");
            fileContent.Should().Contain("Projects Found: 4");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void OutputError_ShouldNotThrow()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act & Assert
        var action = () => _output.OutputError(errorMessage);
        action.Should().NotThrow();
    }

    [Fact]
    public void OutputInfo_ShouldNotThrow()
    {
        // Arrange
        var infoMessage = "Test info message";

        // Act & Assert
        var action = () => _output.OutputInfo(infoMessage);
        action.Should().NotThrow();
    }

    private BuildOrder CreateSampleBuildOrder()
    {
        return new BuildOrder
        {
            BuildLevels = new List<List<ProjectInfo>>
            {
                new()
                {
                    CreateProjectInfo("Core.Library.csproj"),
                    CreateProjectInfo("Utilities.csproj")
                },
                new()
                {
                    CreateProjectInfo("Business.Logic.csproj"),
                    CreateProjectInfo("Web.API.csproj")
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