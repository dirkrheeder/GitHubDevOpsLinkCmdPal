using System;
using System.Collections.Generic;
using GitHubDevOpsLink.Services;
using GitHubDevOpsLink.Services.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Logging;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class GitHubPullRequestsPage : ListPage
{
    private readonly IGitHubService _githubService;
    private readonly IGitHubCacheService _githubCacheService;
    private readonly ILogger<GitHubPullRequestsPage> _logger;

    public GitHubPullRequestsPage()
    {
        _githubService = ServiceContainer.GetService<IGitHubService>();
        _githubCacheService = ServiceContainer.GetService<IGitHubCacheService>();
        _logger = ServiceContainer.GetService<ILoggerFactory>().CreateLogger<GitHubPullRequestsPage>();

        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "GitHub Pull Requests";
        Name = "GitHub Pull Requests";
        
        _logger.LogDebug("GitHubPullRequestsPage initialized");
    }

    public override IListItem[] GetItems()
    {
        _logger.LogInformation("GitHubPullRequestsPage.GetItems() - Starting to load GitHub pull requests");
        
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
                    Subtitle = "You need to configure a Personal Access Token to access pull requests"
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
            if (teamNames is { Length: > 0 })
                filters.Add($"Teams: {string.Join(", ", teamNames)}");
            if (topics is { Length: > 0 })
                filters.Add($"Topics: {string.Join(", ", topics)}");
            
            string filterInfo = filters.Count > 0 ? string.Join(" | ", filters) : "No filters applied";
            _logger.LogInformation("GitHub filters: {FilterInfo}", filterInfo);

            // Check database for cached data
            _logger.LogDebug("Checking database for cached pull requests for user: {UserName}", userName);
            var lastFetchTime = _githubCacheService.GetLastPullRequestFetchTimeAsync(userName)
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

            List<GitHubPullRequestEntity> pullRequests;
            bool usingCache = false;

            // Try to get from cache first - always use cache if available
            _logger.LogDebug("Attempting to retrieve cached pull requests");
            var cachedPRs = _githubCacheService.GetCachedPullRequestsAsync(userName)
                                                .GetAwaiter()
                                                .GetResult();
            
            _logger.LogInformation("Found {CachedCount} pull requests in cache", cachedPRs.Count);

            if (cachedPRs.Count > 0)
            {
                _logger.LogInformation("Using cached pull request data without calling GitHub API");
                pullRequests = cachedPRs;
                usingCache = true;
            }
            else
            {
                _logger.LogInformation("No cache found - fetching fresh pull requests from GitHub API");
                // No cache, fetch fresh and save
                var freshPullRequests = _githubService.GetPullRequestsAsync()
                                            .GetAwaiter()
                                            .GetResult();
                
                _logger.LogInformation("Fetched {PRCount} pull requests from GitHub API", freshPullRequests.Count);
                
                // Save to database
                _logger.LogDebug("Saving {PRCount} pull requests to database cache", freshPullRequests.Count);
                _githubCacheService.SavePullRequestsAsync(freshPullRequests, userName)
                                  .GetAwaiter()
                                  .GetResult();
                
                // Get the cached entities back
                pullRequests = _githubCacheService.GetCachedPullRequestsAsync(userName)
                                                .GetAwaiter()
                                                .GetResult();
                
                lastFetchTime = DateTime.UtcNow;
                _logger.LogInformation("Pull requests saved to database cache at {LastFetchTime}", lastFetchTime.Value);
            }

            if (pullRequests.Count == 0)
            {
                _logger.LogWarning("No pull requests found for user: {UserName}", userName);
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = $"No Open Pull Requests Found for {userName}",
                    Subtitle = $"No pull requests match your filters: {filterInfo}"
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
                        _logger.LogInformation("Refresh button clicked - clearing PR cache for user: {UserName}", userName);
                        var githubService = ServiceContainer.GetService<IGitHubService>();
                        var githubCacheService = ServiceContainer.GetService<IGitHubCacheService>();
                        string user = githubService.GetCurrentUserAsync().GetAwaiter().GetResult();
                        githubCacheService.ClearPullRequestCacheAsync(user).GetAwaiter().GetResult();
                        _logger.LogInformation("PR cache cleared successfully for user: {UserName}", user);
                    })
                    {
                        Name = "Refresh"
                    })
                {
                    Title = "Refresh Pull Requests",
                    Subtitle = cacheInfo
                });

            // Add header with user info and filters
            items.Add(
                new ListItem(new NoOpCommand())
                {
                    Title = $"Logged in as: {userName}",
                    Subtitle = $"Found {pullRequests.Count} open pull request(s) | {filterInfo}"
                });

            // Add pull requests
            _logger.LogDebug("Building list items for {PRCount} pull requests", pullRequests.Count);
            foreach (var pr in pullRequests)
            {
                string draftStatus = pr.IsDraft ? "[DRAFT] " : "";
                string updatedAgo = GetTimeAgo(pr.UpdatedAt);

                _logger.LogTrace("Adding pull request: {PRTitle} ({RepoFullName}, Updated: {UpdatedAt})", 
                    pr.Title, pr.RepositoryFullName, pr.UpdatedAt);

                items.Add(
                    new ListItem(new OpenUrlCommand(pr.HtmlUrl))
                    {
                        Title = $"{draftStatus}{pr.RepositoryFullName} #{pr.Number}",
                        Subtitle = $"{pr.Title} | 👤 {pr.Author} | Updated {updatedAgo}"
                    });
            }

            _logger.LogInformation("GitHubPullRequestsPage.GetItems() - Completed successfully with {ItemCount} items", items.Count);
            return items.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading GitHub pull requests");
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = "Error Loading GitHub Pull Requests",
                Subtitle = ex.Message
            });
            return items.ToArray();
        }
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)}w ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
        
        return $"{(int)(timeSpan.TotalDays / 365)}y ago";
    }
}
