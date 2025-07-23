namespace DotNetDependencyTreeBuilder.Models;

/// <summary>
/// Statistics collected during project discovery
/// </summary>
public class DiscoveryStatistics
{
    /// <summary>
    /// Number of directories successfully scanned
    /// </summary>
    public int DirectoriesScanned { get; set; }

    /// <summary>
    /// Number of directories skipped due to access restrictions or other issues
    /// </summary>
    public int DirectoriesSkipped { get; set; }

    /// <summary>
    /// Number of errors encountered during discovery
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Number of C# projects found
    /// </summary>
    public int CSharpProjectsFound { get; set; }

    /// <summary>
    /// Number of VB.NET projects found
    /// </summary>
    public int VBProjectsFound { get; set; }

    /// <summary>
    /// Total number of projects found
    /// </summary>
    public int TotalProjectsFound => CSharpProjectsFound + VBProjectsFound;
}