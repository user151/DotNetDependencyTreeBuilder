using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;

namespace DotNetDependencyTreeBuilder.Output;

/// <summary>
/// File output wrapper that delegates to another output implementation
/// </summary>
public class FileConsoleOutput : IConsoleOutput
{
    private readonly IConsoleOutput _innerOutput;
    private readonly string _outputPath;

    public FileConsoleOutput(IConsoleOutput innerOutput, string outputPath)
    {
        _innerOutput = innerOutput ?? throw new ArgumentNullException(nameof(innerOutput));
        _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
    }

    /// <summary>
    /// Outputs the build order to the specified file using the inner output implementation
    /// </summary>
    /// <param name="buildOrder">The build order to output</param>
    /// <param name="outputPath">This parameter is ignored as the file path is set in constructor</param>
    public async Task OutputBuildOrderAsync(BuildOrder buildOrder, string? outputPath = null)
    {
        // Ensure the output directory exists
        var directory = Path.GetDirectoryName(_outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await _innerOutput.OutputBuildOrderAsync(buildOrder, _outputPath);
    }

    /// <summary>
    /// Outputs error messages using the inner output implementation
    /// </summary>
    /// <param name="message">The error message to output</param>
    public void OutputError(string message)
    {
        _innerOutput.OutputError(message);
    }

    /// <summary>
    /// Outputs informational messages using the inner output implementation
    /// </summary>
    /// <param name="message">The information message to output</param>
    public void OutputInfo(string message)
    {
        _innerOutput.OutputInfo(message);
    }
}