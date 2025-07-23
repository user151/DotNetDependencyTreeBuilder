using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;

namespace DotNetDependencyTreeBuilder.Output;

/// <summary>
/// Composite output implementation that can combine multiple output implementations
/// </summary>
public class CompositeConsoleOutput : IConsoleOutput
{
    private readonly IList<IConsoleOutput> _outputs;

    public CompositeConsoleOutput(params IConsoleOutput[] outputs)
    {
        _outputs = outputs?.ToList() ?? throw new ArgumentNullException(nameof(outputs));
        
        if (_outputs.Count == 0)
        {
            throw new ArgumentException("At least one output implementation must be provided", nameof(outputs));
        }
    }

    /// <summary>
    /// Outputs the build order using all configured output implementations
    /// </summary>
    /// <param name="buildOrder">The build order to output</param>
    /// <param name="outputPath">Optional file path for output</param>
    public async Task OutputBuildOrderAsync(BuildOrder buildOrder, string? outputPath = null)
    {
        var tasks = _outputs.Select(output => output.OutputBuildOrderAsync(buildOrder, outputPath));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Outputs error messages using all configured output implementations
    /// </summary>
    /// <param name="message">The error message to output</param>
    public void OutputError(string message)
    {
        foreach (var output in _outputs)
        {
            output.OutputError(message);
        }
    }

    /// <summary>
    /// Outputs informational messages using all configured output implementations
    /// </summary>
    /// <param name="message">The information message to output</param>
    public void OutputInfo(string message)
    {
        foreach (var output in _outputs)
        {
            output.OutputInfo(message);
        }
    }
}