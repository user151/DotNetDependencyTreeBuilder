using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;

namespace DotNetDependencyTreeBuilder.Output;

/// <summary>
/// Factory for creating console output implementations
/// </summary>
public static class ConsoleOutputFactory
{
    /// <summary>
    /// Creates a console output implementation based on the specified format and output path
    /// </summary>
    /// <param name="format">The output format to use</param>
    /// <param name="outputPath">Optional file path for output</param>
    /// <returns>Configured console output implementation</returns>
    public static IConsoleOutput Create(OutputFormat format, string? outputPath = null)
    {
        IConsoleOutput baseOutput = format switch
        {
            OutputFormat.Text => new TextConsoleOutput(),
            OutputFormat.Json => new JsonConsoleOutput(),
            _ => throw new ArgumentException($"Unsupported output format: {format}", nameof(format))
        };

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            return new FileConsoleOutput(baseOutput, outputPath);
        }

        return baseOutput;
    }

    /// <summary>
    /// Creates a composite output that outputs to both console and file
    /// </summary>
    /// <param name="format">The output format to use</param>
    /// <param name="outputPath">File path for output</param>
    /// <returns>Composite console output implementation</returns>
    public static IConsoleOutput CreateConsoleAndFile(OutputFormat format, string outputPath)
    {
        if (string.IsNullOrEmpty(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        }

        IConsoleOutput consoleOutput = format switch
        {
            OutputFormat.Text => new TextConsoleOutput(),
            OutputFormat.Json => new JsonConsoleOutput(),
            _ => throw new ArgumentException($"Unsupported output format: {format}", nameof(format))
        };

        var fileOutput = new FileConsoleOutput(consoleOutput, outputPath);
        
        return new CompositeConsoleOutput(consoleOutput, fileOutput);
    }
}