using DotNetDependencyTreeBuilder.Models;

namespace DotNetDependencyTreeBuilder.Tests.Models;

public class DependencyGraphTests
{
    private DependencyGraph CreateSampleGraph()
    {
        var graph = new DependencyGraph();
        
        // Create sample projects
        var projectA = new ProjectInfo { FilePath = "/path/to/A.csproj", ProjectName = "A", Type = ProjectType.CSharp };
        var projectB = new ProjectInfo { FilePath = "/path/to/B.csproj", ProjectName = "B", Type = ProjectType.CSharp };
        var projectC = new ProjectInfo { FilePath = "/path/to/C.csproj", ProjectName = "C", Type = ProjectType.CSharp };
        
        graph.AddProject(projectA);
        graph.AddProject(projectB);
        graph.AddProject(projectC);
        
        return graph;
    }

    [Fact]
    public void AddProject_ShouldAddProjectToGraph()
    {
        // Arrange
        var graph = new DependencyGraph();
        var project = new ProjectInfo 
        { 
            FilePath = "/path/to/test.csproj", 
            ProjectName = "Test", 
            Type = ProjectType.CSharp 
        };

        // Act
        graph.AddProject(project);

        // Assert
        Assert.Single(graph.Projects);
        Assert.True(graph.Projects.ContainsKey("/path/to/test.csproj"));
        Assert.Equal(project, graph.Projects["/path/to/test.csproj"]);
        Assert.True(graph.AdjacencyList.ContainsKey("/path/to/test.csproj"));
        Assert.Empty(graph.AdjacencyList["/path/to/test.csproj"]);
    }

    [Fact]
    public void AddProject_ShouldUpdateExistingProject()
    {
        // Arrange
        var graph = new DependencyGraph();
        var project1 = new ProjectInfo 
        { 
            FilePath = "/path/to/test.csproj", 
            ProjectName = "Test1", 
            Type = ProjectType.CSharp 
        };
        var project2 = new ProjectInfo 
        { 
            FilePath = "/path/to/test.csproj", 
            ProjectName = "Test2", 
            Type = ProjectType.VisualBasic 
        };

        // Act
        graph.AddProject(project1);
        graph.AddProject(project2);

        // Assert
        Assert.Single(graph.Projects);
        Assert.Equal("Test2", graph.Projects["/path/to/test.csproj"].ProjectName);
        Assert.Equal(ProjectType.VisualBasic, graph.Projects["/path/to/test.csproj"].Type);
    }

    [Fact]
    public void AddDependency_ShouldAddDependencyRelationship()
    {
        // Arrange
        var graph = CreateSampleGraph();

        // Act
        graph.AddDependency("/path/to/A.csproj", "/path/to/B.csproj");

        // Assert
        Assert.Contains("/path/to/B.csproj", graph.AdjacencyList["/path/to/A.csproj"]);
        Assert.Empty(graph.AdjacencyList["/path/to/B.csproj"]);
    }

    [Fact]
    public void AddDependency_ShouldNotAddDuplicateDependency()
    {
        // Arrange
        var graph = CreateSampleGraph();

        // Act
        graph.AddDependency("/path/to/A.csproj", "/path/to/B.csproj");
        graph.AddDependency("/path/to/A.csproj", "/path/to/B.csproj");

        // Assert
        Assert.Single(graph.AdjacencyList["/path/to/A.csproj"]);
        Assert.Contains("/path/to/B.csproj", graph.AdjacencyList["/path/to/A.csproj"]);
    }

    [Fact]
    public void AddDependency_ShouldCreateAdjacencyListForNewProjects()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        graph.AddDependency("/path/to/new1.csproj", "/path/to/new2.csproj");

