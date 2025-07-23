namespace DotNetDependencyTreeBuilder.Exceptions;

/// <summary>
/// Exception thrown when circular dependencies are detected
/// </summary>
public class CircularDependencyException : Exception
{
    /// <summary>
    /// List of projects involved in the circular dependency
    /// </summary>
    public IReadOnlyList<string> CircularProjects { get; }

    /// <summary>
    /// Initializes a new instance of the CircularDependencyException class
    /// </summary>
    /// <param name="circularProjects">List of projects involved in the circular dependency</param>
    /// <param name="message">Error message</param>
    public CircularDependencyException(IEnumerable<string> circularProjects, string message) 
        : base(message)
    {
        CircularProjects = circularProjects?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the CircularDependencyException class
    /// </summary>
    /// <param name="circularProjects">List of projects involved in the circular dependency</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    public CircularDependencyException(IEnumerable<string> circularProjects, string message, Exception innerException) 
        : base(message, innerException)
    {
        CircularProjects = circularProjects?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }
}