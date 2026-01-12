namespace GitHubDevOpsLink.Services.Models;

public sealed class GitHubConfiguration
{
    public string? Token { get; set; }

    public string? Organization { get; set; }

    public string[]? TeamNames { get; set; }

    public string[]? Topics { get; set; }

    public string? WorkFolderPath { get; set; }
}
