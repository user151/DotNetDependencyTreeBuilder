using DotNetDependencyTreeBuilder.Exceptions;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Exceptions;

public class CircularDependencyExceptionTests
{
    [Fact]
    public void Constructor_WithCircularProjects_SetsPropertiesCorrectly()
    {
        // Arrange
        var circularProjects = new List<string> 
        { 
            @"C:\Projects\App\App.csproj", 
            @"C:\Projects\Lib\Lib.csproj" 
        };
        var message = "Circular dependency detected";

        // Act
        var exception = new CircularDependencyException(circularProjects, message);

        // Assert
        exception.CircularProjects.Should().BeEquivalentTo(circularProjects);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCircularProjectsAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var circularProjects = new List<string> 
        { 
            @"C:\Projects\App\App.csproj", 
            @"C:\Projects\Lib\Lib.csproj" 
        };
        var message = "Circular dependency detected";
        var innerException = new InvalidOperationException("Graph analysis failed");

        // Act
        var exception = new CircularDependencyException(circularProjects, message, innerException);

        // Assert
        exception.CircularProjects.Should().BeEquivalentTo(circularProjects);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithNullCircularProjects_CreatesEmptyList()
    {
        // Arrange
        var message = "Circular dependency detected";

        // Act
        var exception = new CircularDependencyException(null!, message);

        // Assert
        exception.CircularProjects.Should().BeEmpty();
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void CircularProjects_IsReadOnly()
    {
        // Arrange
        var circularProjects = new List<string> 
        { 
            @"C:\Projects\App\App.csproj", 
            @"C:\Projects\Lib\Lib.csproj" 
        };
        var message = "Circular dependency detected";

        // Act
        var exception = new CircularDependencyException(circularProjects, message);

        // Assert
        exception.CircularProjects.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void CircularDependencyException_InheritsFromException()
    {
        // Arrange
        var circularProjects = new List<string> 
        { 
            @"C:\Projects\App\App.csproj" 
        };
        var message = "Circular dependency detected";

        // Act
        var exception = new CircularDependencyException(circularProjects, message);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}