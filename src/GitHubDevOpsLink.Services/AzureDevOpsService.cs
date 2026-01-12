using GitHubDevOpsLink.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text.Json;
using AzureDevOpsJsonContext = GitHubDevOpsLink.Services.Models.AzureDevOpsJsonContext;

namespace GitHubDevOpsLink.Services;

public sealed class AzureDevOpsService : IAzureDevOpsService, IDisposable
{
    private readonly ILogger<AzureDevOpsService> _logger;
    private VssConnection? _connection;
    private AzureDevOpsConfiguration? _config;

    public AzureDevOpsService(ILogger<AzureDevOpsService> logger)
    {
        _logger = logger;
        _config = LoadConfiguration();
        _connection = InitializeConnection();
        _logger.LogDebug("AzureDevOpsService initialized");
    }

    public AzureDevOpsConfiguration? Configuration => _config;

    private AzureDevOpsConfiguration? LoadConfiguration()
    {
        try
        {
            string configPath = AppDataPathManager.GetAzureDevOpsConfigPath();
            _logger.LogInformation("Loading Azure DevOps configuration from: {ConfigPath}", configPath);

            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize(json, AzureDevOpsJsonContext.Default.AzureDevOpsConfiguration);

                if (config != null)
                {
                    _logger.LogInformation("Azure DevOps configuration loaded - Organization: {Organization}, Project: {Project}, Paths: {PathCount}",
                        config.Organization, config.Project, config.Paths?.Length ?? 0);
                    return config;
                }

                _logger.LogWarning("Azure DevOps configuration file exists but deserialization returned null");
            }
            else
            {
                _logger.LogWarning("Azure DevOps configuration file not found at: {ConfigPath}", configPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Azure DevOps configuration");
        }

        return null;
    }

    public void SaveConfig(
        string organization,
        string project,
        string token)
    {
        _logger.LogInformation("Saving Azure DevOps configuration - Organization: {Organization}, Project: {Project}", organization, project);
        SaveConfig(
            organization,
            project,
            token,
            null);
    }

    public void SaveConfig(
        string organization,
        string project,
        string token,
        string[]? paths)
    {
        _logger.LogInformation("Saving Azure DevOps configuration - Organization: {Organization}, Project: {Project}, Paths: {PathCount}",
            organization, project, paths?.Length ?? 0);

        try
        {
            string configPath = AppDataPathManager.GetAzureDevOpsConfigPath();

            var config = new AzureDevOpsConfiguration
            {
                Organization = organization,
                Project = project,
                Token = token,
                Paths = paths
            };

            string json = JsonSerializer.Serialize(config, AzureDevOpsJsonContext.Default.AzureDevOpsConfiguration);

            File.WriteAllText(configPath, json);

            _logger.LogInformation("Azure DevOps configuration saved successfully to {ConfigPath}", configPath);

            // Reload configuration and reinitialize connection
            _config = LoadConfiguration();
            _connection?.Dispose();
            _connection = InitializeConnection();

            _logger.LogDebug("Azure DevOps configuration and connection reinitialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Azure DevOps configuration");
            throw;
        }
    }

    private VssConnection? InitializeConnection()
    {
        _logger.LogDebug("Initializing Azure DevOps connection");

        if (_config == null || string.IsNullOrEmpty(_config.Organization) || string.IsNullOrEmpty(_config.Token))
        {
            _logger.LogWarning("Cannot create Azure DevOps connection - Configuration, organization or token is missing");
            return null;
        }

        try
        {
            var credentials = new VssBasicCredential(string.Empty, _config.Token);
            var uri = new Uri($"https://dev.azure.com/{_config.Organization}");
            var connection = new VssConnection(uri, credentials);

            _logger.LogInformation("Azure DevOps connection created successfully for organization {Organization}", _config.Organization);
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Azure DevOps connection for organization {Organization}", _config.Organization);
            return null;
        }
    }

    public bool IsConfigured()
    {
        _logger.LogDebug("Checking if Azure DevOps is configured");

        bool isConfigured = _config != null &&
               !string.IsNullOrEmpty(_config.Organization) &&
               !string.IsNullOrEmpty(_config.Project) &&
               !string.IsNullOrEmpty(_config.Token);

        _logger.LogDebug("Azure DevOps configuration check result: {IsConfigured}", isConfigured);
        return isConfigured;
    }

    public async Task<bool> ValidateConnectionAsync()
    {
        _logger.LogInformation("Validating Azure DevOps connection");

        try
        {
            if (_connection == null)
            {
                _logger.LogWarning("Connection validation failed - No connection available");
                return false;
            }

            // Try to connect and get a client to validate the connection
            var buildClient = await _connection.GetClientAsync<BuildHttpClient>();
            bool isValid = buildClient != null;

            _logger.LogInformation("Azure DevOps connection validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Azure DevOps connection");
            return false;
        }
    }

    public async Task<List<BuildDefinitionReference>> GetPipelinesAsync()
    {
        _logger.LogInformation("Getting Azure DevOps pipelines");
        var pipelines = new List<BuildDefinitionReference>();

        try
        {
            if (_connection == null || _config == null || string.IsNullOrEmpty(_config.Project))
            {
                _logger.LogWarning("Cannot get pipelines - Connection is null or project is not set");
                return pipelines;
            }

            var buildClient = await _connection.GetClientAsync<BuildHttpClient>();
            var definitions = await buildClient.GetDefinitionsAsync(_config.Project);

            _logger.LogDebug("Retrieved {Count} pipeline definitions from Azure DevOps", definitions.Count);

            // Filter by configured paths if any
            if (_config.Paths is { Length: > 0 })
            {
                _logger.LogDebug("Filtering pipelines by {PathCount} configured paths", _config.Paths.Length);

                // Filter pipelines that match any of the configured paths
                var filteredDefinitions = definitions.Where(d =>
                {
                    string pipelinePath = d.Path ?? "\\";
                    return _config.Paths.Any(configPath =>
                    {
                        // Normalize paths to use backslashes (Azure DevOps uses backslashes)
                        string normalizedPipelinePath = pipelinePath.Replace("/", "\\");
                        string normalizedConfigPath = configPath.Replace("/", "\\");

                        return normalizedPipelinePath.Equals(normalizedConfigPath, StringComparison.OrdinalIgnoreCase) ||
                               normalizedPipelinePath.StartsWith(normalizedConfigPath + "\\", StringComparison.OrdinalIgnoreCase);
                    });
                });
                pipelines.AddRange(filteredDefinitions);

                _logger.LogInformation("Filtered to {Count} pipelines matching configured paths", pipelines.Count);
            }
            else
            {
                // No path filter, return all pipelines
                pipelines.AddRange(definitions);
                _logger.LogInformation("No path filter configured, returning all {Count} pipelines", pipelines.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Azure DevOps pipelines");
        }

        return pipelines;
    }

    public async Task<List<Build>> GetRecentBuildsForPipelineAsync(int definitionId, int top = 5)
    {
        _logger.LogDebug("Getting recent builds for pipeline {DefinitionId}, top {Top}", definitionId, top);
        var builds = new List<Build>();

        try
        {
            if (_connection == null || _config == null || string.IsNullOrEmpty(_config.Project))
            {
                _logger.LogWarning("Cannot get recent builds - Connection is null or project is not set");
                return builds;
            }

            var buildClient = await _connection.GetClientAsync<BuildHttpClient>();
            var recentBuilds = await buildClient.GetBuildsAsync(
                _config.Project,
                [definitionId],
                top: top);

            builds.AddRange(recentBuilds);

            _logger.LogDebug("Retrieved {Count} recent builds for pipeline {DefinitionId}", builds.Count, definitionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent builds for pipeline {DefinitionId}", definitionId);
        }

        return builds;
    }

    public async Task<BuildDefinition?> GetPipelineDefinitionAsync(int definitionId)
    {
        _logger.LogDebug("Getting pipeline definition for {DefinitionId}", definitionId);

        try
        {
            if (_connection == null || _config == null || string.IsNullOrEmpty(_config.Project))
            {
                _logger.LogWarning("Cannot get pipeline definition - Connection is null or project is not set");
                return null;
            }

            var buildClient = await _connection.GetClientAsync<BuildHttpClient>();
            var definition = await buildClient.GetDefinitionAsync(_config.Project, definitionId);

            _logger.LogDebug("Successfully retrieved pipeline definition for {DefinitionId}", definitionId);
            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pipeline definition for {DefinitionId}", definitionId);
            return null;
        }
    }

    public async Task<List<PipelineViewModel>> GetPipelineViewModelsAsync()
    {
        _logger.LogInformation("Getting pipeline view models");
        var viewModels = new List<PipelineViewModel>();

        try
        {
            var pipelines = await GetPipelinesAsync();
            _logger.LogDebug("Processing {Count} pipelines to create view models", pipelines.Count);

            foreach (var pipeline in pipelines)
            {
                string pipelineName = pipeline.Name ?? "Unknown Pipeline";
                string path = pipeline.Path ?? "\\";
                string queueStatus = pipeline.QueueStatus.ToString();
                int id = pipeline.Id;
                int? lastBuildId = null;

                // Get repository URL if available
                string repositoryUrl = string.Empty;
                try
                {
                    var definition = await GetPipelineDefinitionAsync(id);

                    if (definition?.Repository != null)
                    {
                        repositoryUrl = definition.Repository.Url?.ToString() ?? string.Empty;

                        if (repositoryUrl.StartsWith("https://api.github.com/repos/", StringComparison.OrdinalIgnoreCase))
                        {
                            repositoryUrl = repositoryUrl.Replace("https://api.github.com/repos/", "https://github.com/", StringComparison.OrdinalIgnoreCase);
                            _logger.LogDebug("Normalized GitHub repository URL for pipeline {PipelineId}: {RepositoryUrl}", id, repositoryUrl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get repository info for pipeline {PipelineId}", id);
                }

                // Get the latest build info if available
                string subtitle = $"ID: {id} | Status: {queueStatus}";

                try
                {
                    var recentBuilds = await GetRecentBuildsForPipelineAsync(id, 1);

                    if (recentBuilds.Count > 0)
                    {
                        var latestBuild = recentBuilds[0];
                        lastBuildId = latestBuild.Id;
                        string buildStatus = latestBuild.Status?.ToString() ?? "Unknown";
                        string buildResult = latestBuild.Result?.ToString() ?? "N/A";

                        subtitle = buildStatus == "Completed" ? $"{buildResult} | Build #{latestBuild.BuildNumber}" : $"{buildStatus} | Build #{latestBuild.BuildNumber}";

                        _logger.LogDebug("Pipeline {PipelineId} latest build: {BuildStatus} - {BuildResult}", id, buildStatus, buildResult);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get build info for pipeline {PipelineId}", id);
                }

                viewModels.Add(
                    new PipelineViewModel
                    {
                        Name = pipelineName,
                        Subtitle = subtitle,
                        Id = id,
                        RepositoryUrl = string.IsNullOrEmpty(repositoryUrl) ? null : repositoryUrl,
                        Path = path,
                        LastBuildId = lastBuildId
                    });
            }

            _logger.LogInformation("Successfully created {Count} pipeline view models", viewModels.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pipeline view models");
        }

        return viewModels;
    }

    public async Task<List<PipelineViewModel>> GetPipelineViewModelsByRepositoryAsync(string repositoryUrl)
    {
        _logger.LogInformation("Getting pipeline view models for repository {RepositoryUrl}", repositoryUrl);

        var allPipelines = await GetPipelineViewModelsAsync();

        // Normalize the repository URL for comparison (remove .git suffix, trailing slashes, etc.)
        string normalizedRepoUrl = NormalizeRepositoryUrl(repositoryUrl);
        _logger.LogDebug("Normalized repository URL: {NormalizedUrl}", normalizedRepoUrl);

        // Filter pipelines that match the repository URL
        var matchingPipelines = allPipelines
                                .Where(p => !string.IsNullOrEmpty(p.RepositoryUrl) &&
                                            NormalizeRepositoryUrl(p.RepositoryUrl)
                                                .Equals(normalizedRepoUrl, StringComparison.OrdinalIgnoreCase))
                                .ToList();

        _logger.LogInformation("Found {Count} pipelines matching repository {RepositoryUrl}", matchingPipelines.Count, repositoryUrl);
        return matchingPipelines;
    }

    private static string NormalizeRepositoryUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        // Remove trailing .git suffix
        string normalized = url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? url.Substring(0, url.Length - 4)
            : url;

        // Remove trailing slashes
        normalized = normalized.TrimEnd('/');

        // Convert to lowercase for case-insensitive comparison
        return normalized.ToLowerInvariant();
    }

    public void Dispose()
    {
        if (_connection == null)
        {
            return;
        }

        _logger.LogDebug("Disposing Azure DevOps connection");
        _connection.Dispose();
        _connection = null;
    }
}