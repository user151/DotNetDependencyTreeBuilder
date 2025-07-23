namespace DotNetDependencyTreeBuilder.Exceptions;

/// <summary>
/// Exception thrown when project dependency analysis fails
/// </summary>
public class ProjectAnalysisException : Exception
{
    /// <summary>
    /// Path to the project file that failed analysis
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    /// Initializes a new instance of the ProjectAnalysisException class
    /// </summary>
    /// <param name="projectPath">Path to the project file that failed analysis</param>
    /// <param name="message">Error message</param>
    public ProjectAnalysisException(string projectPath, string message) 
        : base(message)
    {
        ProjectPath = projectPath;
    }

    /// <summary>
    /// Initializes a new instance of the ProjectAnalysisException class
    /// </summary>
    /// <param name="projectPath">Path to the project file that failed analysis</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    public ProjectAnalysisException(string projectPath, string message, Exception innerException) 
        : base(message, innerException)
    {
        ProjectPath = projectPath;
    }
}