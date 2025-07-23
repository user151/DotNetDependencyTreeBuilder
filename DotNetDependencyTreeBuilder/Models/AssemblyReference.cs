namespace DotNetDependencyTreeBuilder.Models;

/// <summary>
/// Represents a direct assembly reference (Reference node in project files)
/// </summary>
public class AssemblyReference
{
    /// <summary>
    /// Full assembly name including version and culture information
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// Path hint to the assembly DLL file
    /// </summary>
    public string HintPath { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether specific version matching is required
    /// </summary>
    public bool SpecificVersion { get; set; } = true;

    /// <summary>
    /// Version information extracted from the assembly name
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Culture information extracted from the assembly name
    /// </summary>
    public string Culture { get; set; } = string.Empty;

    /// <summary>
    /// Processor architecture extracted from the assembly name
    /// </summary>
    public string ProcessorArchitecture { get; set; } = string.Empty;
}