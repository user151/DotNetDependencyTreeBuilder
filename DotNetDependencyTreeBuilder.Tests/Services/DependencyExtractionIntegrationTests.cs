using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Parsers;
using DotNetDependencyTreeBuilder.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetDependencyTreeBuilder.Tests.Services;

/// <summary>
/// Integration tests for dependency extraction logic with various project file scenarios
/// </summary>
public class DependencyExtractionIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly DependencyAnalysisService _dependencyService;
    private readonly CSharpProjectParser _csharpParser;
    private readonly VBProjectParser _vbParser;

    public DependencyExtractionIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        var mockLogger = new Mock<ILogger<DependencyAnalysisService>>();
        _csharpParser = new CSharpProjectParser();
        _vbParser = new VBProjectParser();
        var parsers = new List<IProjectFileParser> { _csharpParser, _vbParser };
        _dependencyService = new DependencyAnalysisService(parsers, mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task DependencyExtraction_WithSimpleProjectChain_ResolvesCorrectly()
    {
        // Arrange
        var coreProjectPath = await CreateProjectFile("Core", "Core.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Extensions.Logging" Version="6.0.0" />
              </ItemGroup>
            </Project>
            """);

        var businessProjectPath = await CreateProjectFile("Business", "Business.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Core\Core.csproj" />
                <PackageReference Include="AutoMapper" Version="12.0.0" />
              </ItemGroup>
            </Project>
            """);

        var appProjectPath = await CreateProjectFile("App", "App.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Business\Business.csproj" />
                <ProjectReference Include="..\Core\Core.csproj" />
              </ItemGroup>
            </Project>
            """);

        // Parse projects
        var coreProject = await _csharpParser.ParseProjectFileAsync(coreProjectPath);
        var businessProject = await _csharpParser.ParseProjectFileAsync(businessProjectPath);
        var appProject = await _csharpParser.ParseProjectFileAsync(appProjectPath);

        var projects = new List<ProjectInfo> { coreProject, businessProject, appProject };

        // Act
        var dependencyGraph = await _dependencyService.AnalyzeDependenciesAsync(projects);
        var buildOrder = _dependencyService.GenerateBuildOrder(dependencyGraph);

        // Assert
        // All dependencies should be resolved
        businessProject.ProjectReferences[0].IsResolved.Should().BeTrue();
        appProject.ProjectReferences[0].IsResolved.Should().BeTrue();
        appProject.ProjectReferences[1].IsResolved.Should().BeTrue();

        // Build order should be correct
        buildOrder.HasCircularDependencies.Should().BeFalse();
        buildOrder.BuildLevels.Should().HaveCount(3);
        
        // Level 1: Core (no dependencies)
        buildOrder.BuildLevels[0].Should().HaveCount(1);
        buildOrder.BuildLevels[0][0].ProjectName.Should().Be("Core");
        
        // Level 2: Business (depends on Core)
        buildOrder.BuildLevels[1].Should().HaveCount(1);
        buildOrder.BuildLevels[1][0].ProjectName.Should().Be("Business");
        
        // Level 3: App (depends on Business and Core)
        buildOrder.BuildLevels[2].Should().HaveCount(1);
        buildOrder.BuildLevels[2][0].ProjectName.Should().Be("App");
    }

    [Fact]
    public async Task DependencyExtraction_WithMixedProjectTypes_ResolvesCorrectly()
    {
        // Arrange
        var csharpProjectPath = await CreateProjectFile("CSharpLib", "CSharpLib.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var vbProjectPath = await CreateProjectFile("VBLib", "VBLib.vbproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\CSharpLib\CSharpLib.csproj" />
              </ItemGroup>
            </Project>
            """);

        // Parse projects
        var csharpProject = await _csharpParser.ParseProjectFileAsync(csharpProjectPath);
        var vbProject = await _vbParser.ParseProjectFileAsync(vbProjectPath);

        var projects = new List<ProjectInfo> { csharpProject, vbProject };

        // Act
        var dependencyGraph = await _dependencyService.AnalyzeDependenciesAsync(projects);

        // Assert
        vbProject.ProjectReferences[0].IsResolved.Should().BeTrue();
        vbProject.ProjectReferences[0].ReferencedProjectPath.Should().Be(csharpProject.FilePath);
        
        dependencyGraph.AdjacencyList[vbProject.FilePath].Should().Contain(csharpProject.FilePath);
    }

    [Fact]
    public async Task DependencyExtraction_WithMissingDependencies_FlagsCorrectly()
    {
        // Arrange
        var appProjectPath = await CreateProjectFile("App", "App.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\MissingProject\MissingProject.csproj" />
                <ProjectReference Include="..\AnotherMissing\AnotherMissing.csproj" />
              </ItemGroup>
            </Project>
            """);

        var appProject = await _csharpParser.ParseProjectFileAsync(appProjectPath);
        var projects = new List<ProjectInfo> { appProject };

        // Act
        var dependencyGraph = await _dependencyService.AnalyzeDependenciesAsync(projects);

        // Assert
        appProject.ProjectReferences.Should().HaveCount(2);
        appProject.ProjectReferences[0].IsResolved.Should().BeFalse();
        appProject.ProjectReferences[1].IsResolved.Should().BeFalse();
        
        // No dependencies should be added to the graph
        dependencyGraph.AdjacencyList[appProject.FilePath].Should().BeEmpty();
    }

    [Fact]
    public async Task DependencyExtraction_WithCircularDependencies_DetectsCorrectly()
    {
        // Arrange
        var project1Path = await CreateProjectFile("Project1", "Project1.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Project2\Project2.csproj" />
              </ItemGroup>
            </Project>
            """);

        var project2Path = await CreateProjectFile("Project2", "Project2.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Project1\Project1.csproj" />
              </ItemGroup>
            </Project>
            """);

        var project1 = await _csharpParser.ParseProjectFileAsync(project1Path);
        var project2 = await _csharpParser.ParseProjectFileAsync(project2Path);
        var projects = new List<ProjectInfo> { project1, project2 };

        // Act
        var dependencyGraph = await _dependencyService.AnalyzeDependenciesAsync(projects);
        var buildOrder = _dependencyService.GenerateBuildOrder(dependencyGraph);

        // Assert
        project1.ProjectReferences[0].IsResolved.Should().BeTrue();
        project2.ProjectReferences[0].IsResolved.Should().BeTrue();
        
        buildOrder.HasCircularDependencies.Should().BeTrue();
        buildOrder.CircularDependencies.Should().HaveCount(2);
        buildOrder.CircularDependencies.Should().Contain(project1.FilePath);
        buildOrder.CircularDependencies.Should().Contain(project2.FilePath);
    }

    [Fact]
    public async Task DependencyExtraction_WithComplexPackageReferences_ExtractsCorrectly()
    {
        // Arrange
        var projectPath = await CreateProjectFile("ComplexApp", "ComplexApp.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="6.0.0" />
                <PackageReference Include="Serilog" Version="2.12.0" />
                <PackageReference Include="AutoMapper" />
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """);

        var project = await _csharpParser.ParseProjectFileAsync(projectPath);
        var projects = new List<ProjectInfo> { project };

        // Act
        var dependencyGraph = await _dependencyService.AnalyzeDependenciesAsync(projects);

        // Assert
        project.PackageReferences.Should().HaveCount(4);
        
        var diPackage = project.PackageReferences.First(p => p.PackageName == "Microsoft.Extensions.DependencyInjection");
        diPackage.Version.Should().Be("6.0.0");
        
        var serilogPackage = project.PackageReferences.First(p => p.PackageName == "Serilog");
        serilogPackage.Version.Should().Be("2.12.0");
        
        var automapperPackage = project.PackageReferences.First(p => p.PackageName == "AutoMapper");
        automapperPackage.Version.Should().BeEmpty();
        
        var newtonsoftPackage = project.PackageReferences.First(p => p.PackageName == "Newtonsoft.Json");
        newtonsoftPackage.Version.Should().Be("13.0.3");
    }

    [Fact]
    public async Task DependencyExtraction_WithRelativePathVariations_ResolvesCorrectly()
    {
        // Arrange
        var coreProjectPath = await CreateProjectFile("Libs\\Core", "Core.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var utilsProjectPath = await CreateProjectFile("Libs\\Utils", "Utils.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var appProjectPath = await CreateProjectFile("Apps\\WebApp", "WebApp.csproj", """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\..\Libs\Core\Core.csproj" />
                <ProjectReference Include="..\..\Libs\Utils\Utils.csproj" />
              </ItemGroup>
            </Project>
            """);

        var coreProject = await _csharpParser.ParseProjectFileAsync(coreProjectPath);
        var utilsProject = await _csharpParser.ParseProjectFileAsync(utilsProjectPath);
        var appProject = await _csharpParser.ParseProjectFileAsync(appProjectPath);
        var projects = new List<ProjectInfo> { coreProject, utilsProject, appProject };

        // Act
        var dependencyGraph = await _dependencyService.AnalyzeDependenciesAsync(projects);
        var buildOrder = _dependencyService.GenerateBuildOrder(dependencyGraph);

        // Assert
        appProject.ProjectReferences.Should().HaveCount(2);
        appProject.ProjectReferences[0].IsResolved.Should().BeTrue();
        appProject.ProjectReferences[1].IsResolved.Should().BeTrue();
        
        buildOrder.BuildLevels.Should().HaveCount(2);
        buildOrder.BuildLevels[0].Should().HaveCount(2); // Core and Utils can be built in parallel
        buildOrder.BuildLevels[1].Should().HaveCount(1); // WebApp depends on both
    }

    [Fact]
    public async Task DependencyExtraction_WithMultiplePackageFormats_HandlesCorrectly()
    {
        // Arrange
        var libProjectPath = await CreateProjectFile("PackageLib", "PackageLib.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageReference Include="AutoMapper" />
              </ItemGroup>
            </Project>
            """);

        var appProjectPath = await CreateProjectFile("ConsumerApp", "ConsumerApp.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\PackageLib\PackageLib.csproj" />
                <PackageReference Include="Microsoft.Extensions.Logging" Version="6.0.0" />
              </ItemGroup>
            </Project>
            """);

        var libProject = await _csharpParser.ParseProjectFileAsync(libProjectPath);
        var appProject = await _csharpParser.ParseProjectFileAsync(appProjectPath);
        var projects = new List<ProjectInfo> { libProject, appProject };

        // Act
        var dependencyGraph = await _dependencyService.AnalyzeDependenciesAsync(projects);

        // Assert
        appProject.ProjectReferences[0].IsResolved.Should().BeTrue();
        
        // Check library packages
        libProject.PackageReferences.Should().HaveCount(2);
        libProject.PackageReferences.Should().Contain(p => p.PackageName == "Newtonsoft.Json" && p.Version == "13.0.3");
        libProject.PackageReferences.Should().Contain(p => p.PackageName == "AutoMapper" && p.Version == "");
        
        // Check app packages
        appProject.PackageReferences.Should().HaveCount(1);
        appProject.PackageReferences[0].PackageName.Should().Be("Microsoft.Extensions.Logging");
        appProject.PackageReferences[0].Version.Should().Be("6.0.0");
    }

    private async Task<string> CreateProjectFile(string projectDir, string fileName, string content)
    {
        var fullDir = Path.Combine(_tempDirectory, projectDir);
        Directory.CreateDirectory(fullDir);
        
        var filePath = Path.Combine(fullDir, fileName);
        await File.WriteAllTextAsync(filePath, content);
        
        return filePath;
    }
}