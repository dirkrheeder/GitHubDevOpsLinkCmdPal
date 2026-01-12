using GitHubDevOpsLink.Services.Models;
using Octokit;

namespace GitHubDevOpsLink.Services;

public interface IGitHubCacheService
{
    Task SaveRepositoriesAsync(IEnumerable<Repository> repositories, string userName);
    Task<List<GitHubRepositoryEntity>> GetCachedRepositoriesAsync(string userName);
    Task<DateTime?> GetLastFetchTimeAsync(string userName);
    Task ClearCacheAsync(string userName);
    
    Task SavePullRequestsAsync(IEnumerable<PullRequest> pullRequests, string userName);
    Task<List<GitHubPullRequestEntity>> GetCachedPullRequestsAsync(string userName);
    Task<DateTime?> GetLastPullRequestFetchTimeAsync(string userName);
    Task ClearPullRequestCacheAsync(string userName);
}
