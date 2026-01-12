using System;
using System.Collections.Generic;
using System.Linq;
using GitHubDevOpsLink.Services;
using GitHubDevOpsLink.Services.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Logging;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class GitHubRepoPullRequestsPage : ListPage
{
    private readonly IGitHubService _githubService;
    private readonly IGitHubCacheService _githubCacheService;
    private readonly ILogger<GitHubRepoPullRequestsPage> _logger;
    private readonly GitHubRepositoryEntity _repository;

    public GitHubRepoPullRequestsPage(GitHubRepositoryEntity repository)
    {
        _repository = repository;
        _githubService = ServiceContainer.GetService<IGitHubService>();
        _githubCacheService = ServiceContainer.GetService<IGitHubCacheService>();
        _logger = ServiceContainer.GetService<ILoggerFactory>().CreateLogger<GitHubRepoPullRequestsPage>();

        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = $"Pull Requests for {repository.Name}";
        Name = "Repo Pull Requests";
        
        _logger.LogDebug("GitHubRepoPullRequestsPage initialized for repository: {RepoName}", repository.Name);
    }

    public override IListItem[] GetItems()
    {
        _logger.LogInformation("GitHubRepoPullRequestsPage.GetItems() - Loading pull requests for repository: {RepoFullName}", _repository.FullName);
        
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
                return
                [
                    new ListItem(new GitHubTokenConfigPage())
                    {
                        Title = "⚙️ Configure GitHub",
                        Subtitle = "Click here to set up your GitHub configuration"
                    },
                    new ListItem(new NoOpCommand())
                    {
                        Title = "GitHub Authentication Required",
                        Subtitle = "Configure GitHub to see pull requests for this repository"
                    }
                ];
            }

            // Get current user
            string userName = _githubService.GetCurrentUserAsync()
                                            .GetAwaiter()
                                            .GetResult();
            
            _logger.LogInformation("Current GitHub user: {UserName}", userName);

            // Check database for cached pull requests
            _logger.LogDebug("Checking database for cached pull requests");
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

            List<GitHubPullRequestEntity> allPullRequests;
            bool usingCache = false;

            // Try to get from cache first
            _logger.LogDebug("Attempting to retrieve cached pull requests for user: {UserName}", userName);
            var cachedPRs = _githubCacheService.GetCachedPullRequestsAsync(userName)
                                               .GetAwaiter()
                                               .GetResult();
            
            _logger.LogInformation("Found {CachedCount} cached pull requests", cachedPRs.Count);

            if (cachedPRs.Count > 0)
            {
                allPullRequests = cachedPRs;
                usingCache = true;
                _logger.LogInformation("Using cached pull request data");
            }
            else
            {
                _logger.LogInformation("No cache found - fetching fresh pull requests from GitHub API");
                var freshPullRequests = _githubService.GetPullRequestsAsync()
                                                      .GetAwaiter()
                                                      .GetResult();
                
                _logger.LogInformation("Fetched {PRCount} pull requests from GitHub API", freshPullRequests.Count);
                
                // Save to database
                _logger.LogDebug("Saving pull requests to database cache");
                _githubCacheService.SavePullRequestsAsync(freshPullRequests, userName)
                                   .GetAwaiter()
                                   .GetResult();
                
                allPullRequests = _githubCacheService.GetCachedPullRequestsAsync(userName)
                                                     .GetAwaiter()
                                                     .GetResult();
                lastFetchTime = DateTime.UtcNow;
                _logger.LogInformation("Pull requests saved to database cache");
            }

            // Filter by repository
            var pullRequests = allPullRequests
                               .Where(pr => pr.RepositoryFullName.Equals(_repository.FullName, StringComparison.OrdinalIgnoreCase))
                               .ToList();

            _logger.LogInformation("Found {FilteredPRCount} pull requests for repository: {RepoFullName}", pullRequests.Count, _repository.FullName);

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
                        _logger.LogInformation("Refresh button clicked - clearing PR cache");
                        var githubService = ServiceContainer.GetService<IGitHubService>();
                        var cacheService = ServiceContainer.GetService<IGitHubCacheService>();
                        string user = githubService.GetCurrentUserAsync().GetAwaiter().GetResult();
                        cacheService.ClearPullRequestCacheAsync(user).GetAwaiter().GetResult();
                        _logger.LogInformation("PR cache cleared successfully");
                    })
                    {
                        Name = "Refresh",
                        Result = CommandResult.KeepOpen()
                    })
                {
                    Title = "Refresh Pull Requests",
                    Subtitle = cacheInfo
                });

            // Add repository info header
            items.Add(
                new ListItem(new NoOpCommand())
                {
                    Title = $"Repository: {_repository.FullName}",
                    Subtitle = $"Found {pullRequests.Count} open pull request(s)"
                });

            if (pullRequests.Count == 0)
            {
                _logger.LogInformation("No pull requests found for repository: {RepoFullName}", _repository.FullName);
                items.Add(
                    new ListItem(new NoOpCommand())
                    {
                        Title = "No Open Pull Requests",
                        Subtitle = "No open pull requests found for this repository"
                    });
            }
            else
            {
                _logger.LogDebug("Building list items for {PRCount} pull requests", pullRequests.Count);
                foreach (var pr in pullRequests)
                {
                    string draftStatus = pr.IsDraft ? "[DRAFT] " : "";
                    string updatedAgo = GetTimeAgo(pr.UpdatedAt);

                    _logger.LogTrace("Adding pull request: {PRTitle} (#{PRNumber})", pr.Title, pr.Number);

                    items.Add(
                        new ListItem(new OpenUrlCommand(pr.HtmlUrl))
                        {
                            Title = $"{draftStatus}#{pr.Number}: {pr.Title}",
                            Subtitle = $"👤 {pr.Author} | Updated {updatedAgo}"
                        });
                }
            }

            _logger.LogInformation("GitHubRepoPullRequestsPage.GetItems() - Completed successfully with {ItemCount} items", items.Count);
            return items.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pull requests for repository: {RepoFullName}", _repository.FullName);
            return
            [
                new ListItem(new NoOpCommand())
                {
                    Title = "Error Loading Pull Requests",
                    Subtitle = ex.Message
                }
            ];
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