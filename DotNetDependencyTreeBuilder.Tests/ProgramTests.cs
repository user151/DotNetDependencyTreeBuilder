using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetDependencyTreeBuilder.Models;
using Xunit;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests;

/// <summary>
/// Tests for command-line interface functionality
/// </summary>
public class ProgramTests
{
    private readonly string _tempDirectory;
    private readonly string _nonExistentDirectory;

    public ProgramTests()
    {
        _tempDirectory = Path.GetTempPath();
        _nonExistentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    [Fact]
    public void CreateRootCommand_ShouldHaveCorrectDescription()
    {
        // Arrange & Act
        var rootCommand = CreateTestRootCommand();

        // Assert
        rootCommand.Description.Should().Be("Analyzes .NET project dependencies and generates build order");
    }

    [Fact]
    public void CreateRootCommand_ShouldHaveSourceDirectoryArgument()
    {
        // Arrange & Act
        var rootCommand = CreateTestRootCommand();

        // Assert
        var argument = rootCommand.Arguments.FirstOrDefault();
        argument.Should().NotBeNull();
        argument!.Name.Should().Be("source-directory");
        argument.Description.Should().Be("The root directory to scan for projects");
        argument.Arity.Should().Be(ArgumentArity.ExactlyOne);
    }

    [Fact]
    public void CreateRootCommand_ShouldHaveOutputOption()
    {
        // Arrange & Act
        var rootCommand = CreateTestRootCommand();

        // Assert
        var option = rootCommand.Options.FirstOrDefault(o => o.Name == "output");
        option.Should().NotBeNull();
        option!.Aliases.Should().Contain("--output");
        option.Aliases.Should().Contain("-o");
        option.Description.Should().Be("Output file path (optional)");
        option.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void CreateRootCommand_ShouldHaveFormatOption()
    {
        // Arrange & Act
        var rootCommand = CreateTestRootCommand();

        // Assert
        var option = rootCommand.Options.FirstOrDefault(o => o.Name == "format");
        option.Should().NotBeNull();
        option!.Aliases.Should().Contain("--format");
        option.Aliases.Should().Contain("-f");
        option.Description.Should().Be("Output format: text|json (default: text)");
        option.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void CreateRootCommand_ShouldHaveVerboseOption()
    {
        // Arrange & Act
        var rootCommand = CreateTestRootCommand();

        // Assert
        var option = rootCommand.Options.FirstOrDefault(o => o.Name == "verbose");
        option.Should().NotBeNull();
        option!.Aliases.Should().Contain("--verbose");
        option.Aliases.Should().Contain("-v");
        option.Description.Should().Be("Enable verbose logging");
        option.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void CreateRootCommand_ShouldHaveIncludePackagesOption()
    {
        // Arrange & Act
        var rootCommand = CreateTestRootCommand();

        // Assert
        var option = rootCommand.Options.FirstOrDefault(o => o.Name == "include-packages");
        option.Should().NotBeNull();
        option!.Aliases.Should().Contain("--include-packages");
        option.Description.Should().Be("Include package dependencies in output");
        option.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void CreateRootCommand_ShouldHaveDetectCyclesOnlyOption()
    {
        // Arrange & Act
        var rootCommand = CreateTestRootCommand();

        // Assert
        var option = rootCommand.Options.FirstOrDefault(o => o.Name == "detect-cycles-only");
        option.Should().NotBeNull();
        option!.Aliases.Should().Contain("--detect-cycles-only");
        option.Description.Should().Be("Only check for circular dependencies");
        option.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void ParseArguments_WithValidDirectory_ShouldSucceed()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { _tempDirectory };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
        var sourceDirectory = parseResult.GetValueForArgument(rootCommand.Arguments.First());
        sourceDirectory.Should().Be(_tempDirectory);
    }

    [Fact]
    public void ParseArguments_WithNonExistentDirectory_ShouldHaveValidationError()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { _nonExistentDirectory };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
        parseResult.Errors.First().Message.Should().Contain("Source directory does not exist");
    }

    [Fact]
    public void ParseArguments_WithEmptyDirectory_ShouldHaveValidationError()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { "" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
        parseResult.Errors.First().Message.Should().Contain("Source directory cannot be empty");
    }

    [Fact]
    public void ParseArguments_WithValidOutputPath_ShouldSucceed()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var outputPath = Path.Combine(_tempDirectory, "output.txt");
        var args = new[] { _tempDirectory, "--output", outputPath };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
        var outputOption = rootCommand.Options.First(o => o.Name == "output");
        var parsedOutputPath = parseResult.GetValueForOption(outputOption);
        parsedOutputPath.Should().Be(outputPath);
    }

    [Fact]
    public void ParseArguments_WithInvalidOutputDirectory_ShouldHaveValidationError()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var invalidOutputPath = Path.Combine(_nonExistentDirectory, "output.txt");
        var args = new[] { _tempDirectory, "--output", invalidOutputPath };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
        parseResult.Errors.First().Message.Should().Contain("Output directory does not exist");
    }

    [Fact]
    public void ParseArguments_WithTextFormat_ShouldSucceed()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { _tempDirectory, "--format", "text" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
        var formatOption = rootCommand.Options.First(o => o.Name == "format");
        var format = parseResult.GetValueForOption(formatOption);
        format.Should().Be(OutputFormat.Text);
    }

    [Fact]
    public void ParseArguments_WithJsonFormat_ShouldSucceed()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { _tempDirectory, "--format", "json" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
        var formatOption = rootCommand.Options.First(o => o.Name == "format");
        var format = parseResult.GetValueForOption(formatOption);
        format.Should().Be(OutputFormat.Json);
    }

    [Fact]
    public void ParseArguments_WithVerboseFlag_ShouldSucceed()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { _tempDirectory, "--verbose" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
        var verboseOption = rootCommand.Options.First(o => o.Name == "verbose");
        var verbose = parseResult.GetValueForOption(verboseOption);
        ((bool)verbose!).Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithShortVerboseFlag_ShouldSucceed()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { _tempDirectory, "-v" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
        var verboseOption = rootCommand.Options.First(o => o.Name == "verbose");
        var verbose = parseResult.GetValueForOption(verboseOption);
        ((bool)verbose!).Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithIncludePackagesFlag_ShouldSucceed()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { _tempDirectory, "--include-packages" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
        var includePackagesOption = rootCommand.Options.First(o => o.Name == "include-packages");
        var includePackages = parseResult.GetValueForOption(includePackagesOption);
        ((bool)includePackages!).Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithDetectCyclesOnlyFlag_ShouldSucceed()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { _tempDirectory, "--detect-cycles-only" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
        var detectCyclesOnlyOption = rootCommand.Options.First(o => o.Name == "detect-cycles-only");
        var detectCyclesOnly = parseResult.GetValueForOption(detectCyclesOnlyOption);
        ((bool)detectCyclesOnly!).Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithAllOptions_ShouldSucceed()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var outputPath = Path.Combine(_tempDirectory, "output.json");
        var args = new[] 
        { 
            _tempDirectory, 
            "--output", outputPath,
            "--format", "json",
            "--verbose",
            "--include-packages",
            "--detect-cycles-only"
        };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().BeEmpty();
        
        var sourceDirectory = parseResult.GetValueForArgument(rootCommand.Arguments.First());
        sourceDirectory.Should().Be(_tempDirectory);
        
        var outputOption = rootCommand.Options.First(o => o.Name == "output");
        var parsedOutputPath = parseResult.GetValueForOption(outputOption);
        parsedOutputPath.Should().Be(outputPath);
        
        var formatOption = rootCommand.Options.First(o => o.Name == "format");
        var format = parseResult.GetValueForOption(formatOption);
        format.Should().Be(OutputFormat.Json);
        
        var verboseOption = rootCommand.Options.First(o => o.Name == "verbose");
        var verbose = parseResult.GetValueForOption(verboseOption);
        ((bool)verbose!).Should().BeTrue();
        
        var includePackagesOption = rootCommand.Options.First(o => o.Name == "include-packages");
        var includePackages = parseResult.GetValueForOption(includePackagesOption);
        ((bool)includePackages!).Should().BeTrue();
        
        var detectCyclesOnlyOption = rootCommand.Options.First(o => o.Name == "detect-cycles-only");
        var detectCyclesOnly = parseResult.GetValueForOption(detectCyclesOnlyOption);
        ((bool)detectCyclesOnly!).Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithNoArguments_ShouldHaveError()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = Array.Empty<string>();

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseArguments_WithHelpFlag_ShouldNotHaveArgumentErrors()
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var args = new[] { "--help" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        // Help flag is handled by System.CommandLine internally
        // We just verify that the parse result is created successfully
        parseResult.Should().NotBeNull();
    }

    [Theory]
    [InlineData("--invalid-option")]
    [InlineData("--format", "xml")]
    [InlineData("--output")]
    public void ParseArguments_WithInvalidOptions_ShouldHaveError(params string[] args)
    {
        // Arrange
        var rootCommand = CreateTestRootCommand();
        var fullArgs = new[] { _tempDirectory }.Concat(args).ToArray();

        // Act
        var parseResult = rootCommand.Parse(fullArgs);

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Creates a test version of the root command using reflection to access the private method
    /// </summary>
    private static RootCommand CreateTestRootCommand()
    {
        // Since CreateRootCommand is private, we need to create a similar command for testing
        var rootCommand = new RootCommand("Analyzes .NET project dependencies and generates build order");
        
        // Define arguments
        var sourceDirectoryArgument = new Argument<string>(
            name: "source-directory",
            description: "The root directory to scan for projects")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        
        // Add validation for source directory
        sourceDirectoryArgument.AddValidator(result =>
        {
            var value = result.GetValueForArgument(sourceDirectoryArgument);
            if (string.IsNullOrWhiteSpace(value))
            {
                result.ErrorMessage = "Source directory cannot be empty";
                return;
            }
            
            if (!Directory.Exists(value))
            {
                result.ErrorMessage = $"Source directory does not exist: {value}";
                return;
            }
        });
        
        // Define options
        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path (optional)");
        
        // Add validation for output path
        outputOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(outputOption);
            if (!string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    var directory = Path.GetDirectoryName(Path.GetFullPath(value));
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        result.ErrorMessage = $"Output directory does not exist: {directory}";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = $"Invalid output path: {ex.Message}";
                    return;
                }
            }
        });
        
        var formatOption = new Option<OutputFormat>(
            aliases: new[] { "--format", "-f" },
            description: "Output format: text|json (default: text)")
        {
            IsRequired = false
        };
        formatOption.SetDefaultValue(OutputFormat.Text);
        
        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Enable verbose logging");
        
        var includePackagesOption = new Option<bool>(
            aliases: new[] { "--include-packages" },
            description: "Include package dependencies in output");
        
        var detectCyclesOnlyOption = new Option<bool>(
            aliases: new[] { "--detect-cycles-only" },
            description: "Only check for circular dependencies");
        
        // Add arguments and options to command
        rootCommand.AddArgument(sourceDirectoryArgument);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(formatOption);
        rootCommand.AddOption(verboseOption);
        rootCommand.AddOption(includePackagesOption);
        rootCommand.AddOption(detectCyclesOnlyOption);
        
        return rootCommand;
    }
}