        // Assert
        Assert.True(graph.AdjacencyList.ContainsKey("/path/to/new1.csproj"));
        Assert.True(graph.AdjacencyList.ContainsKey("/path/to/new2.csproj"));
        Assert.Contains("/path/to/new2.csproj", graph.AdjacencyList["/path/to/new1.csproj"]);
        Assert.Empty(graph.AdjacencyList["/path/to/new2.csproj"]);
    }

    [Fact]
    public void DetectCircularDependencies_ShouldReturnEmptyForNoCycles()
    {
        // Arrange
        var graph = CreateSampleGraph();
        graph.AddDependency("/path/to/A.csproj", "/path/to/B.csproj");
        graph.AddDependency("/path/to/B.csproj", "/path/to/C.csproj");

        // Act
        var cycles = graph.DetectCircularDependencies();

        // Assert
        Assert.Empty(cycles);
    }

    [Fact]
    public void DetectCircularDependencies_ShouldDetectSimpleCycle()
    {
        // Arrange
        var graph = CreateSampleGraph();
        graph.AddDependency("/path/to/A.csproj", "/path/to/B.csproj");
        graph.AddDependency("/path/to/B.csproj", "/path/to/A.csproj");

        // Act
        var cycles = graph.DetectCircularDependencies();

        // Assert
        Assert.Equal(2, cycles.Count);
        Assert.Contains("/path/to/A.csproj", cycles);
        Assert.Contains("/path/to/B.csproj", cycles);
    }

    [Fact]
    public void DetectCircularDependencies_ShouldDetectComplexCycle()
    {
        // Arrange
        var graph = CreateSampleGraph();
        graph.AddDependency("/path/to/A.csproj", "/path/to/B.csproj");
        graph.AddDependency("/path/to/B.csproj", "/path/to/C.csproj");
        graph.AddDependency("/path/to/C.csproj", "/path/to/A.csproj");

        // Act
        var cycles = graph.DetectCircularDependencies();

        // Assert
        Assert.Equal(3, cycles.Count);
        Assert.Contains("/path/to/A.csproj", cycles);
        Assert.Contains("/path/to/B.csproj", cycles);
        Assert.Contains("/path/to/C.csproj", cycles);
    }

    [Fact]
    public void DetectCircularDependencies_ShouldDetectSelfReference()
    {
        // Arrange
        var graph = CreateSampleGraph();
        graph.AddDependency("/path/to/A.csproj", "/path/to/A.csproj");

        // Act
        var cycles = graph.DetectCircularDependencies();

        // Assert
        Assert.Single(cycles);
        Assert.Contains("/path/to/A.csproj", cycles);
    }

    [Fact]
    public void GetTopologicalOrder_ShouldReturnCorrectOrderForLinearDependencies()
    {
        // Arrange
        var graph = CreateSampleGraph();
        // A depends on B, B depends on C
        graph.AddDependency("/path/to/A.csproj", "/path/to/B.csproj");
        graph.AddDependency("/path/to/B.csproj", "/path/to/C.csproj");

        // Act
        var order = graph.GetTopologicalOrder();

        // Assert
        Assert.Equal(3, order.Count);
        // C should be first (no dependencies)
        Assert.Single(order[0]);
        Assert.Contains("/path/to/C.csproj", order[0]);
        // B should be second (depends on C)
        Assert.Single(order[1]);
        Assert.Contains("/path/to/B.csproj", order[1]);
        // A should be last (depends on B)
        Assert.Single(order[2]);
        Assert.Contains("/path/to/A.csproj", order[2]);
    }

    [Fact]
    public void GetTopologicalOrder_ShouldGroupIndependentProjects()
    {
        // Arrange
        var graph = CreateSampleGraph();
        // No dependencies between projects

        // Act
        var order = graph.GetTopologicalOrder();

        // Assert
        Assert.Single(order);
        Assert.Equal(3, order[0].Count);
        Assert.Contains("/path/to/A.csproj", order[0]);
        Assert.Contains("/path/to/B.csproj", order[0]);
        Assert.Contains("/path/to/C.csproj", order[0]);
    }

    [Fact]
    public void GetTopologicalOrder_ShouldHandleParallelDependencies()
    {
        // Arrange
        var graph = CreateSampleGraph();
        var projectD = new ProjectInfo { FilePath = "/path/to/D.csproj", ProjectName = "D", Type = ProjectType.CSharp };
        graph.AddProject(projectD);
        
        // A and B both depend on C, D depends on A
        graph.AddDependency("/path/to/A.csproj", "/path/to/C.csproj");
        graph.AddDependency("/path/to/B.csproj", "/path/to/C.csproj");
        graph.AddDependency("/path/to/D.csproj", "/path/to/A.csproj");

        // Act
        var order = graph.GetTopologicalOrder();

        // Assert
        Assert.Equal(3, order.Count);
        // C should be first
        Assert.Single(order[0]);
        Assert.Contains("/path/to/C.csproj", order[0]);
        // A and B should be second (both depend on C)
        Assert.Equal(2, order[1].Count);
        Assert.Contains("/path/to/A.csproj", order[1]);
        Assert.Contains("/path/to/B.csproj", order[1]);
        // D should be last (depends on A)
        Assert.Single(order[2]);
        Assert.Contains("/path/to/D.csproj", order[2]);
    }

    [Fact]
    public void GetTopologicalOrder_ShouldReturnPartialOrderForCircularDependencies()
    {
        // Arrange
        var graph = CreateSampleGraph();
        graph.AddDependency("/path/to/A.csproj", "/path/to/B.csproj");
        graph.AddDependency("/path/to/B.csproj", "/path/to/A.csproj");
        // C is independent

        // Act
        var order = graph.GetTopologicalOrder();

        // Assert
        // Should return C first, then stop due to circular dependency between A and B
        Assert.Single(order);
        Assert.Single(order[0]);
        Assert.Contains("/path/to/C.csproj", order[0]);
    }

    [Fact]
    public void GetTopologicalOrder_ShouldHandleEmptyGraph()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        var order = graph.GetTopologicalOrder();

        // Assert
        Assert.Empty(order);
    }

    [Fact]
    public void GetTopologicalOrder_ShouldHandleSingleProject()
    {
        // Arrange
        var graph = new DependencyGraph();
        var project = new ProjectInfo { FilePath = "/path/to/single.csproj", ProjectName = "Single", Type = ProjectType.CSharp };
        graph.AddProject(project);

        // Act
        var order = graph.GetTopologicalOrder();

        // Assert
        Assert.Single(order);
        Assert.Single(order[0]);
        Assert.Contains("/path/to/single.csproj", order[0]);
    }
}