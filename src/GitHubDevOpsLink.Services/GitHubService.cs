using System.Text.Json;
using GitHubDevOpsLink.Services.Models;
using Microsoft.Extensions.Logging;
using Octokit;
using GitHubJsonContext = GitHubDevOpsLink.Services.Models.GitHubJsonContext;

namespace GitHubDevOpsLink.Services;

public sealed class GitHubService : IGitHubService
{
    private readonly ILogger<GitHubService> _logger;
    private GitHubClient _client;
    private GitHubConfiguration? _config;

    public GitHubService(ILogger<GitHubService> logger)
    {
        _logger = logger;
        _config = LoadConfiguration();
        _client = GetClient();
    }

    public GitHubConfiguration? Configuration => _config;

    private GitHubConfiguration? LoadConfiguration()
    {
        try
        {
            string configPath = AppDataPathManager.GetGitHubConfigPath();
            _logger.LogInformation("Loading GitHub configuration from: {ConfigPath}", configPath);

            if (File.Exists(configPath))
            {
                string jsonString = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize(jsonString, GitHubJsonContext.Default.GitHubConfiguration);
            }

            _logger.LogWarning("GitHub configuration file not found at: {ConfigPath}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load GitHub configuration");
        }

        return null;
    }

    public void SaveConfig(string token, string? organization, string[]? teamNames, string[]? topics, string? workFolderPath)
    {
        var config = new GitHubConfiguration
        {
            Token = token,
            Organization = organization,
            TeamNames = teamNames,
            Topics = topics,
            WorkFolderPath = workFolderPath
        };

        string configPath = AppDataPathManager.GetGitHubConfigPath();
        string jsonString = JsonSerializer.Serialize(config, GitHubJsonContext.Default.GitHubConfiguration);
        File.WriteAllText(configPath, jsonString);

        _config = LoadConfiguration();
        _client = GetClient();
    }

    private GitHubClient GetClient()
    {
        _client = new GitHubClient(new ProductHeaderValue(nameof(GitHubService)));

        if (!string.IsNullOrEmpty(_config?.Token))
        {
            _client.Credentials = new Credentials(_config?.Token);
        }

        return _client;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var user = await _client.User.Current();
            return user != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub authentication failed");
            return false;
        }
    }

    public async Task<List<Repository>> GetRepositoriesAsync()
    {
        var repositories = new List<Repository>();

        try
        {
            var repos = await GetOrganizationRepositoriesAsync(_config?.Organization);
            repositories.AddRange(repos);

            var teamRepos = await GetConfiguredTeamRepositoriesAsync();
            repositories.AddRange(teamRepos);

            // Remove duplicates (same repo might be in multiple teams)
            repositories = repositories
                           .GroupBy(r => r.Id)
                           .Select(g => g.First())
                           .ToList();

            // Filter by topics if specified
            if (_config?.Topics is { Length: > 0 })
            {
                repositories = repositories
                    .Where(r => r.Topics != null && r.Topics.Any(t => _config.Topics.Contains(t, StringComparer.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Order by full name
            repositories = repositories.OrderBy(r => r.FullName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve team repositories");
        }

        return repositories;
    }

    private async Task<List<Repository>> GetConfiguredTeamRepositoriesAsync()
    {
        List<Repository> repositories = [];

        try
        {
            // Get all teams for the current user
            var myTeams = await _client.Organization.Team.GetAllForCurrent();

            // Filter by team names if specified
            if (_config?.TeamNames is { Length: > 0 })
            {
                myTeams = myTeams.Where(t => _config.TeamNames.Contains(t.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            foreach (var team in myTeams)
            {
                try
                {
                    // Get team repositories
                    var teamRepos = await _client.Organization.Team.GetAllRepositories(team.Id);
                    repositories.AddRange(teamRepos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve repositories for team: {TeamName}", team.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve team repositories");
        }

        return repositories;
    }

    private async Task<List<Repository>> GetOrganizationRepositoriesAsync(string? targetOrganization)
    {
        List<Repository> repositories = [];

        var organizations = await _client.Organization.GetAllForCurrent();

        if (!string.IsNullOrEmpty(targetOrganization))
        {
            organizations = organizations.Where(o => o.Login.Equals(targetOrganization, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        foreach (var organization in organizations)
        {
            _logger.LogInformation("Retrieving repositories for organization: {Org}", organization.Login);

            try
            {
                var apiOptions = new ApiOptions
                {
                    PageSize = 500,
                    StartPage = 1
                };

                IReadOnlyList<Repository> orgRepos;
                do
                {
                    orgRepos = await _client.Repository.GetAllForOrg(organization.Login, apiOptions);

                    _logger.LogInformation("Retrieved {Count} repositories for organization {Org} (Page {Page})", orgRepos.Count,  organization.Login, apiOptions.StartPage);

                    repositories.AddRange(orgRepos);
                    apiOptions.StartPage++;
                } while (orgRepos.Count == apiOptions.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve repositories for organization {Org}", organization.Login);
            }
        }

        _logger.LogInformation("Total organization repositories retrieved: {TotalCount}", repositories.Count);

        return repositories;
    }

    public async Task<string> GetCurrentUserAsync()
    {
        try
        {
            var user = await _client.User.Current();
            return user.Login;
        }
        catch
        {
            return "Not authenticated";
        }
    }

    public async Task<List<PullRequest>> GetPullRequestsAsync()
    {
        var pullRequests = new List<PullRequest>();

        try
        {
            // Get all team repositories first
            var repositories = await GetRepositoriesAsync();

            foreach (var repo in repositories)
            {
                try
                {
                    // Get open pull requests for each repository
                    var request = new PullRequestRequest
                    {
                        State = ItemStateFilter.Open
                    };

                    var repoPullRequests = await _client.PullRequest.GetAllForRepository(repo.Id, request);
                    pullRequests.AddRange(repoPullRequests);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve pull requests for repository {Repo}", repo.FullName);
                }
            }

            // Order by updated date (most recent first)
            pullRequests = pullRequests.OrderByDescending(pr => pr.UpdatedAt).ToList();
        }
        catch (Exception)
        {
            // If authentication fails or no pull requests found, return empty list
        }

        return pullRequests;
    }
}
