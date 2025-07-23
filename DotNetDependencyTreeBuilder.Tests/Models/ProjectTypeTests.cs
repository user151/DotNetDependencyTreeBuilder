using DotNetDependencyTreeBuilder.Models;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Models;

public class ProjectTypeTests
{
    [Fact]
    public void ProjectType_ShouldHaveCSharpValue()
    {
        // Arrange & Act
        var projectType = ProjectType.CSharp;

        // Assert
        projectType.Should().Be(ProjectType.CSharp);
        ((int)projectType).Should().Be(0);
    }

    [Fact]
    public void ProjectType_ShouldHaveVisualBasicValue()
    {
        // Arrange & Act
        var projectType = ProjectType.VisualBasic;

        // Assert
        projectType.Should().Be(ProjectType.VisualBasic);
        ((int)projectType).Should().Be(1);
    }

    [Fact]
    public void ProjectType_ShouldSupportAllDefinedValues()
    {
        // Arrange
        var expectedValues = new[] { ProjectType.CSharp, ProjectType.VisualBasic };

        // Act
        var actualValues = Enum.GetValues<ProjectType>();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues);
        actualValues.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(ProjectType.CSharp, "CSharp")]
    [InlineData(ProjectType.VisualBasic, "VisualBasic")]
    public void ProjectType_ShouldConvertToStringCorrectly(ProjectType projectType, string expectedString)
    {
        // Act
        var result = projectType.ToString();

        // Assert
        result.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("CSharp", ProjectType.CSharp)]
    [InlineData("VisualBasic", ProjectType.VisualBasic)]
    public void ProjectType_ShouldParseFromStringCorrectly(string input, ProjectType expected)
    {
        // Act
        var result = Enum.Parse<ProjectType>(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("csharp", true, ProjectType.CSharp)]
    [InlineData("visualbasic", true, ProjectType.VisualBasic)]
    [InlineData("CSHARP", true, ProjectType.CSharp)]
    [InlineData("VISUALBASIC", true, ProjectType.VisualBasic)]
    [InlineData("InvalidType", false, default(ProjectType))]
    public void ProjectType_ShouldTryParseCorrectly(string input, bool expectedSuccess, ProjectType expectedValue)
    {
        // Act
        var success = Enum.TryParse<ProjectType>(input, ignoreCase: true, out var result);

        // Assert
        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            result.Should().Be(expectedValue);
        }
    }

    [Fact]
    public void ProjectType_ShouldBeUsableInSwitch()
    {
        // Arrange
        var testCases = new[]
        {
            (ProjectType.CSharp, "C# Project"),
            (ProjectType.VisualBasic, "VB.NET Project")
        };

        foreach (var (projectType, expectedDescription) in testCases)
        {
            // Act
            var description = projectType switch
            {
                ProjectType.CSharp => "C# Project",
                ProjectType.VisualBasic => "VB.NET Project",
                _ => "Unknown Project Type"
            };

            // Assert
            description.Should().Be(expectedDescription);
        }
    }

    [Fact]
    public void ProjectType_ShouldSupportComparison()
    {
        // Arrange
        var csharp1 = ProjectType.CSharp;
        var csharp2 = ProjectType.CSharp;
        var vb = ProjectType.VisualBasic;

        // Act & Assert
        csharp1.Should().Be(csharp2);
        csharp1.Should().NotBe(vb);
        (csharp1 == csharp2).Should().BeTrue();
        (csharp1 == vb).Should().BeFalse();
        (csharp1 != vb).Should().BeTrue();
    }
}