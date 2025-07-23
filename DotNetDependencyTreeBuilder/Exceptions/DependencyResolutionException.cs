namespace DotNetDependencyTreeBuilder.Exceptions;

/// <summary>
/// Exception thrown when dependency resolution fails
/// </summary>
public class DependencyResolutionException : Exception
{
    /// <summary>
    /// Source project path that contains the unresolved dependency
    /// </summary>
    public string SourceProjectPath { get; }

    /// <summary>
    /// Target dependency path that could not be resolved
    /// </summary>
    public string TargetDependencyPath { get; }

    /// <summary>
    /// Initializes a new instance of the DependencyResolutionException class
    /// </summary>
    /// <param name="sourceProjectPath">Source project path that contains the unresolved dependency</param>
    /// <param name="targetDependencyPath">Target dependency path that could not be resolved</param>
    /// <param name="message">Error message</param>
    public DependencyResolutionException(string sourceProjectPath, string targetDependencyPath, string message) 
        : base(message)
    {
        SourceProjectPath = sourceProjectPath;
        TargetDependencyPath = targetDependencyPath;
    }

    /// <summary>
    /// Initializes a new instance of the DependencyResolutionException class
    /// </summary>
    /// <param name="sourceProjectPath">Source project path that contains the unresolved dependency</param>
    /// <param name="targetDependencyPath">Target dependency path that could not be resolved</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    public DependencyResolutionException(string sourceProjectPath, string targetDependencyPath, string message, Exception innerException) 
        : base(message, innerException)
    {
        SourceProjectPath = sourceProjectPath;
        TargetDependencyPath = targetDependencyPath;
    }
}