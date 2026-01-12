namespace GitHubDevOpsLink.Services.Models;

public sealed class PipelineViewModel
{
    public required string Name { get; init; }

    public required string Subtitle { get; init; }

    public int Id { get; init; }

    public string? RepositoryUrl { get; init; }

    public string? Path { get; init; }

    public int? LastBuildId { get; init; }
}
