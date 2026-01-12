using System.Diagnostics;
using GitHubDevOpsLink.Services.Data;
using GitHubDevOpsLink.Services.Models;

namespace GitHubDevOpsLink.Services;

public sealed class AzureDevOpsCacheService(IDatabaseService databaseService) : IAzureDevOpsCacheService
{
    public async Task SavePipelinesAsync(IEnumerable<PipelineViewModel> pipelines, string organization, string project)
    {
        await databaseService.SaveAzureDevOpsPipelinesAsync(pipelines, organization, project);
    }

    public async Task<List<AzureDevOpsPipelineEntity>> GetCachedPipelinesAsync(string organization, string project)
    {
        Debug.WriteLine("Getting cached pipelines");
        return await databaseService.GetAzureDevOpsPipelinesAsync(organization, project);
    }

    public async Task<List<AzureDevOpsPipelineEntity>> GetCachedPipelinesByRepositoryAsync(string repositoryUrl)
    {
        return await databaseService.GetAzureDevOpsPipelinesByRepositoryAsync(repositoryUrl);
    }

    public async Task<DateTime?> GetLastFetchTimeAsync(string organization, string project)
    {
        return await databaseService.GetLastAzureDevOpsPipelineFetchTimeAsync(organization, project);
    }

    public async Task ClearCacheAsync(string organization, string project)
    {
        await databaseService.ClearAzureDevOpsPipelinesAsync(organization, project);
    }

    public PipelineViewModel ConvertToViewModel(AzureDevOpsPipelineEntity entity)
    {
        return new PipelineViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Subtitle = $"ID: {entity.Id}",
            Path = entity.Path,
            RepositoryUrl = entity.RepositoryUrl,
            LastBuildId = entity.LastBuildId
        };
    }
}
