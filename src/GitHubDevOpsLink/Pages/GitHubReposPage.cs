using System;
using System.Collections.Generic;
using GitHubDevOpsLink.Services;
using GitHubDevOpsLink.Services.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Logging;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class GitHubReposPage : ListPage
{
    private readonly IGitHubService _githubService;
    private readonly IGitHubCacheService _githubCacheService;
    private readonly ILocalRepositoryService _localRepositoryService;
    private readonly ILogger<GitHubReposPage> _logger;

    public GitHubReposPage()
    {
        _githubService = ServiceContainer.GetService<IGitHubService>();
        _githubCacheService = ServiceContainer.GetService<IGitHubCacheService>();
        _localRepositoryService = ServiceContainer.GetService<ILocalRepositoryService>();
        _logger = ServiceContainer.GetService<ILoggerFactory>().CreateLogger<GitHubReposPage>();

        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "GitHub Repositories";
        Name = "GitHub Repos";
        
        _logger.LogDebug("GitHubReposPage initialized");
    }

    public override IListItem[] GetItems()
    {
        _logger.LogInformation("GitHubReposPage.GetItems() - Starting to load GitHub repositories");
        
        // Initialize items list with Configure GitHub as default first item
        var items = new List<IListItem>
        {
            new ListItem(new GitHubTokenConfigPage())
            {
                Title = "Configure GitHub",
                Subtitle = "Update token, organization, teams, or topics"
            }
        };
        
        try
        {
            // Check if authenticated
            _logger.LogDebug("Checking GitHub authentication status");
            bool isAuthenticated = _githubService.IsAuthenticatedAsync()
                                                .GetAwaiter()
                                                .GetResult();

            if (!isAuthenticated)
            {
                _logger.LogWarning("User is not authenticated with GitHub");
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "GitHub Authentication Required",
                    Subtitle = "You need to configure a Personal Access Token to access repositories"
                });
                return items.ToArray();
            }

            _logger.LogInformation("User is authenticated with GitHub");

            // Get current user
            _logger.LogDebug("Fetching current GitHub user");
            string userName = _githubService.GetCurrentUserAsync()
                                    .GetAwaiter()
                                    .GetResult();
            
            _logger.LogInformation("Current GitHub user: {UserName}", userName);

            // Get configuration
            string? organization = _githubService.Configuration?.Organization;
            string[]? teamNames = _githubService.Configuration?.TeamNames;
            string[]? topics = _githubService.Configuration?.Topics;

            // Build filter description
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(organization))
                filters.Add($"Org: {organization}");
            if (teamNames != null && teamNames.Length > 0)
                filters.Add($"Teams: {string.Join(", ", teamNames)}");
            if (topics != null && topics.Length > 0)
                filters.Add($"Topics: {string.Join(", ", topics)}");
            
            string filterInfo = filters.Count > 0 ? string.Join(" | ", filters) : "No filters applied";
            _logger.LogInformation("GitHub filters: {FilterInfo}", filterInfo);

            // Check database for cached data
            _logger.LogDebug("Checking database for cached repositories for user: {UserName}", userName);
            var lastFetchTime = _githubCacheService.GetLastFetchTimeAsync(userName)
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

            List<GitHubRepositoryEntity> repositories;
            bool usingCache = false;

            // Try to get from cache first - always use cache if available
            _logger.LogDebug("Attempting to retrieve cached repositories");
            var cachedRepos = _githubCacheService.GetCachedRepositoriesAsync(userName)
                                                .GetAwaiter()
                                                .GetResult();
            
            _logger.LogInformation("Found {CachedCount} repositories in cache", cachedRepos.Count);

            if (cachedRepos.Count > 0)
            {
                _logger.LogInformation("Using cached repository data without calling GitHub API");
                // Use cached data directly - no need to fetch from GitHub
                repositories = cachedRepos;
                usingCache = true;
            }
            else
            {
                _logger.LogInformation("No cache found - fetching fresh repositories from GitHub API");
                // No cache, fetch fresh and save
                var freshRepositories = _githubService.GetRepositoriesAsync()
                                            .GetAwaiter()
                                            .GetResult();
                
                _logger.LogInformation("Fetched {RepositoryCount} repositories from GitHub API", freshRepositories.Count);
                
                // Save to database
                _logger.LogDebug("Saving {RepositoryCount} repositories to database cache", freshRepositories.Count);
                _githubCacheService.SaveRepositoriesAsync(freshRepositories, userName)
                                  .GetAwaiter()
                                  .GetResult();
                
                // Get the cached entities back
                repositories = _githubCacheService.GetCachedRepositoriesAsync(userName)
                                                .GetAwaiter()
                                                .GetResult();
                
                lastFetchTime = DateTime.UtcNow;
                _logger.LogInformation("Repositories saved to database cache at {LastFetchTime}", lastFetchTime.Value);
            }

            if (repositories.Count == 0)
            {
                _logger.LogWarning("No repositories found for user: {UserName}", userName);
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = $"No Team Repositories Found for {userName}",
                    Subtitle = $"No repositories match your filters: {filterInfo}"
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
                        _logger.LogInformation("Refresh button clicked - clearing cache for user: {UserName}", userName);
                        var githubService = ServiceContainer.GetService<IGitHubService>();
                        var githubCacheService = ServiceContainer.GetService<IGitHubCacheService>();
                        string user = githubService.GetCurrentUserAsync().GetAwaiter().GetResult();
                        githubCacheService.ClearCacheAsync(user).GetAwaiter().GetResult();
                        _logger.LogInformation("Cache cleared successfully for user: {UserName}", user);
                    })
                    {
                        Name = "Refresh",
                        Result = CommandResult.KeepOpen()
                    })
                {
                    Title = "Refresh Remote Repositories",
                    Subtitle = cacheInfo
                });

            // Add scan local repositories option if work folder is configured
            string? workFolderPath = _githubService.Configuration?.WorkFolderPath;
            if (!string.IsNullOrWhiteSpace(workFolderPath))
            {
                items.Add(
                    new ListItem(
                        new AnonymousCommand(() =>
                        {
                            _logger.LogInformation("Scan Local Repositories clicked - scanning folder: {WorkFolderPath}", workFolderPath);
                            var infoToast = new ToastStatusMessage("Local repositories scan started. This may take a few seconds.");
                            infoToast.Show();

                            var localRepoService = ServiceContainer.GetService<ILocalRepositoryService>();
                            var githubService = ServiceContainer.GetService<IGitHubService>();
                            string user = githubService.GetCurrentUserAsync().GetAwaiter().GetResult();
                            localRepoService.ScanAndLinkRepositoriesAsync(workFolderPath, user).GetAwaiter().GetResult();
                            _logger.LogInformation("Local repositories scan and cleanup completed!");
                            var successToast = new ToastStatusMessage("Local repositories scanned, linked, and invalid links cleaned up successfully!");
                            successToast.Show();
                        })
                        {
                            Name = "Refresh Local Repositories",
                            Result = CommandResult.KeepOpen()
                        })
                    {
                        Title = "Refresh Local Repositories",
                        Subtitle = $"Scan, link, and cleanup repos in: {workFolderPath}"
                    });
            }

            // Add header with user info and filters
            items.Add(
                new ListItem(new NoOpCommand())
                {
                    Title = $"Logged in as: {userName}",
                    Subtitle = $"Found {repositories.Count} team repositories | {filterInfo}"
                });

            // Add repositories
            _logger.LogDebug("Building list items for {RepositoryCount} repositories", repositories.Count);
            foreach (var repo in repositories)
            {
                string visibility = repo.Private ? "Private" : "Public";
                int stars = repo.StargazersCount;
                string language = repo.Language ?? "N/A";

                _logger.LogTrace("Adding repository: {RepoFullName} ({Visibility}, {Stars} stars, {Language})", 
                    repo.FullName, visibility, stars, language);

                items.Add(
                    new ListItem(new GitHubRepoActionsPage(repo))
                    {
                        Title = repo.FullName,
                        Subtitle = $"{visibility} | ⭐ {stars} | Language: {language}"
                    });
            }

            _logger.LogInformation("GitHubReposPage.GetItems() - Completed successfully with {ItemCount} items", items.Count);
            return items.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading GitHub repositories");
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = "Error Loading GitHub Repositories",
                Subtitle = ex.Message
            });
            return items.ToArray();
        }
    }
}