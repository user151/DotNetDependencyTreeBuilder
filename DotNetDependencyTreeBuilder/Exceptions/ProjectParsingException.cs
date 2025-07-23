namespace DotNetDependencyTreeBuilder.Exceptions;

/// <summary>
/// Exception thrown when a project file cannot be parsed
/// </summary>
public class ProjectParsingException : Exception
{
    /// <summary>
    /// Path to the project file that failed to parse
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    /// Initializes a new instance of the ProjectParsingException class
    /// </summary>
    /// <param name="projectPath">Path to the project file that failed to parse</param>
    /// <param name="message">Error message</param>
    public ProjectParsingException(string projectPath, string message) 
        : base(message)
    {
        ProjectPath = projectPath;
    }

    /// <summary>
    /// Initializes a new instance of the ProjectParsingException class
    /// </summary>
    /// <param name="projectPath">Path to the project file that failed to parse</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    public ProjectParsingException(string projectPath, string message, Exception innerException) 
        : base(message, innerException)
    {
        ProjectPath = projectPath;
    }
}