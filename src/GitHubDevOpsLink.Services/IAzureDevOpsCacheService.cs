using GitHubDevOpsLink.Services.Models;

namespace GitHubDevOpsLink.Services;

public interface IAzureDevOpsCacheService
{
    Task SavePipelinesAsync(IEnumerable<PipelineViewModel> pipelines, string organization, string project);
    Task<List<AzureDevOpsPipelineEntity>> GetCachedPipelinesAsync(string organization, string project);
    Task<List<AzureDevOpsPipelineEntity>> GetCachedPipelinesByRepositoryAsync(string repositoryUrl);
    Task<DateTime?> GetLastFetchTimeAsync(string organization, string project);
    Task ClearCacheAsync(string organization, string project);
    PipelineViewModel ConvertToViewModel(AzureDevOpsPipelineEntity entity);
}
