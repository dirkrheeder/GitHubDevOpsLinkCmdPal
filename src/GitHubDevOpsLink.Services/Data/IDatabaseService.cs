using GitHubDevOpsLink.Services.Models;
using Octokit;

namespace GitHubDevOpsLink.Services.Data;

public interface IDatabaseService
{
    Task SaveGitHubRepositoriesAsync(IEnumerable<Repository> repositories, string owner);
    Task<List<GitHubRepositoryEntity>> GetGitHubRepositoriesAsync(string owner);
    Task<List<GitHubRepositoryEntity>> GetAllGitHubRepositoriesAsync();
    Task<DateTime?> GetLastGitHubRepositoryFetchTimeAsync(string owner);
    Task ClearGitHubRepositoriesAsync(string owner);
    Task UpdateRepositoryLocalPathAsync(long repositoryId, string? localPath);
    Task<string?> GetRepositoryLocalPathAsync(long repositoryId);
    
    Task SaveGitHubPullRequestsAsync(IEnumerable<PullRequest> pullRequests, string owner);
    Task<List<GitHubPullRequestEntity>> GetGitHubPullRequestsAsync(string owner);
    Task<DateTime?> GetLastGitHubPullRequestFetchTimeAsync(string owner);
    Task ClearGitHubPullRequestsAsync(string owner);
    
    Task SaveAzureDevOpsPipelinesAsync(IEnumerable<PipelineViewModel> pipelines, string organization, string project);
    Task<List<AzureDevOpsPipelineEntity>> GetAzureDevOpsPipelinesAsync(string organization, string project);
    Task<List<AzureDevOpsPipelineEntity>> GetAzureDevOpsPipelinesByRepositoryAsync(string repositoryUrl);
    Task<DateTime?> GetLastAzureDevOpsPipelineFetchTimeAsync(string organization, string project);
    Task ClearAzureDevOpsPipelinesAsync(string organization, string project);
}
