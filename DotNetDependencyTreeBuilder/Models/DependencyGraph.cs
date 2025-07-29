namespace DotNetDependencyTreeBuilder.Models;

/// <summary>
/// Represents a dependency graph of projects using adjacency list representation
/// </summary>
public class DependencyGraph
{
    private readonly Dictionary<string, ProjectInfo> _projects = new();
    private  Dictionary<string, List<string>> _adjacencyList = new();

    /// <summary>
    /// Gets all projects in the graph
    /// </summary>
    public IReadOnlyDictionary<string, ProjectInfo> Projects => _projects;

    /// <summary>
    /// Gets the adjacency list representation of the graph
    /// </summary>
    public Dictionary<string, List<string>> AdjacencyList
   {

      get
      { return _adjacencyList; }
      set
      {
         _adjacencyList = value;
      }

   }


    /// <summary>
    /// Adds a project to the dependency graph
    /// </summary>
    /// <param name="project">The project to add</param>
    public void AddProject(ProjectInfo project)
    {
        _projects[project.FilePath] = project;
        if (!_adjacencyList.ContainsKey(project.FilePath))
        {
            _adjacencyList[project.FilePath] = new List<string>();
        }
    }

    /// <summary>
    /// Adds a dependency relationship between two projects
    /// </summary>
    /// <param name="fromProject">The project that depends on another</param>
    /// <param name="toProject">The project being depended upon</param>
    public void AddDependency(string fromProject, string toProject)
    {
        if (!_adjacencyList.ContainsKey(fromProject))
        {
            _adjacencyList[fromProject] = new List<string>();
        }
        
        if (!_adjacencyList[fromProject].Contains(toProject))
        {
            _adjacencyList[fromProject].Add(toProject);
        }

        // Ensure the target project exists in the adjacency list
        if (!_adjacencyList.ContainsKey(toProject))
        {
            _adjacencyList[toProject] = new List<string>();
        }
    }

    /// <summary>
    /// Detects circular dependencies in the graph using depth-first search
    /// </summary>
    /// <returns>List of project paths involved in circular dependencies</returns>
    public List<string> DetectCircularDependencies()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var circularDependencies = new HashSet<string>();

        foreach (var project in _adjacencyList.Keys)
        {
            if (!visited.Contains(project))
            {
                DetectCyclesDFS(project, visited, recursionStack, circularDependencies, new List<string>());
            }
        }

        return circularDependencies.ToList();
    }

    /// <summary>
    /// Performs topological sorting to determine build order
    /// </summary>
    /// <returns>List of project lists grouped by dependency levels</returns>
    public List<List<string>> GetTopologicalOrder()
    {
        var inDegree = new Dictionary<string, int>();
        var result = new List<List<string>>();

        // Initialize in-degree count for all projects
        foreach (var project in _adjacencyList.Keys)
        {
            inDegree[project] = 0;
        }

        // Calculate in-degrees (how many dependencies each project has)
        // In our adjacency list: A -> [B] means A depends on B
        // So A has an outgoing edge to B, meaning A has a dependency
        foreach (var project in _adjacencyList.Keys)
        {
            inDegree[project] = _adjacencyList[project].Count;
        }

        // Process projects level by level using modified Kahn's algorithm
        while (inDegree.Any(kvp => kvp.Value >= 0))
        {
            // Find projects with no dependencies (in-degree 0)
            var currentLevel = inDegree
                .Where(kvp => kvp.Value == 0)
                .Select(kvp => kvp.Key)
                .ToList();

            if (!currentLevel.Any())
            {
                break; // Circular dependency detected or all remaining have dependencies
            }

            result.Add(currentLevel);

            // Remove processed projects and update in-degrees
            foreach (var project in currentLevel)
            {
                inDegree[project] = -1; // Mark as processed

                // For each project that depends on the current project, reduce its dependency count
                foreach (var otherProject in _adjacencyList.Keys)
                {
                    if (inDegree[otherProject] > 0 && _adjacencyList[otherProject].Contains(project))
                    {
                        inDegree[otherProject]--;
                    }
                }
            }
        }

        return result;
    }

    private void DetectCyclesDFS(string project, HashSet<string> visited, HashSet<string> recursionStack, HashSet<string> circularDependencies, List<string> currentPath)
    {
        visited.Add(project);
        recursionStack.Add(project);
        currentPath.Add(project);

        foreach (var dependency in _adjacencyList[project])
        {
            if (!visited.Contains(dependency))
            {
                DetectCyclesDFS(dependency, visited, recursionStack, circularDependencies, new List<string>(currentPath));
            }
            else if (recursionStack.Contains(dependency))
            {
                // Circular dependency found - add all nodes in the cycle
                var cycleStartIndex = currentPath.IndexOf(dependency);
                for (int i = cycleStartIndex; i < currentPath.Count; i++)
                {
                    circularDependencies.Add(currentPath[i]);
                }
                circularDependencies.Add(dependency);
            }
        }

        recursionStack.Remove(project);
        currentPath.RemoveAt(currentPath.Count - 1);
    }
}