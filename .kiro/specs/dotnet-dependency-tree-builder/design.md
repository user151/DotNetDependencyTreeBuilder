# Design Document

## Overview

The .NET Dependency Tree Builder is a console application that analyzes C# and Visual Basic project files within a directory structure to build a comprehensive dependency tree. The application uses XML parsing to extract project references and package dependencies, constructs a directed graph of project relationships, performs topological sorting to determine build order, and outputs the results in a format suitable for build automation scripts.

## Architecture

The application follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────┐
│           Console Layer             │
│  (Command-line parsing, output)     │
└─────────────────────────────────────┘
                    │
┌─────────────────────────────────────┐
│          Service Layer              │
│  (Orchestration, business logic)    │
└─────────────────────────────────────┘
                    │
┌─────────────────────────────────────┐
│           Core Layer                │
│  (Models, dependency analysis)      │
└─────────────────────────────────────┘
                    │
┌─────────────────────────────────────┐
│       Infrastructure Layer          │
│  (File I/O, project file parsing)   │
└─────────────────────────────────────┘
```

## Components and Interfaces

### 1. Console Layer Components

**Program.cs**
- Entry point for the application
- Command-line argument parsing using System.CommandLine
- Coordinates the overall application flow

**IConsoleOutput**
- Interface for console and file output operations
- Implementations: ConsoleOutput, FileOutput, CompositeOutput

### 2. Service Layer Components

**IDependencyTreeService**
- Main orchestration service
- Coordinates project discovery, analysis, and output generation

**IProjectDiscoveryService**
- Responsible for finding project files in directory structures
- Handles recursive directory traversal

**IDependencyAnalysisService**
- Analyzes project dependencies and builds dependency graph
- Performs topological sorting for build order determination

### 3. Core Layer Components

**ProjectInfo**
- Represents a discovered project with metadata
- Properties: FilePath, ProjectName, ProjectType, Dependencies, PackageReferences

**ProjectDependency**
- Represents a dependency relationship between projects
- Properties: SourceProject, TargetProject, DependencyType

**DependencyGraph**
- Graph structure representing project relationships
- Methods: AddProject, AddDependency, DetectCycles, GetTopologicalOrder

**BuildOrder**
- Represents the final build order with grouping information
- Properties: OrderedProjects, ParallelGroups, CircularDependencies

### 4. Infrastructure Layer Components

**IProjectFileParser**
- Interface for parsing different project file types
- Implementations: CSharpProjectParser, VBProjectParser

**IFileSystemService**
- Abstraction for file system operations
- Enables testability and mocking

## Data Models

### ProjectInfo Model
```csharp
public class ProjectInfo
{
    public string FilePath { get; set; }
    public string ProjectName { get; set; }
    public ProjectType Type { get; set; }
    public List<ProjectDependency> ProjectReferences { get; set; }
    public List<PackageReference> PackageReferences { get; set; }
    public string TargetFramework { get; set; }
}

public enum ProjectType
{
    CSharp,
    VisualBasic
}
```

### ProjectDependency Model
```csharp
public class ProjectDependency
{
    public string ReferencedProjectPath { get; set; }
    public string ReferencedProjectName { get; set; }
    public bool IsResolved { get; set; }
}
```

### DependencyGraph Model
```csharp
public class DependencyGraph
{
    private Dictionary<string, ProjectInfo> _projects;
    private Dictionary<string, List<string>> _adjacencyList;
    
    public void AddProject(ProjectInfo project);
    public void AddDependency(string fromProject, string toProject);
    public List<string> DetectCircularDependencies();
    public List<List<string>> GetTopologicalOrder();
}
```

### BuildOrder Model
```csharp
public class BuildOrder
{
    public List<List<ProjectInfo>> BuildLevels { get; set; }
    public List<string> CircularDependencies { get; set; }
    public bool HasCircularDependencies => CircularDependencies.Any();
}
```

## Error Handling

### Error Categories

1. **File System Errors**
   - Directory not found
   - Access denied
   - File corruption

2. **Project File Parsing Errors**
   - Malformed XML
   - Missing required elements
   - Unsupported project format

3. **Dependency Resolution Errors**
   - Missing project references
   - Circular dependencies
   - Invalid project paths

### Error Handling Strategy

- Use Result pattern for operations that can fail
- Log errors with appropriate severity levels
- Continue processing when possible (graceful degradation)
- Provide detailed error messages with context
- Exit with appropriate error codes for build automation

### Exception Handling
```csharp
public class ProjectAnalysisException : Exception
{
    public string ProjectPath { get; }
    public ProjectAnalysisException(string projectPath, string message, Exception innerException)
        : base(message, innerException)
    {
        ProjectPath = projectPath;
    }
}
```

## Testing Strategy

### Unit Testing
- Test each component in isolation using mocks
- Focus on business logic and edge cases
- Use xUnit as the testing framework
- Achieve minimum 80% code coverage

### Integration Testing
- Test file system operations with temporary directories
- Test XML parsing with sample project files
- Test end-to-end scenarios with known project structures

### Test Data Strategy
- Create sample project files for different scenarios
- Include projects with various dependency patterns
- Test with both C# and VB.NET projects
- Include edge cases like circular dependencies

### Testing Tools
- xUnit for unit and integration tests
- Moq for mocking dependencies
- FluentAssertions for readable test assertions
- Coverlet for code coverage analysis

## Implementation Considerations

### Performance
- Use async/await for file I/O operations
- Implement parallel processing for project file parsing
- Cache parsed project information to avoid re-parsing
- Use efficient graph algorithms for dependency analysis

### Extensibility
- Plugin architecture for supporting additional project types
- Configurable output formats (JSON, XML, CSV)
- Support for custom dependency resolution rules

### Cross-Platform Compatibility
- Use .NET 6+ for cross-platform support
- Handle path separators correctly across operating systems
- Use appropriate file system APIs

### Memory Management
- Stream large directory traversals to avoid memory issues
- Dispose of file handles properly
- Use appropriate data structures for graph representation

## Command-Line Interface Design

```
dotnet-dependency-tree-builder [options] <source-directory>

Arguments:
  source-directory    The root directory to scan for projects

Options:
  -o, --output <file>     Output file path (optional)
  -f, --format <format>   Output format: text|json|xml (default: text)
  -v, --verbose          Enable verbose logging
  --include-packages     Include package dependencies in output
  --detect-cycles-only   Only check for circular dependencies
  -h, --help             Show help information
```

## Output Format Specification

### Text Format (Default)
```
Build Order Analysis Results
============================

Projects Found: 5
Dependencies Analyzed: 8
Circular Dependencies: None

Build Order:
Level 1: (Can be built in parallel)
  - /path/to/Core.Library/Core.Library.csproj
  - /path/to/Utilities/Utilities.csproj

Level 2:
  - /path/to/Business.Logic/Business.Logic.csproj

Level 3:
  - /path/to/Web.API/Web.API.csproj
  - /path/to/Console.App/Console.App.csproj
```

### JSON Format
```json
{
  "summary": {
    "projectsFound": 5,
    "dependenciesAnalyzed": 8,
    "circularDependencies": []
  },
  "buildOrder": [
    {
      "level": 1,
      "projects": [
        "/path/to/Core.Library/Core.Library.csproj",
        "/path/to/Utilities/Utilities.csproj"
      ]
    }
  ]
}
```