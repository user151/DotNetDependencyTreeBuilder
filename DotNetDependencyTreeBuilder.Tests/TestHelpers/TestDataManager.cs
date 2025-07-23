namespace DotNetDependencyTreeBuilder.Tests.TestHelpers;

/// <summary>
/// Helper class for managing test data and creating test scenarios
/// </summary>
public static class TestDataManager
{
    public static string GetTestDataPath()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "TestData");
    }

    public static string GetTestScenarioPath(string scenarioName)
    {
        return Path.Combine(GetTestDataPath(), scenarioName);
    }

    public static async Task<string> CreateTemporaryTestScenario(string scenarioName, Dictionary<string, string> projectFiles)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"DependencyTreeTest_{scenarioName}_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        foreach (var kvp in projectFiles)
        {
            var projectPath = kvp.Key;
            var projectContent = kvp.Value;
            
            var fullPath = Path.Combine(tempPath, projectPath);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(fullPath, projectContent);
        }

        return tempPath;
    }

    public static void CleanupTemporaryPath(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    public static string CreateBasicCSharpProject(string projectName, List<string>? projectReferences = null, List<string>? packageReferences = null)
    {
        projectReferences ??= new List<string>();
        packageReferences ??= new List<string>();

        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>";

        foreach (var projectRef in projectReferences)
        {
            content += $@"
    <ProjectReference Include=""{projectRef}"" />";
        }

        foreach (var packageRef in packageReferences)
        {
            content += $@"
    <PackageReference Include=""{packageRef}"" Version=""6.0.0"" />";
        }

        content += @"
  </ItemGroup>

</Project>";

        return content;
    }

    public static string CreateBasicVBProject(string projectName, List<string>? projectReferences = null, List<string>? packageReferences = null)
    {
        projectReferences ??= new List<string>();
        packageReferences ??= new List<string>();

        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <RootNamespace>" + projectName + @"</RootNamespace>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>";

        foreach (var projectRef in projectReferences)
        {
            content += $@"
    <ProjectReference Include=""{projectRef}"" />";
        }

        content += @"
    <PackageReference Include=""Microsoft.VisualBasic"" Version=""10.3.0"" />";

        foreach (var packageRef in packageReferences)
        {
            content += $@"
    <PackageReference Include=""{packageRef}"" Version=""6.0.0"" />";
        }

        content += @"
  </ItemGroup>

</Project>";

        return content;
    }

    public static Dictionary<string, string> CreateSimpleLinearScenario()
    {
        return new Dictionary<string, string>
        {
            ["ProjectA/ProjectA.csproj"] = CreateBasicCSharpProject("ProjectA", 
                packageReferences: new List<string> { "Newtonsoft.Json", "Microsoft.Extensions.Logging" }),
            ["ProjectB/ProjectB.csproj"] = CreateBasicCSharpProject("ProjectB", 
                projectReferences: new List<string> { "../ProjectA/ProjectA.csproj" },
                packageReferences: new List<string> { "Microsoft.Extensions.DependencyInjection" }),
            ["ProjectC/ProjectC.csproj"] = CreateBasicCSharpProject("ProjectC", 
                projectReferences: new List<string> { "../ProjectB/ProjectB.csproj" },
                packageReferences: new List<string> { "AutoMapper" })
        };
    }

    public static Dictionary<string, string> CreateCircularDependencyScenario()
    {
        return new Dictionary<string, string>
        {
            ["ProjectX/ProjectX.csproj"] = CreateBasicCSharpProject("ProjectX", 
                projectReferences: new List<string> { "../ProjectY/ProjectY.csproj" }),
            ["ProjectY/ProjectY.csproj"] = CreateBasicCSharpProject("ProjectY", 
                projectReferences: new List<string> { "../ProjectZ/ProjectZ.csproj" }),
            ["ProjectZ/ProjectZ.csproj"] = CreateBasicCSharpProject("ProjectZ", 
                projectReferences: new List<string> { "../ProjectX/ProjectX.csproj" })
        };
    }

    public static Dictionary<string, string> CreateMixedLanguageScenario()
    {
        return new Dictionary<string, string>
        {
            ["VBProject/VBProject.vbproj"] = CreateBasicVBProject("VBProject"),
            ["CSharpProject/CSharpProject.csproj"] = CreateBasicCSharpProject("CSharpProject", 
                projectReferences: new List<string> { "../VBProject/VBProject.vbproj" })
        };
    }

    public static Dictionary<string, string> CreateMissingReferencesScenario()
    {
        return new Dictionary<string, string>
        {
            ["ValidProject/ValidProject.csproj"] = CreateBasicCSharpProject("ValidProject", 
                projectReferences: new List<string> 
                { 
                    "../NonExistentProject/NonExistentProject.csproj",
                    "../AnotherMissingProject/AnotherMissingProject.csproj"
                })
        };
    }
}