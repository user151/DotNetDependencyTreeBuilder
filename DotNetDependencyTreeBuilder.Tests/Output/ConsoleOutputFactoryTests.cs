using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Output;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Output;

public class ConsoleOutputFactoryTests
{
    [Fact]
    public void Create_WithTextFormat_ShouldReturnTextConsoleOutput()
    {
        // Act
        var result = ConsoleOutputFactory.Create(OutputFormat.Text);

        // Assert
        result.Should().BeOfType<TextConsoleOutput>();
    }

    [Fact]
    public void Create_WithJsonFormat_ShouldReturnJsonConsoleOutput()
    {
        // Act
        var result = ConsoleOutputFactory.Create(OutputFormat.Json);

        // Assert
        result.Should().BeOfType<JsonConsoleOutput>();
    }

    [Fact]
    public void Create_WithTextFormatAndOutputPath_ShouldReturnFileConsoleOutput()
    {
        // Act
        var result = ConsoleOutputFactory.Create(OutputFormat.Text, "/path/to/output.txt");

        // Assert
        result.Should().BeOfType<FileConsoleOutput>();
    }

    [Fact]
    public void Create_WithJsonFormatAndOutputPath_ShouldReturnFileConsoleOutput()
    {
        // Act
        var result = ConsoleOutputFactory.Create(OutputFormat.Json, "/path/to/output.json");

        // Assert
        result.Should().BeOfType<FileConsoleOutput>();
    }

    [Fact]
    public void Create_WithInvalidFormat_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => ConsoleOutputFactory.Create((OutputFormat)999);
        action.Should().Throw<ArgumentException>().WithParameterName("format");
    }

    [Fact]
    public void CreateConsoleAndFile_WithTextFormat_ShouldReturnCompositeConsoleOutput()
    {
        // Act
        var result = ConsoleOutputFactory.CreateConsoleAndFile(OutputFormat.Text, "/path/to/output.txt");

        // Assert
        result.Should().BeOfType<CompositeConsoleOutput>();
    }

    [Fact]
    public void CreateConsoleAndFile_WithJsonFormat_ShouldReturnCompositeConsoleOutput()
    {
        // Act
        var result = ConsoleOutputFactory.CreateConsoleAndFile(OutputFormat.Json, "/path/to/output.json");

        // Assert
        result.Should().BeOfType<CompositeConsoleOutput>();
    }

    [Fact]
    public void CreateConsoleAndFile_WithNullOutputPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => ConsoleOutputFactory.CreateConsoleAndFile(OutputFormat.Text, null!);
        action.Should().Throw<ArgumentException>().WithParameterName("outputPath");
    }

    [Fact]
    public void CreateConsoleAndFile_WithEmptyOutputPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => ConsoleOutputFactory.CreateConsoleAndFile(OutputFormat.Text, "");
        action.Should().Throw<ArgumentException>().WithParameterName("outputPath");
    }

    [Fact]
    public void CreateConsoleAndFile_WithInvalidFormat_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => ConsoleOutputFactory.CreateConsoleAndFile((OutputFormat)999, "/path/to/output.txt");
        action.Should().Throw<ArgumentException>().WithParameterName("format");
    }

    [Fact]
    public void Create_WithEmptyOutputPath_ShouldReturnBaseOutput()
    {
        // Act
        var result = ConsoleOutputFactory.Create(OutputFormat.Text, "");

        // Assert
        result.Should().BeOfType<TextConsoleOutput>();
    }

    [Fact]
    public void Create_WithWhitespaceOutputPath_ShouldReturnBaseOutput()
    {
        // Act
        var result = ConsoleOutputFactory.Create(OutputFormat.Text, "   ");

        // Assert
        result.Should().BeOfType<TextConsoleOutput>();
    }
}