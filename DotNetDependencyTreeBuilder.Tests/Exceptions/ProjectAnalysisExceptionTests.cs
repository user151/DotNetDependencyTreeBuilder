using DotNetDependencyTreeBuilder.Exceptions;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Exceptions;

public class ProjectAnalysisExceptionTests
{
    [Fact]
    public void Constructor_WithProjectPathAndMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var projectPath = @"C:\Projects\App\App.csproj";
        var message = "Analysis failed";

        // Act
        var exception = new ProjectAnalysisException(projectPath, message);

        // Assert
        exception.ProjectPath.Should().Be(projectPath);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithProjectPathMessageAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var projectPath = @"C:\Projects\App\App.csproj";
        var message = "Analysis failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new ProjectAnalysisException(projectPath, message, innerException);

        // Assert
        exception.ProjectPath.Should().Be(projectPath);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void ProjectAnalysisException_InheritsFromException()
    {
        // Arrange
        var projectPath = @"C:\Projects\App\App.csproj";
        var message = "Analysis failed";

        // Act
        var exception = new ProjectAnalysisException(projectPath, message);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}