using System;
using System.Collections.Generic;
using System.Linq;
using GitHubDevOpsLink.Services;
using GitHubDevOpsLink.Services.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Logging;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class GitHubRepoPipelinesPage : ListPage
{
    private readonly IAzureDevOpsService _azureDevOpsService;
    private readonly IAzureDevOpsCacheService _azureDevOpsCacheService;
    private readonly ILogger<GitHubRepoPipelinesPage> _logger;
    private readonly GitHubRepositoryEntity _repository;

    public GitHubRepoPipelinesPage(GitHubRepositoryEntity repository)
    {
        _repository = repository;
        _azureDevOpsService = ServiceContainer.GetService<IAzureDevOpsService>();
        _azureDevOpsCacheService = ServiceContainer.GetService<IAzureDevOpsCacheService>();
        _logger = ServiceContainer.GetService<ILoggerFactory>().CreateLogger<GitHubRepoPipelinesPage>();

        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = $"Pipelines for {repository.Name}";
        Name = "Repo Pipelines";
        
        _logger.LogDebug("GitHubRepoPipelinesPage initialized for repository: {RepoName}", repository.Name);
    }

    public override IListItem[] GetItems()
    {
        _logger.LogInformation("GitHubRepoPipelinesPage.GetItems() - Loading pipelines for repository: {RepoFullName}", _repository.FullName);
        
        try
        {
            // Check if Azure DevOps is configured
            _logger.LogDebug("Checking Azure DevOps configuration");
            bool isConfigured = _azureDevOpsService.IsConfigured();

            if (!isConfigured)
            {
                _logger.LogWarning("Azure DevOps is not configured");
                return
                [
                    new ListItem(new DevOpsConfigPage())
                    {
                        Title = "Configure Azure DevOps",
                        Subtitle = "Click here to set up your Azure DevOps organization and PAT"
                    },
                    new ListItem(new NoOpCommand())
                    {
                        Title = "Azure DevOps Configuration Required",
                        Subtitle = "Configure Azure DevOps to see pipelines for this repository"
                    }
                ];
            }

            // Get organization and project from configuration
            var config = _azureDevOpsService.Configuration;
            string organization = config?.Organization ?? string.Empty;
            string project = config?.Project ?? string.Empty;
            
            _logger.LogInformation("Azure DevOps configured - Organization: {Organization}, Project: {Project}", organization, project);

            // Check database for cached pipelines
            _logger.LogDebug("Checking database for cached pipelines");
            var lastFetchTime = _azureDevOpsCacheService.GetLastFetchTimeAsync(organization, project)
                                                        .GetAwaiter()
                                                        .GetResult();

            if (lastFetchTime.HasValue)
            {
                _logger.LogDebug("Last fetch time from cache: {LastFetchTime}", lastFetchTime.Value);
            }
            else
            {
                _logger.LogDebug("No previous fetch time found in cache");
            }

            List<PipelineViewModel> pipelines;
            bool usingCache = false;

            // Try to get from cache first - always use cache if available
            _logger.LogDebug("Attempting to retrieve cached pipelines for repository: {RepoUrl}", _repository.HtmlUrl);
            var cachedPipelines = _azureDevOpsCacheService
                                  .GetCachedPipelinesByRepositoryAsync(_repository.HtmlUrl)
                                  .GetAwaiter()
                                  .GetResult();
            
            _logger.LogInformation("Found {CachedPipelineCount} cached pipelines for repository", cachedPipelines.Count);

            if (cachedPipelines.Count > 0 || lastFetchTime.HasValue)
            {
                // Convert entities to view models
                pipelines = cachedPipelines.Select(p => _azureDevOpsCacheService.ConvertToViewModel(p)).ToList();
                usingCache = true;
                _logger.LogInformation("Using cached pipeline data");
            }
            else
            {
                _logger.LogInformation("No cache found - fetching fresh pipelines from Azure DevOps API");
                // No cache, fetch fresh and save
                var allPipelines = _azureDevOpsService.GetPipelineViewModelsAsync()
                                                      .GetAwaiter()
                                                      .GetResult();
                
                _logger.LogInformation("Fetched {TotalPipelineCount} total pipelines from Azure DevOps API", allPipelines.Count);
                
                // Save to database
                _logger.LogDebug("Saving pipelines to database cache");
                _azureDevOpsCacheService.SavePipelinesAsync(allPipelines, organization, project)
                                        .GetAwaiter()
                                        .GetResult();
                
                // Filter by repository
                pipelines = _azureDevOpsService.GetPipelineViewModelsByRepositoryAsync(_repository.HtmlUrl)
                                               .GetAwaiter()
                                               .GetResult();
                lastFetchTime = DateTime.UtcNow;
                _logger.LogInformation("Pipelines saved to database cache. Found {FilteredPipelineCount} pipelines for this repository", pipelines.Count);
            }

            var items = new List<IListItem>();

            // Add refresh option
            string cacheInfo = lastFetchTime.HasValue 
                ? $"Last updated: {lastFetchTime.Value.ToLocalTime():HH:mm:ss}" 
                : "Never fetched";
            
            if (usingCache)
            {
                cacheInfo += " (from cache - click to refresh)";
            }

            _logger.LogDebug("Cache info: {CacheInfo}", cacheInfo);

            items.Add(
                new ListItem(
                    new AnonymousCommand(() =>
                    {
                        _logger.LogInformation("Refresh button clicked - clearing pipeline cache");
                        var devOpsService = ServiceContainer.GetService<IAzureDevOpsService>();
                        var cacheService = ServiceContainer.GetService<IAzureDevOpsCacheService>();
                        var cfg = devOpsService.Configuration;
                        string org = cfg?.Organization ?? string.Empty;
                        string proj = cfg?.Project ?? string.Empty;
                        cacheService.ClearCacheAsync(org, proj).GetAwaiter().GetResult();
                        _logger.LogInformation("Pipeline cache cleared successfully");
                    })
                    {
                        Name = "Refresh"
                    })
                {
                    Title = "Refresh Pipelines",
                    Subtitle = cacheInfo
                });

            // Add repository info header
            items.Add(
                new ListItem(new NoOpCommand())
                {
                    Title = $"Repository: {_repository.FullName}",
                    Subtitle = $"Found {pipelines.Count} associated pipeline(s) (stored in SQLite database)"
                });

            if (pipelines.Count == 0)
            {
                _logger.LogInformation("No pipelines found for repository: {RepoFullName}", _repository.FullName);
                items.Add(
                    new ListItem(new NoOpCommand())
                    {
                        Title = "No Pipelines Found",
                        Subtitle = "No Azure DevOps pipelines are associated with this repository"
                    });
            }
            else
            {
                _logger.LogDebug("Building list items for {PipelineCount} pipelines", pipelines.Count);
                // Add each pipeline with navigation to PipelineActionsPage
                foreach (var pipeline in pipelines)
                {
                    string subtitle = pipeline.Subtitle;

                    if (!string.IsNullOrEmpty(pipeline.Path))
                    {
                        subtitle = $"{subtitle} | Path: {pipeline.Path}";
                    }

                    _logger.LogTrace("Adding pipeline: {PipelineName} (ID: {PipelineId})", pipeline.Name, pipeline.Id);

                    items.Add(
                        new ListItem(new DevOpsPipelineActionsPage(pipeline, organization, project))
                        {
                            Title = pipeline.Name,
                            Subtitle = subtitle
                        });
                }
            }

            _logger.LogInformation("GitHubRepoPipelinesPage.GetItems() - Completed successfully with {ItemCount} items", items.Count);
            return items.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pipelines for repository: {RepoFullName}", _repository.FullName);
            return
            [
                new ListItem(new NoOpCommand())
                {
                    Title = "Error Loading Pipelines",
                    Subtitle = ex.Message
                }
            ];
        }
    }
}