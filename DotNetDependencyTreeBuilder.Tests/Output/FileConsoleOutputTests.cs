using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Output;
using FluentAssertions;
using Moq;

namespace DotNetDependencyTreeBuilder.Tests.Output;

public class FileConsoleOutputTests
{
    private readonly Mock<IConsoleOutput> _mockInnerOutput;
    private readonly string _tempDirectory;
    private readonly string _outputPath;
    private readonly FileConsoleOutput _fileOutput;

    public FileConsoleOutputTests()
    {
        _mockInnerOutput = new Mock<IConsoleOutput>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _outputPath = Path.Combine(_tempDirectory, "output.txt");
        _fileOutput = new FileConsoleOutput(_mockInnerOutput.Object, _outputPath);
    }

    [Fact]
    public void Constructor_WithNullInnerOutput_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FileConsoleOutput(null!, "/path/to/file");
        action.Should().Throw<ArgumentNullException>().WithParameterName("innerOutput");
    }

    [Fact]
    public void Constructor_WithNullOutputPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FileConsoleOutput(_mockInnerOutput.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("outputPath");
    }

    [Fact]
    public async Task OutputBuildOrderAsync_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange
        var buildOrder = new BuildOrder();
        Directory.Exists(_tempDirectory).Should().BeFalse();

        // Act
        await _fileOutput.OutputBuildOrderAsync(buildOrder);

        // Assert
        Directory.Exists(_tempDirectory).Should().BeTrue();
        _mockInnerOutput.Verify(x => x.OutputBuildOrderAsync(buildOrder, _outputPath), Times.Once);
        
        // Cleanup
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [Fact]
    public async Task OutputBuildOrderAsync_ShouldCallInnerOutputWithCorrectPath()
    {
        // Arrange
        var buildOrder = new BuildOrder();

        // Act
        await _fileOutput.OutputBuildOrderAsync(buildOrder, "ignored-path");

        // Assert
        _mockInnerOutput.Verify(x => x.OutputBuildOrderAsync(buildOrder, _outputPath), Times.Once);
        
        // Cleanup
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [Fact]
    public void OutputError_ShouldDelegateToInnerOutput()
    {
        // Arrange
        var errorMessage = "Test error";

        // Act
        _fileOutput.OutputError(errorMessage);

        // Assert
        _mockInnerOutput.Verify(x => x.OutputError(errorMessage), Times.Once);
    }

    [Fact]
    public void OutputInfo_ShouldDelegateToInnerOutput()
    {
        // Arrange
        var infoMessage = "Test info";

        // Act
        _fileOutput.OutputInfo(infoMessage);

        // Assert
        _mockInnerOutput.Verify(x => x.OutputInfo(infoMessage), Times.Once);
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WithExistingDirectory_ShouldNotThrow()
    {
        // Arrange
        var buildOrder = new BuildOrder();
        Directory.CreateDirectory(_tempDirectory);

        try
        {
            // Act
            var action = async () => await _fileOutput.OutputBuildOrderAsync(buildOrder);

            // Assert
            await action.Should().NotThrowAsync();
            _mockInnerOutput.Verify(x => x.OutputBuildOrderAsync(buildOrder, _outputPath), Times.Once);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WithFileInRootDirectory_ShouldWork()
    {
        // Arrange
        var buildOrder = new BuildOrder();
        var rootFileOutput = new FileConsoleOutput(_mockInnerOutput.Object, "output.txt");

        // Act
        await rootFileOutput.OutputBuildOrderAsync(buildOrder);

        // Assert
        _mockInnerOutput.Verify(x => x.OutputBuildOrderAsync(buildOrder, "output.txt"), Times.Once);
    }
}