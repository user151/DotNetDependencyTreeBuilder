using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetDependencyTreeBuilder.Output;

/// <summary>
/// JSON format console output implementation
/// </summary>
public class JsonConsoleOutput : IConsoleOutput
{
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonConsoleOutput()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Outputs the build order in JSON format to console or file
    /// </summary>
    /// <param name="buildOrder">The build order to output</param>
    /// <param name="outputPath">Optional file path for output</param>
    public async Task OutputBuildOrderAsync(BuildOrder buildOrder, string? outputPath = null)
    {
        var jsonOutput = CreateJsonOutput(buildOrder);
        var json = JsonSerializer.Serialize(jsonOutput, _jsonOptions);
        
        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine(json);
        }
        else
        {
            await File.WriteAllTextAsync(outputPath, json);
        }
    }

    /// <summary>
    /// Outputs error messages in JSON format
    /// </summary>
    /// <param name="message">The error message to output</param>
    public void OutputError(string message)
    {
        var errorOutput = new { error = message, timestamp = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(errorOutput, _jsonOptions);
        Console.Error.WriteLine(json);
    }

    /// <summary>
    /// Outputs informational messages in JSON format
    /// </summary>
    /// <param name="message">The information message to output</param>
    public void OutputInfo(string message)
    {
        var infoOutput = new { info = message, timestamp = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(infoOutput, _jsonOptions);
        Console.WriteLine(json);
    }

    /// <summary>
    /// Creates a JSON-serializable representation of the build order
    /// </summary>
    /// <param name="buildOrder">The build order to convert</param>
    /// <returns>JSON-serializable object</returns>
    private object CreateJsonOutput(BuildOrder buildOrder)
    {
        return new
        {
            Summary = new
            {
                ProjectsFound = buildOrder.TotalProjects,
                BuildLevels = buildOrder.TotalLevels,
                CircularDependencies = buildOrder.CircularDependencies.ToArray(),
                HasCircularDependencies = buildOrder.HasCircularDependencies
            },
            BuildOrder = buildOrder.BuildLevels.Select((level, index) => new
            {
                Level = index + 1,
                Projects = level.Select(p => new
                {
                    FilePath = p.FilePath,
                    ProjectName = p.ProjectName,
                    ProjectType = p.Type.ToString(),
                    TargetFramework = p.TargetFramework
                }).ToArray()
            }).ToArray()
        };
    }
}