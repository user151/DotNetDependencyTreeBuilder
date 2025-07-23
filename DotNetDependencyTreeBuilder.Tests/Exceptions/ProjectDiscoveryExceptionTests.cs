using DotNetDependencyTreeBuilder.Exceptions;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Exceptions;

public class ProjectDiscoveryExceptionTests
{
    [Fact]
    public void Constructor_WithDirectoryPathAndMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var directoryPath = @"C:\Projects\Source";
        var message = "Discovery failed";

        // Act
        var exception = new ProjectDiscoveryException(directoryPath, message);

        // Assert
        exception.DirectoryPath.Should().Be(directoryPath);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDirectoryPathMessageAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var directoryPath = @"C:\Projects\Source";
        var message = "Discovery failed";
        var innerException = new UnauthorizedAccessException("Access denied");

        // Act
        var exception = new ProjectDiscoveryException(directoryPath, message, innerException);

        // Assert
        exception.DirectoryPath.Should().Be(directoryPath);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void ProjectDiscoveryException_InheritsFromException()
    {
        // Arrange
        var directoryPath = @"C:\Projects\Source";
        var message = "Discovery failed";

        // Act
        var exception = new ProjectDiscoveryException(directoryPath, message);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}