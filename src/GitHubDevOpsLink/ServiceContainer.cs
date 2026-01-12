using System;
using System.Globalization;
using System.IO;
using System.Threading;
using GitHubDevOpsLink.Services;
using GitHubDevOpsLink.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GitHubDevOpsLink;

public static class ServiceContainer
{
    private static IServiceProvider? _serviceProvider;
    private static readonly Lock _lock = new();

    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider != null)
            {
                return _serviceProvider;
            }

            lock (_lock)
            {
                _serviceProvider ??= BuildServiceProvider();
            }

            return _serviceProvider;
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Configure Serilog
        ConfigureSerilog();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
        });

        // Register services
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IAzureDevOpsService, AzureDevOpsService>();
        services.AddSingleton<IAzureDevOpsCacheService, AzureDevOpsCacheService>();
        services.AddSingleton<IGitHubService, GitHubService>();
        services.AddSingleton<IGitHubCacheService, GitHubCacheService>();
        services.AddSingleton<ILocalRepositoryService, LocalRepositoryService>();

        return services.BuildServiceProvider();
    }

    private static void ConfigureSerilog()
    {
        string logsFolder = AppDataPathManager.GetLogsFolderPath();

        string logFilePath = Path.Combine(logsFolder, "logs.txt");

        // Configure Serilog with Debug, Console, and File sinks
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 7) // Keep logs for 7 days
            .CreateLogger();

        Log.Information("ServiceContainer initialized with Serilog logging");
        Log.Information("Log file path: {LogFilePath}", logFilePath);
    }

    public static T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public static void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        Log.CloseAndFlush();
    }
}
