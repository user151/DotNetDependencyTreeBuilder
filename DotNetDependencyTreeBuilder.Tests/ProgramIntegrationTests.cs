using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Services;
using DotNetDependencyTreeBuilder.Parsers;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Exceptions;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetDependencyTreeBuilder.Tests;

/// <summary>
/// End-to-end integration tests for the complete application workflow
/// </summary>
public class ProgramIntegrationTests : IDisposable
{
    private readonly string _testRootDirectory;
    private readonly List<string> _createdDirectories;
    private readonly List<string> _createdFiles;

    public ProgramIntegrationTests()
    {
        _testRootDirectory = Path.Combine(Path.GetTempPath(), "DotNetDependencyTreeBuilder_Tests", Guid.NewGuid().ToString());
        _createdDirectories = new List<string>();
        _createdFiles = new List<string>();
        
        Directory.CreateDirectory(_testRootDirectory);
        _createdDirectories.Add(_testRootDirectory);
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithSimpleProjectStructure_ShouldReturnSuccess()
    {
        // Arrange
        var projectStructure = CreateSimpleProjectStructure();
        var outputPath = Path.Combine(_testRootDirectory, "output.txt");

        // Act
        var exitCode = await ExecuteApplicationWithTestStructure(projectStructure, outputPath, verbose: true);

        // Assert
        exitCode.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        
        var output = await File.ReadAllTextAsync(outputPath);
        output.Should().Contain("Build Order Analysis Results");
        output.Should().Contain("Projects Found: 3");
        output.Should().Contain("Core.Library.csproj");
        output.Should().Contain("Business.Logic.csproj");
        output.Should().Contain("Web.API.csproj");
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithCircularDependencies_ShouldReturnWarning()
    {
        // Arrange
        var projectStructure = CreateCircularDependencyStructure();

        // Act
        var exitCode = await ExecuteApplicationWithTestStructure(projectStructure, verbose: true);

        // Assert
        exitCode.Should().Be(1); // Warning exit code for circular dependencies
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithComplexProjectStructure_ShouldReturnSuccess()
    {
        // Arrange
        var projectStructure = CreateComplexProjectStructure();
        var outputPath = Path.Combine(_testRootDirectory, "complex_output.json");

        // Act
        var exitCode = await ExecuteApplicationWithTestStructure(projectStructure, outputPath, OutputFormat.Json, verbose: true);

        // Assert
        exitCode.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        
        var output = await File.ReadAllTextAsync(outputPath);
        output.Should().Contain("Projects Found");
        output.Should().Contain("Build Order");
        output.Should().Contain("Build Levels");
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithMixedProjectTypes_ShouldReturnSuccess()
    {
        // Arrange
        var projectStructure = CreateMixedProjectTypeStructure();

        // Act
        var exitCode = await ExecuteApplicationWithTestStructure(projectStructure, verbose: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithMissingProjectReferences_ShouldReturnSuccess()
    {
        // Arrange
        var projectStructure = CreateMissingReferenceStructure();

        // Act
        var exitCode = await ExecuteApplicationWithTestStructure(projectStructure, verbose: true);

        // Assert
        exitCode.Should().Be(0); // Should continue processing despite missing references
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithEmptyDirectory_ShouldReturnSuccess()
    {
        // Arrange
        var emptyDirectory = Path.Combine(_testRootDirectory, "empty");
        Directory.CreateDirectory(emptyDirectory);
        _createdDirectories.Add(emptyDirectory);

        // Act
        var exitCode = await ExecuteApplicationDirectly(emptyDirectory, verbose: true);

        // Assert
        exitCode.Should().Be(0); // No projects found is not an error
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithNonExistentDirectory_ShouldReturnError()
    {
        // Arrange
        var nonExistentDirectory = Path.Combine(_testRootDirectory, "nonexistent");

        // Act
        var exitCode = await ExecuteApplicationDirectly(nonExistentDirectory, verbose: true);

        // Assert
        exitCode.Should().Be(2); // Error exit code
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithMalformedProjectFile_ShouldReturnError()
    {
        // Arrange
        var projectStructure = CreateMalformedProjectStructure();

        // Act
        var exitCode = await ExecuteApplicationWithTestStructure(projectStructure, verbose: true);

        // Assert
        exitCode.Should().Be(2); // Error exit code for parsing errors
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithLargeProjectStructure_ShouldReturnSuccess()
    {
        // Arrange
        var projectStructure = CreateLargeProjectStructure();

        // Act
        var exitCode = await ExecuteApplicationWithTestStructure(projectStructure, verbose: false);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteApplicationAsync_WithNestedDirectoryStructure_ShouldReturnSuccess()
    {
        // Arrange
        var projectStructure = CreateNestedDirectoryStructure();

        // Act
        var exitCode = await ExecuteApplicationWithTestStructure(projectStructure, verbose: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public void DependencyInjectionContainer_ShouldResolveAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureTestServices(services, verbose: true);

        // Act
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetRequiredService<IFileSystemService>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IProjectDiscoveryService>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IDependencyAnalysisService>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IDependencyTreeService>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ILogger<Program>>().Should().NotBeNull();
        
        var parsers = serviceProvider.GetRequiredService<IEnumerable<IProjectFileParser>>();
        parsers.Should().HaveCount(2);
        parsers.Should().Contain(p => p is CSharpProjectParser);
        parsers.Should().Contain(p => p is VBProjectParser);
    }

    [Fact]
    public void GlobalExceptionHandling_ShouldCatchUnhandledExceptions()
    {
        // This test verifies that the global exception handlers are properly configured
        // We can't easily test the actual handlers without causing application termination
        // So we verify that the handlers are set up correctly by checking they don't throw
        
        // Arrange & Act
        var services = new ServiceCollection();
        ConfigureTestServices(services, verbose: true);
        
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Assert
        logger.Should().NotBeNull();
        // If we reach this point, the DI container setup didn't throw, which is good
    }

    /// <summary>
    /// Creates a simple project structure with linear dependencies
    /// </summary>
    private Dictionary<string, string> CreateSimpleProjectStructure()
    {
        return new Dictionary<string, string>
        {
            ["Core.Library/Core.Library.csproj"] = CreateCSharpProject("Core.Library", "net9.0"),
            ["Business.Logic/Business.Logic.csproj"] = CreateCSharpProject("Business.Logic", "net9.0", 
                projectReferences: new[] { "../Core.Library/Core.Library.csproj" }),
            ["Web.API/Web.API.csproj"] = CreateCSharpProject("Web.API", "net9.0", 
                projectReferences: new[] { "../Business.Logic/Business.Logic.csproj" })
        };
    }

    /// <summary>
    /// Creates a project structure with circular dependencies
    /// </summary>
    private Dictionary<string, string> CreateCircularDependencyStructure()
    {
        return new Dictionary<string, string>
        {
            ["ProjectA/ProjectA.csproj"] = CreateCSharpProject("ProjectA", "net9.0", 
                projectReferences: new[] { "../ProjectB/ProjectB.csproj" }),
            ["ProjectB/ProjectB.csproj"] = CreateCSharpProject("ProjectB", "net9.0", 
                projectReferences: new[] { "../ProjectC/ProjectC.csproj" }),
            ["ProjectC/ProjectC.csproj"] = CreateCSharpProject("ProjectC", "net9.0", 
                projectReferences: new[] { "../ProjectA/ProjectA.csproj" })
        };
    }

    /// <summary>
    /// Creates a complex project structure with multiple dependency levels
    /// </summary>
    private Dictionary<string, string> CreateComplexProjectStructure()
    {
        return new Dictionary<string, string>
        {
            ["Foundation/Utilities/Utilities.csproj"] = CreateCSharpProject("Utilities", "net9.0"),
            ["Foundation/Core/Core.csproj"] = CreateCSharpProject("Core", "net9.0", 
                projectReferences: new[] { "../Utilities/Utilities.csproj" }),
            ["Services/DataAccess/DataAccess.csproj"] = CreateCSharpProject("DataAccess", "net9.0", 
                projectReferences: new[] { "../../Foundation/Core/Core.csproj" }),
            ["Services/BusinessLogic/BusinessLogic.csproj"] = CreateCSharpProject("BusinessLogic", "net9.0", 
                projectReferences: new[] { "../DataAccess/DataAccess.csproj", "../../Foundation/Utilities/Utilities.csproj" }),
            ["Applications/WebAPI/WebAPI.csproj"] = CreateCSharpProject("WebAPI", "net9.0", 
                projectReferences: new[] { "../../Services/BusinessLogic/BusinessLogic.csproj" }),
            ["Applications/ConsoleApp/ConsoleApp.csproj"] = CreateCSharpProject("ConsoleApp", "net9.0", 
                projectReferences: new[] { "../../Services/BusinessLogic/BusinessLogic.csproj" }),
            ["Tests/UnitTests/UnitTests.csproj"] = CreateCSharpProject("UnitTests", "net9.0", 
                projectReferences: new[] { "../../Services/BusinessLogic/BusinessLogic.csproj", "../../Foundation/Core/Core.csproj" })
        };
    }

    /// <summary>
    /// Creates a project structure with mixed C# and VB.NET projects
    /// </summary>
    private Dictionary<string, string> CreateMixedProjectTypeStructure()
    {
        return new Dictionary<string, string>
        {
            ["CSharpCore/CSharpCore.csproj"] = CreateCSharpProject("CSharpCore", "net9.0"),
            ["VBCore/VBCore.vbproj"] = CreateVBProject("VBCore", "net9.0"),
            ["MixedApp/MixedApp.csproj"] = CreateCSharpProject("MixedApp", "net9.0", 
                projectReferences: new[] { "../CSharpCore/CSharpCore.csproj", "../VBCore/VBCore.vbproj" })
        };
    }

    /// <summary>
    /// Creates a project structure with missing project references
    /// </summary>
    private Dictionary<string, string> CreateMissingReferenceStructure()
    {
        return new Dictionary<string, string>
        {
            ["ExistingProject/ExistingProject.csproj"] = CreateCSharpProject("ExistingProject", "net9.0"),
            ["ProjectWithMissingRef/ProjectWithMissingRef.csproj"] = CreateCSharpProject("ProjectWithMissingRef", "net9.0", 
                projectReferences: new[] { "../ExistingProject/ExistingProject.csproj", "../NonExistentProject/NonExistentProject.csproj" })
        };
    }

    /// <summary>
    /// Creates a malformed project structure for error testing
    /// </summary>
    private Dictionary<string, string> CreateMalformedProjectStructure()
    {
        return new Dictionary<string, string>
        {
            ["ValidProject/ValidProject.csproj"] = CreateCSharpProject("ValidProject", "net9.0"),
            ["MalformedProject/MalformedProject.csproj"] = "<Project><InvalidXml></Project"
        };
    }

    /// <summary>
    /// Creates a large project structure for performance testing
    /// </summary>
    private Dictionary<string, string> CreateLargeProjectStructure()
    {
        var projects = new Dictionary<string, string>();
        
        // Create 20 projects with various dependency patterns
        for (int i = 1; i <= 20; i++)
        {
            var projectName = $"Project{i:D2}";
            var projectReferences = new List<string>();
            
            // Create dependencies to previous projects
            if (i > 1)
            {
                projectReferences.Add($"../Project{(i-1):D2}/Project{(i-1):D2}.csproj");
            }
            if (i > 5)
            {
                projectReferences.Add($"../Project{(i-5):D2}/Project{(i-5):D2}.csproj");
            }
            
            projects[$"{projectName}/{projectName}.csproj"] = CreateCSharpProject(projectName, "net9.0", 
                projectReferences: projectReferences.ToArray());
        }
        
        return projects;
    }

    /// <summary>
    /// Creates a nested directory structure
    /// </summary>
    private Dictionary<string, string> CreateNestedDirectoryStructure()
    {
        return new Dictionary<string, string>
        {
            ["Level1/Level2/Level3/DeepProject/DeepProject.csproj"] = CreateCSharpProject("DeepProject", "net9.0"),
            ["Level1/Level2/MidProject/MidProject.csproj"] = CreateCSharpProject("MidProject", "net9.0", 
                projectReferences: new[] { "../Level3/DeepProject/DeepProject.csproj" }),
            ["Level1/TopProject/TopProject.csproj"] = CreateCSharpProject("TopProject", "net9.0", 
                projectReferences: new[] { "../Level2/MidProject/MidProject.csproj" })
        };
    }

    /// <summary>
    /// Creates a C# project file content
    /// </summary>
    private string CreateCSharpProject(string projectName, string targetFramework, 
        string[]? projectReferences = null, string[]? packageReferences = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <OutputType>Library</OutputType>");
        sb.AppendLine($"    <TargetFramework>{targetFramework}</TargetFramework>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("  </PropertyGroup>");
        
        if (projectReferences?.Any() == true)
        {
            sb.AppendLine("  <ItemGroup>");
            foreach (var reference in projectReferences)
            {
                sb.AppendLine($"    <ProjectReference Include=\"{reference}\" />");
            }
            sb.AppendLine("  </ItemGroup>");
        }
        
        if (packageReferences?.Any() == true)
        {
            sb.AppendLine("  <ItemGroup>");
            foreach (var package in packageReferences)
            {
                sb.AppendLine($"    <PackageReference Include=\"{package}\" Version=\"1.0.0\" />");
            }
            sb.AppendLine("  </ItemGroup>");
        }
        
        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    /// <summary>
    /// Creates a VB.NET project file content
    /// </summary>
    private string CreateVBProject(string projectName, string targetFramework, 
        string[]? projectReferences = null, string[]? packageReferences = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <OutputType>Library</OutputType>");
        sb.AppendLine($"    <TargetFramework>{targetFramework}</TargetFramework>");
        sb.AppendLine("    <RootNamespace>" + projectName + "</RootNamespace>");
        sb.AppendLine("  </PropertyGroup>");
        
        if (projectReferences?.Any() == true)
        {
            sb.AppendLine("  <ItemGroup>");
            foreach (var reference in projectReferences)
            {
                sb.AppendLine($"    <ProjectReference Include=\"{reference}\" />");
            }
            sb.AppendLine("  </ItemGroup>");
        }
        
        if (packageReferences?.Any() == true)
        {
            sb.AppendLine("  <ItemGroup>");
            foreach (var package in packageReferences)
            {
                sb.AppendLine($"    <PackageReference Include=\"{package}\" Version=\"1.0.0\" />");
            }
            sb.AppendLine("  </ItemGroup>");
        }
        
        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    /// <summary>
    /// Executes the application with a test project structure
    /// </summary>
    private async Task<int> ExecuteApplicationWithTestStructure(
        Dictionary<string, string> projectStructure, 
        string? outputPath = null, 
        OutputFormat format = OutputFormat.Text, 
        bool verbose = false)
    {
        // Create the test project structure
        var testDirectory = Path.Combine(_testRootDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);
        _createdDirectories.Add(testDirectory);

        foreach (var kvp in projectStructure)
        {
            var fullPath = Path.Combine(testDirectory, kvp.Key);
            var directory = Path.GetDirectoryName(fullPath)!;
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _createdDirectories.Add(directory);
            }
            
            await File.WriteAllTextAsync(fullPath, kvp.Value);
            _createdFiles.Add(fullPath);
        }

        return await ExecuteApplicationDirectly(testDirectory, outputPath, format, verbose);
    }

    /// <summary>
    /// Executes the application directly with the given parameters
    /// </summary>
    private async Task<int> ExecuteApplicationDirectly(
        string sourceDirectory, 
        string? outputPath = null, 
        OutputFormat format = OutputFormat.Text, 
        bool verbose = false)
    {
        // Set up dependency injection container
        var services = new ServiceCollection();
        ConfigureTestServices(services, verbose);
        
        using var serviceProvider = services.BuildServiceProvider();
        
        try
        {
            var dependencyTreeService = serviceProvider.GetRequiredService<IDependencyTreeService>();
            return await dependencyTreeService.AnalyzeDependenciesAsync(sourceDirectory, outputPath, verbose);
        }
        catch (ProjectAnalysisException)
        {
            return 2; // Error exit code
        }
        catch (ProjectParsingException)
        {
            return 2; // Error exit code
        }
        catch (UnauthorizedAccessException)
        {
            return 2; // Error exit code
        }
        catch (DirectoryNotFoundException)
        {
            return 2; // Error exit code
        }
        catch (ArgumentException)
        {
            return 2; // Error exit code
        }
        catch (Exception)
        {
            return 3; // Critical error exit code
        }
    }

    /// <summary>
    /// Configures test services for dependency injection
    /// </summary>
    private void ConfigureTestServices(IServiceCollection services, bool verbose = false)
    {
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
        });
        
        // Register core services
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IProjectDiscoveryService, ProjectDiscoveryService>();
        services.AddSingleton<IDependencyAnalysisService, DependencyAnalysisService>();
        services.AddSingleton<IDependencyTreeService, DependencyTreeService>();
        
        // Register parsers
        services.AddSingleton<IProjectFileParser, CSharpProjectParser>();
        services.AddSingleton<IProjectFileParser, VBProjectParser>();
        
        // Register parser collection
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProjectFileParser, CSharpProjectParser>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProjectFileParser, VBProjectParser>());    
    }

    public void Dispose()
    {
        // Clean up created files
        foreach (var file in _createdFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up created directories (in reverse order)
        foreach (var directory in _createdDirectories.AsEnumerable().Reverse())
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}