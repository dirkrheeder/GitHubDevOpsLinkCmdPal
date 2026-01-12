using GitHubDevOpsLink.Services.Models;
using Microsoft.TeamFoundation.Build.WebApi;

namespace GitHubDevOpsLink.Services;

public interface IAzureDevOpsService
{
    AzureDevOpsConfiguration? Configuration { get; }
    void SaveConfig(string organization, string project, string token);
    void SaveConfig(string organization, string project, string token, string[]? paths);
    bool IsConfigured();
    Task<bool> ValidateConnectionAsync();
    Task<List<BuildDefinitionReference>> GetPipelinesAsync();
    Task<List<Build>> GetRecentBuildsForPipelineAsync(int definitionId, int top = 5);
    Task<BuildDefinition?> GetPipelineDefinitionAsync(int definitionId);
    Task<List<PipelineViewModel>> GetPipelineViewModelsAsync();
    Task<List<PipelineViewModel>> GetPipelineViewModelsByRepositoryAsync(string repositoryUrl);
}
