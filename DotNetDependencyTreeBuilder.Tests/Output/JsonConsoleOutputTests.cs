using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Output;
using FluentAssertions;
using System.Text.Json;

namespace DotNetDependencyTreeBuilder.Tests.Output;

public class JsonConsoleOutputTests : IDisposable
{
    private readonly JsonConsoleOutput _output;
    private readonly StringWriter _consoleOutput;
    private readonly StringWriter _errorOutput;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;

    public JsonConsoleOutputTests()
    {
        _output = new JsonConsoleOutput();
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
    public async Task OutputBuildOrderAsync_WithSimpleBuildOrder_ShouldFormatAsValidJson()
    {
        // Arrange
        var buildOrder = CreateSampleBuildOrder();

        // Act
        await _output.OutputBuildOrderAsync(buildOrder);

        // Assert
        var result = _consoleOutput.ToString();
        var jsonDocument = JsonDocument.Parse(result);
        
        jsonDocument.RootElement.GetProperty("summary").GetProperty("projectsFound").GetInt32().Should().Be(3);
        jsonDocument.RootElement.GetProperty("summary").GetProperty("buildLevels").GetInt32().Should().Be(2);
        jsonDocument.RootElement.GetProperty("summary").GetProperty("hasCircularDependencies").GetBoolean().Should().BeFalse();
        
        var buildOrderArray = jsonDocument.RootElement.GetProperty("buildOrder");
        buildOrderArray.GetArrayLength().Should().Be(2);
        
        var level1 = buildOrderArray[0];
        level1.GetProperty("level").GetInt32().Should().Be(1);
        level1.GetProperty("projects").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WithCircularDependencies_ShouldIncludeCircularDependencies()
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
        var jsonDocument = JsonDocument.Parse(result);
        
        jsonDocument.RootElement.GetProperty("summary").GetProperty("hasCircularDependencies").GetBoolean().Should().BeTrue();
        var circularDeps = jsonDocument.RootElement.GetProperty("summary").GetProperty("circularDependencies");
        circularDeps.GetArrayLength().Should().Be(1);
        circularDeps[0].GetString().Should().Be("Project1 -> Project2 -> Project1");
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
            var jsonDocument = JsonDocument.Parse(fileContent);
            jsonDocument.RootElement.GetProperty("summary").GetProperty("projectsFound").GetInt32().Should().Be(3);
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

    [Fact]
    public async Task OutputBuildOrderAsync_ShouldIncludeProjectDetails()
    {
        // Arrange
        var buildOrder = new BuildOrder
        {
            BuildLevels = new List<List<ProjectInfo>>
            {
                new()
                {
                    new ProjectInfo
                    {
                        FilePath = "/path/to/Core.Library.csproj",
                        ProjectName = "Core.Library",
                        Type = ProjectType.CSharp,
                        TargetFramework = "net6.0"
                    }
                }
            }
        };

        // Act
        await _output.OutputBuildOrderAsync(buildOrder);

        // Assert
        var result = _consoleOutput.ToString();
        var jsonDocument = JsonDocument.Parse(result);
        
        var project = jsonDocument.RootElement.GetProperty("buildOrder")[0].GetProperty("projects")[0];
        project.GetProperty("filePath").GetString().Should().Be("/path/to/Core.Library.csproj");
        project.GetProperty("projectName").GetString().Should().Be("Core.Library");
        project.GetProperty("projectType").GetString().Should().Be("CSharp");
        project.GetProperty("targetFramework").GetString().Should().Be("net6.0");
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
                    CreateProjectInfo("Business.Logic.csproj")
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