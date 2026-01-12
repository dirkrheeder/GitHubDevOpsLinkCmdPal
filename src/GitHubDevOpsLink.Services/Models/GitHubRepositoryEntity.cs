namespace GitHubDevOpsLink.Services.Models;

public class GitHubRepositoryEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public bool Private { get; set; }
    public int StargazersCount { get; set; }
    public string? Language { get; set; }
    public string Owner { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastFetchedAt { get; set; }
    public string? LocalPath { get; set; }
}
