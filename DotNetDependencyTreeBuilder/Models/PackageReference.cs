namespace DotNetDependencyTreeBuilder.Models;

/// <summary>
/// Represents a NuGet package reference
/// </summary>
public class PackageReference
{
    /// <summary>
    /// Name of the NuGet package
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// Version of the NuGet package
    /// </summary>
    public string Version { get; set; } = string.Empty;
}