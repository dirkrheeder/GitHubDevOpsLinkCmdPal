using GitHubDevOpsLink.Services.Data;
using GitHubDevOpsLink.Services.Models;
using Octokit;

namespace GitHubDevOpsLink.Services;

public sealed class GitHubCacheService(IDatabaseService databaseService) : IGitHubCacheService
{
    public async Task SaveRepositoriesAsync(IEnumerable<Repository> repositories, string userName)
    {
        await databaseService.SaveGitHubRepositoriesAsync(repositories, userName);
    }

    public async Task<List<GitHubRepositoryEntity>> GetCachedRepositoriesAsync(string userName)
    {
        return await databaseService.GetGitHubRepositoriesAsync(userName);
    }

    public async Task<DateTime?> GetLastFetchTimeAsync(string userName)
    {
        return await databaseService.GetLastGitHubRepositoryFetchTimeAsync(userName);
    }

    public async Task ClearCacheAsync(string userName)
    {
        await databaseService.ClearGitHubRepositoriesAsync(userName);
    }

    public async Task SavePullRequestsAsync(IEnumerable<PullRequest> pullRequests, string userName)
    {
        await databaseService.SaveGitHubPullRequestsAsync(pullRequests, userName);
    }

    public async Task<List<GitHubPullRequestEntity>> GetCachedPullRequestsAsync(string userName)
    {
        return await databaseService.GetGitHubPullRequestsAsync(userName);
    }

    public async Task<DateTime?> GetLastPullRequestFetchTimeAsync(string userName)
    {
        return await databaseService.GetLastGitHubPullRequestFetchTimeAsync(userName);
    }

    public async Task ClearPullRequestCacheAsync(string userName)
    {
        await databaseService.ClearGitHubPullRequestsAsync(userName);
    }
}
