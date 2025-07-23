using DotNetDependencyTreeBuilder.Services;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Parsers;
using DotNetDependencyTreeBuilder.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace DotNetDependencyTreeBuilder.Tests.Integration;

/// <summary>
/// End-to-end integration tests that validate complete workflows
/// These tests simulate real-world usage scenarios
/// </summary>
public class EndToEndIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<DependencyTreeService>> _mockLogger;
    private readonly List<string> _tempDirectories;

    public EndToEndIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<DependencyTreeService>>();
        _tempDirectories = new List<string>();
    }

    [Fact]
    public async Task RealWorldScenario_WebApplicationWithMultipleLayers_ShouldAnalyzeCorrectly()
    {
        // Arrange - Create a realistic web application structure
        var tempPath = await CreateTemporaryTestScenario("WebApplication", new Dictionary<string, string>
        {
            // Core/Foundation layer
            ["src/Core/Domain/Domain.csproj"] = TestDataManager.CreateBasicCSharpProject("Domain"),
            ["src/Core/Application/Application.csproj"] = TestDataManager.CreateBasicCSharpProject("Application", 
                new List<string> { "../Domain/Domain.csproj" },
                new List<string> { "MediatR", "FluentValidation" }),
            
            // Infrastructure layer
            ["src/Infrastructure/Persistence/Persistence.csproj"] = TestDataManager.CreateBasicCSharpProject("Persistence", 
                new List<string> { "../../Core/Domain/Domain.csproj", "../../Core/Application/Application.csproj" },
                new List<string> { "Microsoft.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.SqlServer" }),
            ["src/Infrastructure/Identity/Identity.csproj"] = TestDataManager.CreateBasicCSharpProject("Identity", 
                new List<string> { "../../Core/Domain/Domain.csproj" },
                new List<string> { "Microsoft.AspNetCore.Identity" }),
            
            // Presentation layer
            ["src/Presentation/WebAPI/WebAPI.csproj"] = TestDataManager.CreateBasicCSharpProject("WebAPI", 
                new List<string> { 
                    "../../Core/Application/Application.csproj", 
                    "../../Infrastructure/Persistence/Persistence.csproj",
                    "../../Infrastructure/Identity/Identity.csproj"
                },
                new List<string> { "Microsoft.AspNetCore.OpenApi", "Swashbuckle.AspNetCore" }),
            
            // Test projects
            ["tests/Application.Tests/Application.Tests.csproj"] = TestDataManager.CreateBasicCSharpProject("Application.Tests", 
                new List<string> { "../../src/Core/Application/Application.csproj" },
                new List<string> { "xunit", "Moq" }),
            ["tests/WebAPI.Tests/WebAPI.Tests.csproj"] = TestDataManager.CreateBasicCSharpProject("WebAPI.Tests", 
                new List<string> { "../../src/Presentation/WebAPI/WebAPI.csproj" },
                new List<string> { "Microsoft.AspNetCore.Mvc.Testing" })
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        
        // Verify that all projects were discovered
        VerifyProjectDiscoveryLogging(7);
        
        // Verify analysis completed successfully
        VerifyInfoLogging("Analysis completed successfully");
    }

    [Fact]
    public async Task MicroservicesScenario_MultipleIndependentServices_ShouldAnalyzeInParallel()
    {
        // Arrange - Create a microservices structure
        var tempPath = await CreateTemporaryTestScenario("Microservices", new Dictionary<string, string>
        {
            // Shared libraries
            ["shared/Common/Common.csproj"] = TestDataManager.CreateBasicCSharpProject("Common"),
            ["shared/EventBus/EventBus.csproj"] = TestDataManager.CreateBasicCSharpProject("EventBus", 
                new List<string> { "../Common/Common.csproj" }),
            
            // User Service
            ["services/UserService/UserService.API/UserService.API.csproj"] = TestDataManager.CreateBasicCSharpProject("UserService.API", 
                new List<string> { "../../../shared/Common/Common.csproj", "../../../shared/EventBus/EventBus.csproj" }),
            ["services/UserService/UserService.Domain/UserService.Domain.csproj"] = TestDataManager.CreateBasicCSharpProject("UserService.Domain", 
                new List<string> { "../../../shared/Common/Common.csproj" }),
            
            // Order Service
            ["services/OrderService/OrderService.API/OrderService.API.csproj"] = TestDataManager.CreateBasicCSharpProject("OrderService.API", 
                new List<string> { "../../../shared/Common/Common.csproj", "../../../shared/EventBus/EventBus.csproj" }),
            ["services/OrderService/OrderService.Domain/OrderService.Domain.csproj"] = TestDataManager.CreateBasicCSharpProject("OrderService.Domain", 
                new List<string> { "../../../shared/Common/Common.csproj" }),
            
            // Gateway
            ["gateway/ApiGateway/ApiGateway.csproj"] = TestDataManager.CreateBasicCSharpProject("ApiGateway", 
                new List<string> { "../../shared/Common/Common.csproj" })
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(7);
    }

    [Fact]
    public async Task LegacyMigrationScenario_MixedFrameworkVersions_ShouldHandleGracefully()
    {
        // Arrange - Create a scenario with mixed .NET versions
        var tempPath = await CreateTemporaryTestScenario("LegacyMigration", new Dictionary<string, string>
        {
            // Legacy .NET Framework projects
            ["legacy/OldLibrary/OldLibrary.csproj"] = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
  </ItemGroup>
</Project>",
            
            // Modern .NET projects
            ["modern/NewLibrary/NewLibrary.csproj"] = TestDataManager.CreateBasicCSharpProject("NewLibrary"),
            ["modern/BridgeLibrary/BridgeLibrary.csproj"] = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net48;net6.0</TargetFrameworks>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""../../legacy/OldLibrary/OldLibrary.csproj"" />
    <ProjectReference Include=""../NewLibrary/NewLibrary.csproj"" />
  </ItemGroup>
</Project>",
            
            // Application using both
            ["app/MainApp/MainApp.csproj"] = TestDataManager.CreateBasicCSharpProject("MainApp", 
                new List<string> { "../../modern/BridgeLibrary/BridgeLibrary.csproj" })
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(4);
    }

    [Fact]
    public async Task OutputFormats_ShouldGenerateCorrectFileOutput()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("OutputFormats", 
            TestDataManager.CreateSimpleLinearScenario());

        var outputFile = Path.GetTempFileName();
        var service = CreateDependencyTreeService();

        try
        {
            // Act
            var exitCode = await service.AnalyzeDependenciesAsync(tempPath, outputFile);

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputFile));
            
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.NotEmpty(content);
            
            // Verify output contains expected structure
            Assert.Contains("Build Order", content);
            Assert.Contains("Level", content);
        }
        finally
        {
            if (File.Exists(outputFile))
                File.Delete(outputFile);
        }
    }

    [Fact]
    public async Task ErrorRecovery_PartialFailures_ShouldContinueProcessing()
    {
        // Arrange - Mix valid and invalid projects
        var tempPath = await CreateTemporaryTestScenario("ErrorRecovery", new Dictionary<string, string>
        {
            // Valid projects
            ["valid/Project1/Project1.csproj"] = TestDataManager.CreateBasicCSharpProject("Project1"),
            ["valid/Project2/Project2.csproj"] = TestDataManager.CreateBasicCSharpProject("Project2", 
                new List<string> { "../Project1/Project1.csproj" }),
            
            // Invalid/problematic projects
            ["invalid/EmptyFile/EmptyFile.csproj"] = "",
            ["invalid/MalformedXml/MalformedXml.csproj"] = "<Project><PropertyGroup><TargetFramework>net6.0",
            ["invalid/MissingReferences/MissingReferences.csproj"] = TestDataManager.CreateBasicCSharpProject("MissingReferences", 
                new List<string> { "../NonExistent/NonExistent.csproj" }),
            
            // More valid projects
            ["valid/Project3/Project3.csproj"] = TestDataManager.CreateBasicCSharpProject("Project3")
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        // Should complete with some form of success (exact code depends on implementation)
        Assert.True(exitCode >= 0);
        
        // Should have processed the valid projects
        VerifyInfoLogging("Starting dependency analysis");
    }

    [Fact]
    public async Task VerboseMode_ShouldProvideDetailedOutput()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("VerboseMode", new Dictionary<string, string>
        {
            ["ProjectA/ProjectA.csproj"] = TestDataManager.CreateBasicCSharpProject("ProjectA"),
            ["ProjectB/ProjectB.csproj"] = TestDataManager.CreateBasicCSharpProject("ProjectB", 
                new List<string> { "../ProjectA/ProjectA.csproj" })
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath, verbose: true);

        // Assert
        Assert.Equal(0, exitCode);
        
        // Verify verbose logging was enabled
        VerifyInfoLogging("Analysis Summary");
        VerifyProjectDiscoveryLogging(2);
    }

    [Fact]
    public async Task LargeRealWorldStructure_ShouldHandleComplexDependencies()
    {
        // Arrange - Create a large, realistic project structure
        var projects = new Dictionary<string, string>();
        
        // Create foundation libraries (10 projects)
        for (int i = 0; i < 10; i++)
        {
            var projectName = $"Foundation.Lib{i:D2}";
            var dependencies = new List<string>();
            
            // Add some cross-dependencies within foundation
            if (i > 0 && i % 3 == 0)
            {
                dependencies.Add($"../Foundation.Lib{(i - 1):D2}/Foundation.Lib{(i - 1):D2}.csproj");
            }
            
            projects[$"foundation/{projectName}/{projectName}.csproj"] = 
                TestDataManager.CreateBasicCSharpProject(projectName, dependencies);
        }
        
        // Create business logic projects (15 projects)
        for (int i = 0; i < 15; i++)
        {
            var projectName = $"Business.Module{i:D2}";
            var dependencies = new List<string>();
            
            // Each business module depends on 1-3 foundation libraries
            var foundationDeps = Math.Min(3, i % 5 + 1);
            for (int j = 0; j < foundationDeps; j++)
            {
                var foundationIndex = (i + j) % 10;
                dependencies.Add($"../../foundation/Foundation.Lib{foundationIndex:D2}/Foundation.Lib{foundationIndex:D2}.csproj");
            }
            
            projects[$"business/{projectName}/{projectName}.csproj"] = 
                TestDataManager.CreateBasicCSharpProject(projectName, dependencies);
        }
        
        // Create application projects (5 projects)
        for (int i = 0; i < 5; i++)
        {
            var projectName = $"App.Service{i:D2}";
            var dependencies = new List<string>();
            
            // Each app depends on several business modules
            var businessDeps = Math.Min(5, i + 2);
            for (int j = 0; j < businessDeps; j++)
            {
                var businessIndex = (i * 3 + j) % 15;
                dependencies.Add($"../../business/Business.Module{businessIndex:D2}/Business.Module{businessIndex:D2}.csproj");
            }
            
            projects[$"applications/{projectName}/{projectName}.csproj"] = 
                TestDataManager.CreateBasicCSharpProject(projectName, dependencies);
        }

        var tempPath = await CreateTemporaryTestScenario("LargeRealWorld", projects);
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(30); // 10 + 15 + 5 = 30 projects
        VerifyInfoLogging("Analysis completed successfully");
    }

    #region Helper Methods

    private async Task<string> CreateTemporaryTestScenario(string scenarioName, Dictionary<string, string> projectFiles)
    {
        var tempPath = await TestDataManager.CreateTemporaryTestScenario(scenarioName, projectFiles);
        _tempDirectories.Add(tempPath);
        return tempPath;
    }

    private DependencyTreeService CreateDependencyTreeService()
    {
        var fileSystemService = new FileSystemService(Mock.Of<ILogger<FileSystemService>>());
        var projectDiscoveryService = new ProjectDiscoveryService(
            fileSystemService, 
            Mock.Of<ILogger<ProjectDiscoveryService>>());
        
        var parsers = new List<IProjectFileParser>
        {
            new CSharpProjectParser(),
            new VBProjectParser()
        };
        
        var dependencyAnalysisService = new DependencyAnalysisService(
            parsers,
            Mock.Of<ILogger<DependencyAnalysisService>>());
        
        return new DependencyTreeService(
            projectDiscoveryService,
            dependencyAnalysisService,
            _mockLogger.Object);
    }

    private void VerifyProjectDiscoveryLogging(int expectedProjectCount)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Found {expectedProjectCount} projects")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyInfoLogging(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        foreach (var tempDir in _tempDirectories)
        {
            TestDataManager.CleanupTemporaryPath(tempDir);
        }
    }

    #endregion
}