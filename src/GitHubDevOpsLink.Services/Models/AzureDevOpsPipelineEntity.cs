namespace GitHubDevOpsLink.Services.Models;

public class AzureDevOpsPipelineEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? RepositoryUrl { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string? LastBuildStatus { get; set; }
    public string? LastBuildResult { get; set; }
    public int? LastBuildId { get; set; }
    public string? LastBuildNumber { get; set; }
    public DateTime LastFetchedAt { get; set; }
}
