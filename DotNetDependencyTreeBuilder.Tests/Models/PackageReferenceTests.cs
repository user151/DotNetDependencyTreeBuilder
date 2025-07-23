using DotNetDependencyTreeBuilder.Models;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Models;

public class PackageReferenceTests
{
    [Fact]
    public void PackageReference_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var packageReference = new PackageReference();

        // Assert
        packageReference.PackageName.Should().BeEmpty();
        packageReference.Version.Should().BeEmpty();
    }

    [Fact]
    public void PackageReference_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var packageReference = new PackageReference();

        // Act
        packageReference.PackageName = "Newtonsoft.Json";
        packageReference.Version = "13.0.1";

        // Assert
        packageReference.PackageName.Should().Be("Newtonsoft.Json");
        packageReference.Version.Should().Be("13.0.1");
    }

    [Theory]
    [InlineData("Microsoft.Extensions.Logging", "6.0.0")]
    [InlineData("System.CommandLine", "2.0.0-beta4.22272.1")]
    [InlineData("xunit", "2.4.1")]
    [InlineData("FluentAssertions", "6.8.0")]
    public void PackageReference_ShouldHandleVariousPackages(string packageName, string version)
    {
        // Arrange & Act
        var packageReference = new PackageReference
        {
            PackageName = packageName,
            Version = version
        };

        // Assert
        packageReference.PackageName.Should().Be(packageName);
        packageReference.Version.Should().Be(version);
    }

    [Fact]
    public void PackageReference_ShouldHandlePreReleaseVersions()
    {
        // Arrange & Act
        var packageReference = new PackageReference
        {
            PackageName = "Microsoft.AspNetCore.App",
            Version = "7.0.0-preview.1.22076.8"
        };

        // Assert
        packageReference.PackageName.Should().Be("Microsoft.AspNetCore.App");
        packageReference.Version.Should().Be("7.0.0-preview.1.22076.8");
    }

    [Fact]
    public void PackageReference_ShouldHandleVersionRanges()
    {
        // Arrange & Act
        var packageReference = new PackageReference
        {
            PackageName = "Microsoft.Extensions.DependencyInjection",
            Version = "[6.0.0,7.0.0)"
        };

        // Assert
        packageReference.PackageName.Should().Be("Microsoft.Extensions.DependencyInjection");
        packageReference.Version.Should().Be("[6.0.0,7.0.0)");
    }

    [Fact]
    public void PackageReference_ShouldHandleEmptyValues()
    {
        // Arrange & Act
        var packageReference = new PackageReference
        {
            PackageName = "",
            Version = ""
        };

        // Assert
        packageReference.PackageName.Should().BeEmpty();
        packageReference.Version.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Package.Name.With.Dots", "1.0.0")]
    [InlineData("Package_Name_With_Underscores", "2.1.3")]
    [InlineData("Package-Name-With-Hyphens", "1.2.3-alpha")]
    public void PackageReference_ShouldHandleVariousNamingConventions(string packageName, string version)
    {
        // Arrange & Act
        var packageReference = new PackageReference
        {
            PackageName = packageName,
            Version = version
        };

        // Assert
        packageReference.PackageName.Should().Be(packageName);
        packageReference.Version.Should().Be(version);
    }

    [Fact]
    public void PackageReference_ShouldSupportComplexVersionStrings()
    {
        // Arrange
        var testCases = new[]
        {
            ("Package1", "1.0.0"),
            ("Package2", "1.0.0-alpha"),
            ("Package3", "1.0.0-beta.1"),
            ("Package4", "1.0.0-rc.1+build.123"),
            ("Package5", "1.0.0+build.456")
        };

        foreach (var (packageName, version) in testCases)
        {
            // Act
            var packageReference = new PackageReference
            {
                PackageName = packageName,
                Version = version
            };

            // Assert
            packageReference.PackageName.Should().Be(packageName);
            packageReference.Version.Should().Be(version);
        }
    }
}