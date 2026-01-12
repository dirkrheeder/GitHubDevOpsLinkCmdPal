namespace GitHubDevOpsLink.Services.Models;

public class GitHubPullRequestEntity
{
    public int Id { get; set; }
    public long Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public string RepositoryFullName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDraft { get; set; }
    public string Owner { get; set; } = string.Empty;
    public DateTime LastFetchedAt { get; set; }
}
