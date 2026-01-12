namespace GitHubDevOpsLink.Services.Models;

public sealed class AzureDevOpsConfiguration
{
    public string? Organization { get; set; }

    public string? Project { get; set; }

    public string? Token { get; set; }

    public string[]? Paths { get; set; }
}
