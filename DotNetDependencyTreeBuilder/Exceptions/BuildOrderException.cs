namespace DotNetDependencyTreeBuilder.Exceptions;

/// <summary>
/// Exception thrown when build order generation fails
/// </summary>
public class BuildOrderException : Exception
{
    /// <summary>
    /// Number of projects that were being processed when the error occurred
    /// </summary>
    public int ProjectCount { get; }

    /// <summary>
    /// Initializes a new instance of the BuildOrderException class
    /// </summary>
    /// <param name="projectCount">Number of projects that were being processed</param>
    /// <param name="message">Error message</param>
    public BuildOrderException(int projectCount, string message) 
        : base(message)
    {
        ProjectCount = projectCount;
    }

    /// <summary>
    /// Initializes a new instance of the BuildOrderException class
    /// </summary>
    /// <param name="projectCount">Number of projects that were being processed</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    public BuildOrderException(int projectCount, string message, Exception innerException) 
        : base(message, innerException)
    {
        ProjectCount = projectCount;
    }
}