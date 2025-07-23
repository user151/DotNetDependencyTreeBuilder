namespace DotNetDependencyTreeBuilder.Exceptions;

/// <summary>
/// Exception thrown when project discovery fails
/// </summary>
public class ProjectDiscoveryException : Exception
{
    /// <summary>
    /// Directory path where discovery failed
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// Initializes a new instance of the ProjectDiscoveryException class
    /// </summary>
    /// <param name="directoryPath">Directory path where discovery failed</param>
    /// <param name="message">Error message</param>
    public ProjectDiscoveryException(string directoryPath, string message) 
        : base(message)
    {
        DirectoryPath = directoryPath;
    }

    /// <summary>
    /// Initializes a new instance of the ProjectDiscoveryException class
    /// </summary>
    /// <param name="directoryPath">Directory path where discovery failed</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    public ProjectDiscoveryException(string directoryPath, string message, Exception innerException) 
        : base(message, innerException)
    {
        DirectoryPath = directoryPath;
    }
}