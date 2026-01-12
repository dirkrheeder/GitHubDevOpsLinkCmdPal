using System;
using System.Collections.Generic;
using System.Linq;
using GitHubDevOpsLink.Services;
using GitHubDevOpsLink.Services.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Logging;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class DevOpsPipelinesPage : ListPage
{
    private readonly IAzureDevOpsService _azureDevOpsService;
    private readonly IAzureDevOpsCacheService _azureDevOpsCacheService;
    private readonly ILogger<DevOpsPipelinesPage> _logger;

    public DevOpsPipelinesPage()
    {
        _azureDevOpsService = ServiceContainer.GetService<IAzureDevOpsService>();
        _azureDevOpsCacheService = ServiceContainer.GetService<IAzureDevOpsCacheService>();
        _logger = ServiceContainer.GetService<ILoggerFactory>().CreateLogger<DevOpsPipelinesPage>();

        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Azure DevOps Pipelines";
        Name = "DevOps Pipelines";
        
        _logger.LogDebug("DevOpsPipelinesPage initialized");
    }

    public override IListItem[] GetItems()
    {
        _logger.LogInformation("DevOpsPipelinesPage.GetItems() - Starting to load Azure DevOps pipelines");
        
        // Initialize items list with Configure Azure DevOps as default first item
        var items = new List<IListItem>
        {
            new ListItem(new DevOpsConfigPage())
            {
                Title = "Configure Azure DevOps",
                Subtitle = "Update organization, project, or PAT"
            }
        };
        
        try
        {
            // Check if configured
            _logger.LogDebug("Checking Azure DevOps configuration");
            bool isConfigured = _azureDevOpsService.IsConfigured();

            if (!isConfigured)
            {
                _logger.LogWarning("Azure DevOps is not configured");
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "Azure DevOps Configuration Required",
                    Subtitle = "You need to configure your organization, project, and PAT to access pipelines"
                });
                return items.ToArray();
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

            List<PipelineViewModel> pipelineViewModels;
            bool usingCache = false;

            // Try to get from cache first - always use cache if available
            _logger.LogDebug("Attempting to retrieve cached pipelines");
            var cachedPipelines = _azureDevOpsCacheService.GetCachedPipelinesAsync(organization, project)
                                                         .GetAwaiter()
                                                         .GetResult();
            
            _logger.LogInformation("Found {CachedPipelineCount} pipelines in cache", cachedPipelines.Count);

            if (cachedPipelines.Count > 0)
            {
                // Convert entities to view models
                pipelineViewModels = cachedPipelines.Select(p => _azureDevOpsCacheService.ConvertToViewModel(p)).ToList();
                usingCache = true;
                _logger.LogInformation("Using cached pipeline data");
            }
            else
            {
                _logger.LogInformation("No cache found - fetching fresh pipelines from Azure DevOps API");
                // No cache, fetch fresh and save
                pipelineViewModels = _azureDevOpsService.GetPipelineViewModelsAsync()
                                                       .GetAwaiter()
                                                       .GetResult();

                _logger.LogInformation("Fetched {PipelineCount} pipelines from Azure DevOps API", pipelineViewModels.Count);

                // Save to database
                _logger.LogDebug("Saving {PipelineCount} pipelines to database cache", pipelineViewModels.Count);
                _azureDevOpsCacheService.SavePipelinesAsync(pipelineViewModels, organization, project)
                                      .GetAwaiter()
                                      .GetResult();
                
                lastFetchTime = DateTime.UtcNow;
                _logger.LogInformation("Pipelines saved to database cache at {LastFetchTime}", lastFetchTime.Value);
            }

            if (pipelineViewModels.Count == 0)
            {
                _logger.LogWarning("No pipelines found for organization: {Organization}, project: {Project}", organization, project);
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = $"No Pipelines Found in {organization}/{project}",
                    Subtitle = "No build pipelines found in the configured project"
                });
                return items.ToArray();
            }

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
                        var cfg = _azureDevOpsService.Configuration;
                        string org = cfg?.Organization ?? string.Empty;
                        string proj = cfg?.Project ?? string.Empty;
                        _azureDevOpsCacheService.ClearCacheAsync(org, proj).GetAwaiter().GetResult();
                        _logger.LogInformation("Pipeline cache cleared successfully");
                    })
                    {
                        Name = "Refresh"
                    })
                {
                    Title = "Refresh Pipelines",
                    Subtitle = cacheInfo
                });

            // Add header with organization/project info
            items.Add(
                new ListItem(new NoOpCommand())
                {
                    Title = $"Organization: {organization} | Project: {project}",
                    Subtitle = $"Found {pipelineViewModels.Count} pipeline(s) (stored in SQLite database)"
                });

            // Add pipelines from view models
            _logger.LogDebug("Building list items for {PipelineCount} pipelines", pipelineViewModels.Count);
            foreach (var pipelineViewModel in pipelineViewModels)
            {
                // Build subtitle with path and repository URL
                string subtitle = pipelineViewModel.Subtitle;

                if (!string.IsNullOrEmpty(pipelineViewModel.Path))
                {
                    subtitle = $"{subtitle} | Path: {pipelineViewModel.Path}";
                }

                if (!string.IsNullOrEmpty(pipelineViewModel.RepositoryUrl))
                {
                    subtitle = $"{subtitle} | Repo: {pipelineViewModel.RepositoryUrl}";
                }

                _logger.LogTrace("Adding pipeline: {PipelineName} (ID: {PipelineId}, Path: {Path})", 
                    pipelineViewModel.Name, pipelineViewModel.Id, pipelineViewModel.Path ?? "N/A");

                items.Add(
                    new ListItem(new DevOpsPipelineActionsPage(pipelineViewModel, organization, project))
                    {
                        Title = pipelineViewModel.Name,
                        Subtitle = subtitle
                    });
            }

            _logger.LogInformation("DevOpsPipelinesPage.GetItems() - Completed successfully with {ItemCount} items", items.Count);
            return items.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Azure DevOps pipelines");
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = "Error Loading Pipelines",
                Subtitle = ex.Message
            });
            return items.ToArray();
        }
    }
}