using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using System.Text;

namespace DotNetDependencyTreeBuilder.Output;

/// <summary>
/// Text format console output implementation
/// </summary>
public class TextConsoleOutput : IConsoleOutput
{
    /// <summary>
    /// Outputs the build order in text format to console or file
    /// </summary>
    /// <param name="buildOrder">The build order to output</param>
    /// <param name="outputPath">Optional file path for output</param>
    public async Task OutputBuildOrderAsync(BuildOrder buildOrder, string? outputPath = null)
    {
        var output = FormatBuildOrder(buildOrder);
        
        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine(output);
        }
        else
        {
            await File.WriteAllTextAsync(outputPath, output);
        }
    }

    /// <summary>
    /// Outputs error messages to console
    /// </summary>
    /// <param name="message">The error message to output</param>
    public void OutputError(string message)
    {
        Console.Error.WriteLine($"ERROR: {message}");
    }

    /// <summary>
    /// Outputs informational messages to console
    /// </summary>
    /// <param name="message">The information message to output</param>
    public void OutputInfo(string message)
    {
        Console.WriteLine($"INFO: {message}");
    }

    /// <summary>
    /// Formats the build order as text
    /// </summary>
    /// <param name="buildOrder">The build order to format</param>
    /// <returns>Formatted text representation</returns>
    private string FormatBuildOrder(BuildOrder buildOrder)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("Build Order Analysis Results");
        sb.AppendLine("============================");
        sb.AppendLine();
        
        // Summary information
        sb.AppendLine($"Projects Found: {buildOrder.TotalProjects}");
        sb.AppendLine($"Build Levels: {buildOrder.TotalLevels}");
        
        if (buildOrder.HasCircularDependencies)
        {
            sb.AppendLine($"Circular Dependencies: {buildOrder.CircularDependencies.Count}");
            sb.AppendLine();
            sb.AppendLine("CIRCULAR DEPENDENCIES DETECTED:");
            foreach (var dependency in buildOrder.CircularDependencies)
            {
                sb.AppendLine($"  - {dependency}");
            }
        }
        else
        {
            sb.AppendLine("Circular Dependencies: None");
        }
        
        sb.AppendLine();
        sb.AppendLine("Build Order:");
        
        // Flatten all projects from all levels into a single list
        var allProjects = buildOrder.BuildLevels.SelectMany(level => level).ToList();
        
        foreach (var project in allProjects)
        {
            sb.AppendLine($"  - {project.FilePath}");
        }
        
        return sb.ToString();
    }
}