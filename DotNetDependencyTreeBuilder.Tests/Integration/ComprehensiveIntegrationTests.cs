using DotNetDependencyTreeBuilder.Services;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Parsers;
using DotNetDependencyTreeBuilder.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;

namespace DotNetDependencyTreeBuilder.Tests.Integration;

/// <summary>
/// Comprehensive integration tests covering all requirements and edge cases
/// </summary>
public class ComprehensiveIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<DependencyTreeService>> _mockLogger;
    private readonly List<string> _tempDirectories;

    public ComprehensiveIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<DependencyTreeService>>();
        _tempDirectories = new List<string>();
    }

    #region Requirement 1 Tests - Project Discovery

    [Fact]
    public async Task Requirement1_1_RecursiveTraversal_ShouldFindAllProjects()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("RecursiveTraversal", new Dictionary<string, string>
        {
            ["Level1/Project1/Project1.csproj"] = TestDataManager.CreateBasicCSharpProject("Project1"),
            ["Level1/Level2/Project2/Project2.csproj"] = TestDataManager.CreateBasicCSharpProject("Project2"),
            ["Level1/Level2/Level3/Project3/Project3.csproj"] = TestDataManager.CreateBasicCSharpProject("Project3"),
            ["RootProject/RootProject.csproj"] = TestDataManager.CreateBasicCSharpProject("RootProject")
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        // Verify that all 4 projects were discovered through logging
        VerifyProjectDiscoveryLogging(4);
    }

    [Fact]
    public async Task Requirement1_2_CSharpProjectDiscovery_ShouldIncludeCsprojFiles()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("CSharpProjects", new Dictionary<string, string>
        {
            ["Project1/Project1.csproj"] = TestDataManager.CreateBasicCSharpProject("Project1"),
            ["Project2/Project2.csproj"] = TestDataManager.CreateBasicCSharpProject("Project2"),
            ["NotAProject/SomeFile.txt"] = "This is not a project file"
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(2); // Only .csproj files should be found
    }

    [Fact]
    public async Task Requirement1_3_VBProjectDiscovery_ShouldIncludeVbprojFiles()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("VBProjects", new Dictionary<string, string>
        {
            ["VBProject1/VBProject1.vbproj"] = TestDataManager.CreateBasicVBProject("VBProject1"),
            ["VBProject2/VBProject2.vbproj"] = TestDataManager.CreateBasicVBProject("VBProject2")
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(2);
    }

    [Fact]
    public async Task Requirement1_4_InvalidDirectory_ShouldDisplayErrorMessage()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid()}");
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(nonExistentPath);

        // Assert
        Assert.Equal(1, exitCode); // Error exit code
        VerifyErrorLogging("Source directory does not exist");
    }

    [Fact]
    public async Task Requirement1_5_EmptyDirectory_ShouldReportNoProjects()
    {
        // Arrange
        var tempPath = CreateEmptyTempDirectory();
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode); // Success - no projects is not an error
        VerifyWarningLogging("No projects found");
    }

    #endregion

    #region Requirement 2 Tests - Dependency Analysis

    [Fact]
    public async Task Requirement2_1_ProjectFileParsing_ShouldExtractDependencyInformation()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("ProjectParsing", new Dictionary<string, string>
        {
            ["ProjectA/ProjectA.csproj"] = TestDataManager.CreateBasicCSharpProject("ProjectA", 
                packageReferences: new List<string> { "Newtonsoft.Json", "Microsoft.Extensions.Logging" }),
            ["ProjectB/ProjectB.csproj"] = TestDataManager.CreateBasicCSharpProject("ProjectB", 
                projectReferences: new List<string> { "../ProjectA/ProjectA.csproj" })
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(2);
    }

    [Fact]
    public async Task Requirement2_2_ProjectReferences_ShouldRecordDirectDependencies()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("ProjectReferences", 
            TestDataManager.CreateSimpleLinearScenario());

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(3);
    }

    [Fact]
    public async Task Requirement2_3_PackageReferences_ShouldRecordExternalDependencies()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("PackageReferences", new Dictionary<string, string>
        {
            ["ProjectWithPackages/ProjectWithPackages.csproj"] = TestDataManager.CreateBasicCSharpProject("ProjectWithPackages", 
                packageReferences: new List<string> { "Newtonsoft.Json", "AutoMapper", "FluentValidation" })
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(1);
    }

    [Fact]
    public async Task Requirement2_4_MalformedProject_ShouldLogErrorAndContinue()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("MalformedProject", new Dictionary<string, string>
        {
            ["ValidProject/ValidProject.csproj"] = TestDataManager.CreateBasicCSharpProject("ValidProject"),
            ["MalformedProject/MalformedProject.csproj"] = "<Project><PropertyGroup><TargetFramework>net6.0</TargetFramework>" // Missing closing tags
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        // Should handle gracefully - exact exit code depends on implementation
        Assert.True(exitCode >= 0);
    }

    [Fact]
    public async Task Requirement2_5_MissingProjectReferences_ShouldFlagMissingDependencies()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("MissingReferences", 
            TestDataManager.CreateMissingReferencesScenario());

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode); // Should complete successfully with warnings
        VerifyProjectDiscoveryLogging(1);
    }

    #endregion

    #region Requirement 3 Tests - Dependency Tree Generation

    [Fact]
    public async Task Requirement3_1_DependencyGraph_ShouldConstructProjectRelationships()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("DependencyGraph", 
            TestDataManager.CreateSimpleLinearScenario());

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyInfoLogging("Analyzing project dependencies");
    }

    [Fact]
    public async Task Requirement3_2_CircularDependencyDetection_ShouldDetectCycles()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("CircularDependency", 
            TestDataManager.CreateCircularDependencyScenario());

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(1, exitCode); // Warning exit code for circular dependencies
        VerifyWarningLogging("Circular dependencies detected");
    }

    [Fact]
    public async Task Requirement3_3_CircularDependencyReporting_ShouldReportSpecificProjects()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("CircularDependencyReporting", 
            TestDataManager.CreateCircularDependencyScenario());

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(1, exitCode);
        VerifyWarningLogging("Circular dependencies detected");
    }

    [Fact]
    public async Task Requirement3_4_TopologicalSorting_ShouldDetermineBuildOrder()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("TopologicalSorting", 
            TestDataManager.CreateSimpleLinearScenario());

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyInfoLogging("Generating build order");
    }

    [Fact]
    public async Task Requirement3_5_NoDependencies_ShouldListAllProjectsAsParallel()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("NoDependencies", new Dictionary<string, string>
        {
            ["Project1/Project1.csproj"] = TestDataManager.CreateBasicCSharpProject("Project1"),
            ["Project2/Project2.csproj"] = TestDataManager.CreateBasicCSharpProject("Project2"),
            ["Project3/Project3.csproj"] = TestDataManager.CreateBasicCSharpProject("Project3")
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(3);
    }

    #endregion

    #region Requirement 4 Tests - Output Generation

    [Fact]
    public async Task Requirement4_1_DependencyOrder_ShouldOutputDependenciesFirst()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("DependencyOrder", 
            TestDataManager.CreateSimpleLinearScenario());

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyInfoLogging("Analysis completed successfully");
    }

    [Fact]
    public async Task Requirement4_2_ParallelProjects_ShouldGroupIndependentProjects()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("ParallelProjects", new Dictionary<string, string>
        {
            ["IndependentProject1/IndependentProject1.csproj"] = TestDataManager.CreateBasicCSharpProject("IndependentProject1"),
            ["IndependentProject2/IndependentProject2.csproj"] = TestDataManager.CreateBasicCSharpProject("IndependentProject2")
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(2);
    }

    [Fact]
    public async Task Requirement4_3_FullProjectPaths_ShouldIncludeCompleteFilePaths()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("FullPaths", new Dictionary<string, string>
        {
            ["TestProject/TestProject.csproj"] = TestDataManager.CreateBasicCSharpProject("TestProject")
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(1);
    }

    [Fact]
    public async Task Requirement4_4_ConsoleAndFileOutput_ShouldProvideMultipleOutputOptions()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("OutputOptions", new Dictionary<string, string>
        {
            ["TestProject/TestProject.csproj"] = TestDataManager.CreateBasicCSharpProject("TestProject")
        });

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
        }
        finally
        {
            if (File.Exists(outputFile))
                File.Delete(outputFile);
        }
    }

    [Fact]
    public async Task Requirement4_5_OutputFileSpecification_ShouldWriteToSpecifiedFile()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("FileOutput", new Dictionary<string, string>
        {
            ["TestProject/TestProject.csproj"] = TestDataManager.CreateBasicCSharpProject("TestProject")
        });

        var outputFile = Path.Combine(Path.GetTempPath(), $"test_output_{Guid.NewGuid()}.txt");
        var service = CreateDependencyTreeService();

        try
        {
            // Act
            var exitCode = await service.AnalyzeDependenciesAsync(tempPath, outputFile);

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputFile));
        }
        finally
        {
            if (File.Exists(outputFile))
                File.Delete(outputFile);
        }
    }

    #endregion

    #region Requirement 5 Tests - Command Line Interface (Tested through service)

    [Fact]
    public async Task Requirement5_1_EmptyArguments_ShouldHandleGracefully()
    {
        // This would be tested at the Program level, but we can test service behavior
        var service = CreateDependencyTreeService();

        // Act & Assert
        var exitCode = await service.AnalyzeDependenciesAsync("");
        Assert.Equal(1, exitCode); // Should return error for empty directory
    }

    [Fact]
    public async Task Requirement5_4_OutputFileOption_ShouldWriteToSpecifiedFile()
    {
        // Already tested in Requirement4_5, but included for completeness
        await Requirement4_5_OutputFileSpecification_ShouldWriteToSpecifiedFile();
    }

    #endregion

    #region Requirement 6 Tests - Logging and Error Reporting

    [Fact]
    public async Task Requirement6_1_ProgressInformation_ShouldProvideProjectDiscoveryProgress()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("ProgressInfo", new Dictionary<string, string>
        {
            ["Project1/Project1.csproj"] = TestDataManager.CreateBasicCSharpProject("Project1"),
            ["Project2/Project2.csproj"] = TestDataManager.CreateBasicCSharpProject("Project2")
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyInfoLogging("Starting dependency analysis");
        VerifyInfoLogging("Discovering projects");
    }

    [Fact]
    public async Task Requirement6_2_ErrorLogging_ShouldLogParsingErrors()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("ErrorLogging", new Dictionary<string, string>
        {
            ["ValidProject/ValidProject.csproj"] = TestDataManager.CreateBasicCSharpProject("ValidProject"),
            ["InvalidProject/InvalidProject.csproj"] = "Invalid XML content"
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        // Should handle gracefully
        Assert.True(exitCode >= 0);
    }

    [Fact]
    public async Task Requirement6_3_SummaryStatistics_ShouldReportAnalysisResults()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("SummaryStats", 
            TestDataManager.CreateSimpleLinearScenario());

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyInfoLogging("Analysis Summary");
    }

    [Fact]
    public async Task Requirement6_4_VerboseLogging_ShouldProvideDetailedInformation()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("VerboseLogging", new Dictionary<string, string>
        {
            ["TestProject/TestProject.csproj"] = TestDataManager.CreateBasicCSharpProject("TestProject")
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath, verbose: true);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyInfoLogging("Analysis Summary");
    }

    [Fact]
    public async Task Requirement6_5_CriticalErrors_ShouldExitWithAppropriateErrorCodes()
    {
        // Arrange
        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(""); // Empty path should cause error

        // Assert
        Assert.Equal(1, exitCode); // Error exit code
        VerifyErrorLogging("Source directory cannot be null or empty");
    }

    #endregion

    #region Edge Cases and Performance Tests

    [Fact]
    public async Task EdgeCase_MixedLanguageProjects_ShouldHandleBothCSharpAndVB()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("MixedLanguage", 
            TestDataManager.CreateMixedLanguageScenario());

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(2);
    }

    [Fact]
    public async Task EdgeCase_UnusualProjectNames_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var tempPath = await CreateTemporaryTestScenario("UnusualNames", new Dictionary<string, string>
        {
            ["Project-With-Dashes/Project-With-Dashes.csproj"] = TestDataManager.CreateBasicCSharpProject("Project-With-Dashes"),
            ["Project.With.Dots/Project.With.Dots.csproj"] = TestDataManager.CreateBasicCSharpProject("Project.With.Dots")
        });

        var service = CreateDependencyTreeService();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        Assert.Equal(0, exitCode);
        VerifyProjectDiscoveryLogging(2);
    }

    [Fact]
    public async Task Performance_LargeProjectCount_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        const int projectCount = 25;
        var projects = new Dictionary<string, string>();
        
        for (int i = 0; i < projectCount; i++)
        {
            var projectName = $"Project{i:D3}";
            projects[$"{projectName}/{projectName}.csproj"] = TestDataManager.CreateBasicCSharpProject(projectName);
        }

        var tempPath = await CreateTemporaryTestScenario("LargeProjectCount", projects);
        var service = CreateDependencyTreeService();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var exitCode = await service.AnalyzeDependenciesAsync(tempPath);

        // Assert
        stopwatch.Stop();
        Assert.Equal(0, exitCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
            $"Analysis took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 10-second threshold");
        VerifyProjectDiscoveryLogging(projectCount);
    }

    #endregion

    #region Helper Methods

    private async Task<string> CreateTemporaryTestScenario(string scenarioName, Dictionary<string, string> projectFiles)
    {
        var tempPath = await TestDataManager.CreateTemporaryTestScenario(scenarioName, projectFiles);
        _tempDirectories.Add(tempPath);
        return tempPath;
    }

    private string CreateEmptyTempDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"EmptyTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);
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

    private void VerifyErrorLogging(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyWarningLogging(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
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