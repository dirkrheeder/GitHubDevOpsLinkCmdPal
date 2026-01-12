using GitHubDevOpsLink.Services.Models;
using Octokit;

namespace GitHubDevOpsLink.Services;

public interface IGitHubService
{
    void SaveConfig(string token, string? organization, string[]? teamNames, string[]? topics, string? workFolderPath);
    Task<bool> IsAuthenticatedAsync();
    Task<List<Repository>> GetRepositoriesAsync();
    Task<List<PullRequest>> GetPullRequestsAsync();
    Task<string> GetCurrentUserAsync();

    GitHubConfiguration? Configuration { get; }
}
