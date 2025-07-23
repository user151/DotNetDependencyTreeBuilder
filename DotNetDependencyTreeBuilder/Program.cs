using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNetDependencyTreeBuilder.Interfaces;
using DotNetDependencyTreeBuilder.Services;
using DotNetDependencyTreeBuilder.Parsers;
using DotNetDependencyTreeBuilder.Models;
using DotNetDependencyTreeBuilder.Exceptions;
using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetDependencyTreeBuilder;

public class Program
{
    // Application exit codes for build automation integration
    private const int ExitCodeSuccess = 0;
    private const int ExitCodeWarning = 1;
    private const int ExitCodeError = 2;
    private const int ExitCodeCriticalError = 3;

    static async Task<int> Main(string[] args)
    {
        try
        {
            // Set up global exception handling
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Set up command line interface
            var rootCommand = CreateRootCommand();
            
            var exitCode = await rootCommand.InvokeAsync(args);
            
            // Ensure proper exit code is returned
            return exitCode;
        }
        catch (Exception ex)
        {
            // Global exception handler - last resort
            Console.Error.WriteLine($"Critical application error: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            return ExitCodeCriticalError;
        }
    }
    
    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Analyzes .NET project dependencies and generates build order");
        
        // Define arguments
        var sourceDirectoryArgument = new Argument<string>(
            name: "source-directory",
            description: "The root directory to scan for projects")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        
        // Add validation for source directory
        sourceDirectoryArgument.AddValidator(result =>
        {
            var value = result.GetValueForArgument(sourceDirectoryArgument);
            if (string.IsNullOrWhiteSpace(value))
            {
                result.ErrorMessage = "Source directory cannot be empty";
                return;
            }
            
            if (!Directory.Exists(value))
            {
                result.ErrorMessage = $"Source directory does not exist: {value}";
                return;
            }
        });
        
        // Define options
        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path (optional)");
        
        // Add validation for output path
        outputOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(outputOption);
            if (!string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    var directory = Path.GetDirectoryName(Path.GetFullPath(value));
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        result.ErrorMessage = $"Output directory does not exist: {directory}";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = $"Invalid output path: {ex.Message}";
                    return;
                }
            }
        });
        
        var formatOption = new Option<OutputFormat>(
            aliases: new[] { "--format", "-f" },
            description: "Output format: text|json (default: text)")
        {
            IsRequired = false
        };
        formatOption.SetDefaultValue(OutputFormat.Text);
        
        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Enable verbose logging");
        
        var includePackagesOption = new Option<bool>(
            aliases: new[] { "--include-packages" },
            description: "Include package dependencies in output");
        
        var detectCyclesOnlyOption = new Option<bool>(
            aliases: new[] { "--detect-cycles-only" },
            description: "Only check for circular dependencies");
        
        // Add arguments and options to command
        rootCommand.AddArgument(sourceDirectoryArgument);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(formatOption);
        rootCommand.AddOption(verboseOption);
        rootCommand.AddOption(includePackagesOption);
        rootCommand.AddOption(detectCyclesOnlyOption);
        
        // Set up command handler
        rootCommand.SetHandler(async (sourceDirectory, outputPath, format, verbose, includePackages, detectCyclesOnly) =>
        {
            var exitCode = await ExecuteApplicationAsync(sourceDirectory, outputPath, format, verbose, includePackages, detectCyclesOnly);
            Environment.Exit(exitCode);
        }, sourceDirectoryArgument, outputOption, formatOption, verboseOption, includePackagesOption, detectCyclesOnlyOption);
        
        return rootCommand;
    }
    
    /// <summary>
    /// Executes the main application logic with comprehensive error handling
    /// </summary>
    private static async Task<int> ExecuteApplicationAsync(
        string sourceDirectory, 
        string? outputPath, 
        OutputFormat format, 
        bool verbose, 
        bool includePackages, 
        bool detectCyclesOnly)
    {
        IServiceProvider? serviceProvider = null;
        ILogger<Program>? logger = null;

        try
        {
            // Set up dependency injection container with logging configuration
            var services = new ServiceCollection();
            ConfigureServices(services, verbose);
            
            serviceProvider = services.BuildServiceProvider();
            logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Starting .NET Dependency Tree Builder");
            logger.LogInformation("Source Directory: {SourceDirectory}", sourceDirectory);
            logger.LogInformation("Output Path: {OutputPath}", outputPath ?? "Console");
            logger.LogInformation("Format: {Format}", format);
            logger.LogInformation("Verbose: {Verbose}", verbose);
            logger.LogInformation("Include Packages: {IncludePackages}", includePackages);
            logger.LogInformation("Detect Cycles Only: {DetectCyclesOnly}", detectCyclesOnly);

            // Get the main service and execute analysis
            var dependencyTreeService = serviceProvider.GetRequiredService<IDependencyTreeService>();
            
            var exitCode = await dependencyTreeService.AnalyzeDependenciesAsync(sourceDirectory, outputPath, verbose);
            
            logger.LogInformation("Application completed with exit code: {ExitCode}", exitCode);
            return exitCode;
        }
        catch (ProjectAnalysisException ex)
        {
            logger?.LogError(ex, "Project analysis error: {Message}", ex.Message);
            Console.Error.WriteLine($"Project analysis error: {ex.Message}");
            return ExitCodeError;
        }
        catch (ProjectParsingException ex)
        {
            logger?.LogError(ex, "Project parsing error in {ProjectPath}: {Message}", ex.ProjectPath, ex.Message);
            Console.Error.WriteLine($"Project parsing error in {ex.ProjectPath}: {ex.Message}");
            return ExitCodeError;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.LogError(ex, "Access denied: {Message}", ex.Message);
            Console.Error.WriteLine($"Access denied: {ex.Message}");
            return ExitCodeError;
        }
        catch (DirectoryNotFoundException ex)
        {
            logger?.LogError(ex, "Directory not found: {Message}", ex.Message);
            Console.Error.WriteLine($"Directory not found: {ex.Message}");
            return ExitCodeError;
        }
        catch (ArgumentException ex)
        {
            logger?.LogError(ex, "Invalid argument: {Message}", ex.Message);
            Console.Error.WriteLine($"Invalid argument: {ex.Message}");
            return ExitCodeError;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Critical error during application execution: {Message}", ex.Message);
            Console.Error.WriteLine($"Critical error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return ExitCodeCriticalError;
        }
        finally
        {
            // Ensure proper cleanup of service provider
            if (serviceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }
    }

    /// <summary>
    /// Configures dependency injection services with proper lifetime management
    /// </summary>
    private static void ConfigureServices(IServiceCollection services, bool verbose = false)
    {
        // Configure structured logging with appropriate levels
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            
            // Set logging levels based on verbose flag
            if (verbose)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter("Microsoft", LogLevel.Warning);
                builder.AddFilter("System", LogLevel.Warning);
            }
            else
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("Microsoft", LogLevel.Error);
                builder.AddFilter("System", LogLevel.Error);
            }
        });
        
        // Register core services with appropriate lifetimes
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IProjectDiscoveryService, ProjectDiscoveryService>();
        services.AddSingleton<IDependencyAnalysisService, DependencyAnalysisService>();
        services.AddSingleton<IDependencyTreeService, DependencyTreeService>();
        
        // Register parsers as singletons since they are stateless
        services.AddSingleton<IProjectFileParser, CSharpProjectParser>();
        services.AddSingleton<IProjectFileParser, VBProjectParser>();
        
        // Register parser collection for dependency injection
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProjectFileParser, CSharpProjectParser>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProjectFileParser, VBProjectParser>());
    }

    /// <summary>
    /// Global unhandled exception handler for application domain
    /// </summary>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        Console.Error.WriteLine($"Unhandled exception: {exception?.Message}");
        Console.Error.WriteLine($"Stack trace: {exception?.StackTrace}");
        
        if (e.IsTerminating)
        {
            Console.Error.WriteLine("Application is terminating due to unhandled exception.");
            Environment.Exit(ExitCodeCriticalError);
        }
    }

    /// <summary>
    /// Global unobserved task exception handler
    /// </summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Console.Error.WriteLine($"Unobserved task exception: {e.Exception.Message}");
        Console.Error.WriteLine($"Stack trace: {e.Exception.StackTrace}");
        
        // Mark exception as observed to prevent application termination
        e.SetObserved();
        
        // Log the exception but don't terminate the application
        // The application should handle task exceptions gracefully
    }
}
