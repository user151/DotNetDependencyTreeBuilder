using DotNetDependencyTreeBuilder.Exceptions;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Parsers;
using FluentAssertions;

namespace DotNetDependencyTreeBuilder.Tests.Parsers;

public class VBProjectParserTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly VBProjectParser _parser;

    public VBProjectParserTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _parser = new VBProjectParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void CanParse_WithVbprojFile_ReturnsTrue()
    {
        // Arrange
        var filePath = "test.vbproj";

        // Act
        var result = _parser.CanParse(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanParse_WithCsprojFile_ReturnsFalse()
    {
        // Arrange
        var filePath = "test.csproj";

        // Act
        var result = _parser.CanParse(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanParse_WithOtherFile_ReturnsFalse()
    {
        // Arrange
        var filePath = "test.txt";

        // Act
        var result = _parser.CanParse(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        await _parser.Invoking(p => p.ParseProjectFileAsync(null!))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("projectFilePath");
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await _parser.Invoking(p => p.ParseProjectFileAsync(string.Empty))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("projectFilePath");
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "nonexistent.vbproj");

        // Act & Assert
        await _parser.Invoking(p => p.ParseProjectFileAsync(filePath))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithSimpleProject_ReturnsBasicProjectInfo()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;
        var filePath = Path.Combine(_tempDirectory, "SimpleProject.vbproj");
        await File.WriteAllTextAsync(filePath, projectContent);

        // Act
        var result = await _parser.ParseProjectFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.ProjectName.Should().Be("SimpleProject");
        result.Type.Should().Be(ProjectType.VisualBasic);
        result.TargetFramework.Should().Be("net6.0");
        result.FilePath.Should().Be(Path.GetFullPath(filePath));
        result.ProjectReferences.Should().BeEmpty();
        result.PackageReferences.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithProjectReferences_ExtractsProjectDependencies()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Core.Library\Core.Library.vbproj" />
                <ProjectReference Include="..\Utilities\Utilities.vbproj" />
              </ItemGroup>
            </Project>
            """;
        var filePath = Path.Combine(_tempDirectory, "ProjectWithReferences.vbproj");
        await File.WriteAllTextAsync(filePath, projectContent);

        // Act
        var result = await _parser.ParseProjectFileAsync(filePath);

        // Assert
        result.ProjectReferences.Should().HaveCount(2);
        result.ProjectReferences[0].ReferencedProjectPath.Should().Be(@"..\Core.Library\Core.Library.vbproj");
        result.ProjectReferences[0].ReferencedProjectName.Should().Be("Core.Library");
        result.ProjectReferences[0].IsResolved.Should().BeFalse();
        result.ProjectReferences[1].ReferencedProjectPath.Should().Be(@"..\Utilities\Utilities.vbproj");
        result.ProjectReferences[1].ReferencedProjectName.Should().Be("Utilities");
        result.ProjectReferences[1].IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithPackageReferences_ExtractsPackageDependencies()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageReference Include="Microsoft.Extensions.Logging" Version="6.0.0" />
                <PackageReference Include="AutoMapper" />
              </ItemGroup>
            </Project>
            """;
        var filePath = Path.Combine(_tempDirectory, "ProjectWithPackages.vbproj");
        await File.WriteAllTextAsync(filePath, projectContent);

        // Act
        var result = await _parser.ParseProjectFileAsync(filePath);

        // Assert
        result.PackageReferences.Should().HaveCount(3);
        result.PackageReferences[0].PackageName.Should().Be("Newtonsoft.Json");
        result.PackageReferences[0].Version.Should().Be("13.0.3");
        result.PackageReferences[1].PackageName.Should().Be("Microsoft.Extensions.Logging");
        result.PackageReferences[1].Version.Should().Be("6.0.0");
        result.PackageReferences[2].PackageName.Should().Be("AutoMapper");
        result.PackageReferences[2].Version.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithMultipleTargetFrameworks_ExtractsTargetFrameworks()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net6.0;net7.0;net8.0</TargetFrameworks>
              </PropertyGroup>
            </Project>
            """;
        var filePath = Path.Combine(_tempDirectory, "MultiTargetProject.vbproj");
        await File.WriteAllTextAsync(filePath, projectContent);

        // Act
        var result = await _parser.ParseProjectFileAsync(filePath);

        // Assert
        result.TargetFramework.Should().Be("net6.0;net7.0;net8.0");
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithComplexProject_ExtractsAllInformation()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Core.Library\Core.Library.vbproj" />
                <ProjectReference Include="..\Business.Logic\Business.Logic.vbproj" />
              </ItemGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="6.0.0" />
                <PackageReference Include="Serilog" Version="2.12.0" />
              </ItemGroup>
            </Project>
            """;
        var filePath = Path.Combine(_tempDirectory, "ComplexProject.vbproj");
        await File.WriteAllTextAsync(filePath, projectContent);

        // Act
        var result = await _parser.ParseProjectFileAsync(filePath);

        // Assert
        result.ProjectName.Should().Be("ComplexProject");
        result.Type.Should().Be(ProjectType.VisualBasic);
        result.TargetFramework.Should().Be("net6.0");
        result.ProjectReferences.Should().HaveCount(2);
        result.PackageReferences.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithMalformedXml_ThrowsProjectParsingException()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <!-- Missing closing tag -->
            """;
        var filePath = Path.Combine(_tempDirectory, "MalformedProject.vbproj");
        await File.WriteAllTextAsync(filePath, projectContent);

        // Act & Assert
        await _parser.Invoking(p => p.ParseProjectFileAsync(filePath))
            .Should().ThrowAsync<ProjectParsingException>()
            .Where(ex => ex.ProjectPath == filePath);
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithEmptyFile_ThrowsProjectParsingException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "EmptyProject.vbproj");
        await File.WriteAllTextAsync(filePath, string.Empty);

        // Act & Assert
        await _parser.Invoking(p => p.ParseProjectFileAsync(filePath))
            .Should().ThrowAsync<ProjectParsingException>()
            .Where(ex => ex.ProjectPath == filePath);
    }

    [Fact]
    public async Task ParseProjectFileAsync_WithNoTargetFramework_ReturnsEmptyTargetFramework()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
            </Project>
            """;
        var filePath = Path.Combine(_tempDirectory, "NoTargetFramework.vbproj");
        await File.WriteAllTextAsync(filePath, projectContent);

        // Act
        var result = await _parser.ParseProjectFileAsync(filePath);

        // Assert
        result.TargetFramework.Should().BeEmpty();
    }


}