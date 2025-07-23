using DotNetDependencyTreeBuilder.Exceptions;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Exceptions;

public class BuildOrderExceptionTests
{
    [Fact]
    public void Constructor_WithProjectCountAndMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var projectCount = 5;
        var message = "Build order generation failed";

        // Act
        var exception = new BuildOrderException(projectCount, message);

        // Assert
        exception.ProjectCount.Should().Be(projectCount);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithProjectCountMessageAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var projectCount = 10;
        var message = "Build order generation failed";
        var innerException = new InvalidOperationException("Topological sort failed");

        // Act
        var exception = new BuildOrderException(projectCount, message, innerException);

        // Assert
        exception.ProjectCount.Should().Be(projectCount);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithZeroProjectCount_SetsPropertyCorrectly()
    {
        // Arrange
        var projectCount = 0;
        var message = "No projects to process";

        // Act
        var exception = new BuildOrderException(projectCount, message);

        // Assert
        exception.ProjectCount.Should().Be(0);
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void BuildOrderException_InheritsFromException()
    {
        // Arrange
        var projectCount = 3;
        var message = "Build order generation failed";

        // Act
        var exception = new BuildOrderException(projectCount, message);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}