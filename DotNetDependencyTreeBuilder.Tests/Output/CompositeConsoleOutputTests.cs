using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Output;
using FluentAssertions;
using Moq;

namespace DotNetDependencyTreeBuilder.Tests.Output;

public class CompositeConsoleOutputTests
{
    private readonly Mock<IConsoleOutput> _mockOutput1;
    private readonly Mock<IConsoleOutput> _mockOutput2;
    private readonly CompositeConsoleOutput _compositeOutput;

    public CompositeConsoleOutputTests()
    {
        _mockOutput1 = new Mock<IConsoleOutput>();
        _mockOutput2 = new Mock<IConsoleOutput>();
        _compositeOutput = new CompositeConsoleOutput(_mockOutput1.Object, _mockOutput2.Object);
    }

    [Fact]
    public void Constructor_WithNullOutputs_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CompositeConsoleOutput(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("outputs");
    }

    [Fact]
    public void Constructor_WithEmptyOutputs_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new CompositeConsoleOutput();
        action.Should().Throw<ArgumentException>().WithParameterName("outputs");
    }

    [Fact]
    public async Task OutputBuildOrderAsync_ShouldCallAllOutputs()
    {
        // Arrange
        var buildOrder = new BuildOrder();
        var outputPath = "/path/to/output";

        // Act
        await _compositeOutput.OutputBuildOrderAsync(buildOrder, outputPath);

        // Assert
        _mockOutput1.Verify(x => x.OutputBuildOrderAsync(buildOrder, outputPath), Times.Once);
        _mockOutput2.Verify(x => x.OutputBuildOrderAsync(buildOrder, outputPath), Times.Once);
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WithNullOutputPath_ShouldCallAllOutputs()
    {
        // Arrange
        var buildOrder = new BuildOrder();

        // Act
        await _compositeOutput.OutputBuildOrderAsync(buildOrder, null);

        // Assert
        _mockOutput1.Verify(x => x.OutputBuildOrderAsync(buildOrder, null), Times.Once);
        _mockOutput2.Verify(x => x.OutputBuildOrderAsync(buildOrder, null), Times.Once);
    }

    [Fact]
    public void OutputError_ShouldCallAllOutputs()
    {
        // Arrange
        var errorMessage = "Test error";

        // Act
        _compositeOutput.OutputError(errorMessage);

        // Assert
        _mockOutput1.Verify(x => x.OutputError(errorMessage), Times.Once);
        _mockOutput2.Verify(x => x.OutputError(errorMessage), Times.Once);
    }

    [Fact]
    public void OutputInfo_ShouldCallAllOutputs()
    {
        // Arrange
        var infoMessage = "Test info";

        // Act
        _compositeOutput.OutputInfo(infoMessage);

        // Assert
        _mockOutput1.Verify(x => x.OutputInfo(infoMessage), Times.Once);
        _mockOutput2.Verify(x => x.OutputInfo(infoMessage), Times.Once);
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WhenOneOutputThrows_ShouldStillCallOthers()
    {
        // Arrange
        var buildOrder = new BuildOrder();
        _mockOutput1.Setup(x => x.OutputBuildOrderAsync(It.IsAny<BuildOrder>(), It.IsAny<string>()))
                   .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        var action = async () => await _compositeOutput.OutputBuildOrderAsync(buildOrder);
        await action.Should().ThrowAsync<InvalidOperationException>();

        // Verify both outputs were called (even though one threw)
        _mockOutput1.Verify(x => x.OutputBuildOrderAsync(buildOrder, null), Times.Once);
        _mockOutput2.Verify(x => x.OutputBuildOrderAsync(buildOrder, null), Times.Once);
    }

    [Fact]
    public void Constructor_WithSingleOutput_ShouldWork()
    {
        // Arrange & Act
        var singleOutput = new CompositeConsoleOutput(_mockOutput1.Object);

        // Assert
        singleOutput.Should().NotBeNull();
    }

    [Fact]
    public async Task OutputBuildOrderAsync_WithSingleOutput_ShouldCallThatOutput()
    {
        // Arrange
        var singleOutput = new CompositeConsoleOutput(_mockOutput1.Object);
        var buildOrder = new BuildOrder();

        // Act
        await singleOutput.OutputBuildOrderAsync(buildOrder);

        // Assert
        _mockOutput1.Verify(x => x.OutputBuildOrderAsync(buildOrder, null), Times.Once);
        _mockOutput2.Verify(x => x.OutputBuildOrderAsync(It.IsAny<BuildOrder>(), It.IsAny<string>()), Times.Never);
    }
}