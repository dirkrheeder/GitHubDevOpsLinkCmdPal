using System.Globalization;
using GitHubDevOpsLink.Services;
using GitHubDevOpsLink.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

// Configure Serilog
ConfigureSerilog();

// Build service provider
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSerilog(dispose: true));
services.AddSingleton<IDatabaseService, DatabaseService>();
services.AddSingleton<IAzureDevOpsService, AzureDevOpsService>();
services.AddSingleton<IAzureDevOpsCacheService, AzureDevOpsCacheService>();
services.AddSingleton<IGitHubService, GitHubService>();
services.AddSingleton<IGitHubCacheService, GitHubCacheService>();
services.AddSingleton<ILocalRepositoryService, LocalRepositoryService>();

var serviceProvider = services.BuildServiceProvider();

var azureDevOpsService = serviceProvider.GetRequiredService<IAzureDevOpsService>();
var azureDevOpsCacheService = serviceProvider.GetRequiredService<IAzureDevOpsCacheService>();
var githubService = serviceProvider.GetRequiredService<IGitHubService>();


await githubService.IsAuthenticatedAsync();
await githubService.GetRepositoriesAsync();
//await DevOps();

async Task DevOps()
{
    var devOpsViewModels = await azureDevOpsService.GetPipelineViewModelsAsync();

    await azureDevOpsCacheService.SavePipelinesAsync(devOpsViewModels, "test-org", "test-project");

    Console.WriteLine("Azure DevOps Pipelines:");
    foreach (var pipeline in devOpsViewModels)
    {
        Console.WriteLine($"- {pipeline.Name} (ID: {pipeline.Id})");
    }
}

static void ConfigureSerilog()
{
    string logFilePath = Path.Combine(AppDataPathManager.GetLogsFolderPath(), "logs.txt");

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.InvariantCulture)
        .WriteTo.File(
            logFilePath,
            rollingInterval: RollingInterval.Day,
            formatProvider: CultureInfo.InvariantCulture,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            retainedFileCountLimit: 7)
        .CreateLogger();

    Log.Information("Console application started");
}