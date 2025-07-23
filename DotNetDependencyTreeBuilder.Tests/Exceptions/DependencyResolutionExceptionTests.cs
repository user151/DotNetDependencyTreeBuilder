using DotNetDependencyTreeBuilder.Exceptions;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Exceptions;

public class DependencyResolutionExceptionTests
{
    [Fact]
    public void Constructor_WithSourceAndTargetPaths_SetsPropertiesCorrectly()
    {
        // Arrange
        var sourceProjectPath = @"C:\Projects\App\App.csproj";
        var targetDependencyPath = @"C:\Projects\Lib\Lib.csproj";
        var message = "Dependency resolution failed";

        // Act
        var exception = new DependencyResolutionException(sourceProjectPath, targetDependencyPath, message);

        // Assert
        exception.SourceProjectPath.Should().Be(sourceProjectPath);
        exception.TargetDependencyPath.Should().Be(targetDependencyPath);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithSourceTargetPathsAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var sourceProjectPath = @"C:\Projects\App\App.csproj";
        var targetDependencyPath = @"C:\Projects\Lib\Lib.csproj";
        var message = "Dependency resolution failed";
        var innerException = new FileNotFoundException("Target not found");

        // Act
        var exception = new DependencyResolutionException(sourceProjectPath, targetDependencyPath, message, innerException);

        // Assert
        exception.SourceProjectPath.Should().Be(sourceProjectPath);
        exception.TargetDependencyPath.Should().Be(targetDependencyPath);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void DependencyResolutionException_InheritsFromException()
    {
        // Arrange
        var sourceProjectPath = @"C:\Projects\App\App.csproj";
        var targetDependencyPath = @"C:\Projects\Lib\Lib.csproj";
        var message = "Dependency resolution failed";

        // Act
        var exception = new DependencyResolutionException(sourceProjectPath, targetDependencyPath, message);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}