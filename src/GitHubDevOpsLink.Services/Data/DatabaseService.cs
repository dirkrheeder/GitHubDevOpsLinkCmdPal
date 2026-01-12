using GitHubDevOpsLink.Services.Models;
using Microsoft.EntityFrameworkCore;
using Octokit;

namespace GitHubDevOpsLink.Services.Data;

public sealed class DatabaseService : IDatabaseService
{
    public DatabaseService()
    {
        using var context = new AppDbContext();
        context.Database.EnsureCreated();
    }

    public async Task SaveGitHubRepositoriesAsync(IEnumerable<Repository> repositories, string owner)
    {
        await using var context = new AppDbContext();

        var entities = repositories.Select(repo => new GitHubRepositoryEntity
        {
            Id = repo.Id,
            Name = repo.Name,
            FullName = repo.FullName,
            Description = repo.Description,
            HtmlUrl = repo.HtmlUrl,
            Private = repo.Private,
            StargazersCount = repo.StargazersCount,
            Language = repo.Language,
            Owner = owner,
            CreatedAt = repo.CreatedAt.UtcDateTime,
            UpdatedAt = repo.UpdatedAt.UtcDateTime,
            LastFetchedAt = DateTime.UtcNow
        }).ToList();

        foreach (var entity in entities)
        {
            var existing = await context.GitHubRepositories.FindAsync(entity.Id);
            if (existing != null)
            {
                // Update existing
                existing.Name = entity.Name;
                existing.FullName = entity.FullName;
                existing.Description = entity.Description;
                existing.HtmlUrl = entity.HtmlUrl;
                existing.Private = entity.Private;
                existing.StargazersCount = entity.StargazersCount;
                existing.Language = entity.Language;
                existing.Owner = entity.Owner;
                existing.UpdatedAt = entity.UpdatedAt;
                existing.LastFetchedAt = entity.LastFetchedAt;
            }
            else
            {
                // Add new
                await context.GitHubRepositories.AddAsync(entity);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<GitHubRepositoryEntity>> GetGitHubRepositoriesAsync(string owner)
    {
        await using var context = new AppDbContext();
        var ownerParam = owner;
        return await context.GitHubRepositories
            .Where(r => r.Owner == ownerParam)
            .OrderBy(r => r.FullName)
            .ToListAsync();
    }

    public async Task<List<GitHubRepositoryEntity>> GetAllGitHubRepositoriesAsync()
    {
        await using var context = new AppDbContext();
        return await context.GitHubRepositories
            .OrderBy(r => r.FullName)
            .ToListAsync();
    }

    public async Task<DateTime?> GetLastGitHubRepositoryFetchTimeAsync(string owner)
    {
        await using var context = new AppDbContext();
        var ownerParam = owner;
        var lastFetch = await context.GitHubRepositories
            .Where(r => r.Owner == ownerParam)
            .OrderByDescending(r => r.LastFetchedAt)
            .Select(r => r.LastFetchedAt)
            .FirstOrDefaultAsync();

        return lastFetch == default ? null : lastFetch;
    }

    public async Task ClearGitHubRepositoriesAsync(string owner)
    {
        await using var context = new AppDbContext();
        var ownerParam = owner;
        var repos = await context.GitHubRepositories
            .Where(r => r.Owner == ownerParam)
            .ToListAsync();

        context.GitHubRepositories.RemoveRange(repos);
        await context.SaveChangesAsync();
    }

    public async Task UpdateRepositoryLocalPathAsync(long repositoryId, string? localPath)
    {
        await using var context = new AppDbContext();
        var repository = await context.GitHubRepositories.FindAsync(repositoryId);
        if (repository != null)
        {
            repository.LocalPath = localPath;
            await context.SaveChangesAsync();
        }
    }

    public async Task<string?> GetRepositoryLocalPathAsync(long repositoryId)
    {
        await using var context = new AppDbContext();
        var repository = await context.GitHubRepositories.FindAsync(repositoryId);
        return repository?.LocalPath;
    }

    public async Task SaveGitHubPullRequestsAsync(IEnumerable<PullRequest> pullRequests, string owner)
    {
        await using var context = new AppDbContext();

        var entities = pullRequests.Select(pr => new GitHubPullRequestEntity
        {
            Id = (int)pr.Id,
            Number = pr.Number,
            Title = pr.Title,
            HtmlUrl = pr.HtmlUrl,
            State = pr.State.StringValue,
            RepositoryName = pr.Base.Repository.Name,
            RepositoryFullName = pr.Base.Repository.FullName,
            Author = pr.User.Login,
            CreatedAt = pr.CreatedAt.UtcDateTime,
            UpdatedAt = pr.UpdatedAt.UtcDateTime,
            IsDraft = pr.Draft,
            Owner = owner,
            LastFetchedAt = DateTime.UtcNow
        }).ToList();

        foreach (var entity in entities)
        {
            var existing = await context.GitHubPullRequests.FindAsync(entity.Id);
            if (existing != null)
            {
                // Update existing
                existing.Number = entity.Number;
                existing.Title = entity.Title;
                existing.HtmlUrl = entity.HtmlUrl;
                existing.State = entity.State;
                existing.RepositoryName = entity.RepositoryName;
                existing.RepositoryFullName = entity.RepositoryFullName;
                existing.Author = entity.Author;
                existing.UpdatedAt = entity.UpdatedAt;
                existing.IsDraft = entity.IsDraft;
                existing.Owner = entity.Owner;
                existing.LastFetchedAt = entity.LastFetchedAt;
            }
            else
            {
                // Add new
                await context.GitHubPullRequests.AddAsync(entity);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<GitHubPullRequestEntity>> GetGitHubPullRequestsAsync(string owner)
    {
        await using var context = new AppDbContext();
        var ownerParam = owner;
        return await context.GitHubPullRequests
            .Where(pr => pr.Owner == ownerParam)
            .OrderByDescending(pr => pr.UpdatedAt)
            .ToListAsync();
    }

    public async Task<DateTime?> GetLastGitHubPullRequestFetchTimeAsync(string owner)
    {
        await using var context = new AppDbContext();
        var ownerParam = owner;
        var lastFetch = await context.GitHubPullRequests
            .Where(pr => pr.Owner == ownerParam)
            .OrderByDescending(pr => pr.LastFetchedAt)
            .Select(pr => pr.LastFetchedAt)
            .FirstOrDefaultAsync();

        return lastFetch == default ? null : lastFetch;
    }

    public async Task ClearGitHubPullRequestsAsync(string owner)
    {
        await using var context = new AppDbContext();
        var ownerParam = owner;
        var pullRequests = await context.GitHubPullRequests
            .Where(pr => pr.Owner == ownerParam)
            .ToListAsync();

        context.GitHubPullRequests.RemoveRange(pullRequests);
        await context.SaveChangesAsync();
    }

    public async Task SaveAzureDevOpsPipelinesAsync(IEnumerable<PipelineViewModel> pipelines, string organization, string project)
    {
        await using var context = new AppDbContext();

        var entities = pipelines.Select(pipeline => new AzureDevOpsPipelineEntity
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            Path = pipeline.Path,
            RepositoryUrl = pipeline.RepositoryUrl,
            Organization = organization,
            Project = project,
            LastBuildId = pipeline.LastBuildId,
            LastFetchedAt = DateTime.UtcNow
        }).ToList();

        foreach (var entity in entities)
        {
            var existing = await context.AzureDevOpsPipelines.FindAsync(entity.Id);
            if (existing != null)
            {
                // Update existing
                existing.Name = entity.Name;
                existing.Path = entity.Path;
                existing.RepositoryUrl = entity.RepositoryUrl;
                existing.Organization = entity.Organization;
                existing.Project = entity.Project;
                existing.LastBuildId = entity.LastBuildId;
                existing.LastFetchedAt = entity.LastFetchedAt;
            }
            else
            {
                // Add new
                await context.AzureDevOpsPipelines.AddAsync(entity);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<AzureDevOpsPipelineEntity>> GetAzureDevOpsPipelinesAsync(string organization, string project)
    {
        await using var context = new AppDbContext();
        var orgParam = organization;
        var projParam = project;
        return await context.AzureDevOpsPipelines
            .Where(p => p.Organization == orgParam && p.Project == projParam)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<AzureDevOpsPipelineEntity>> GetAzureDevOpsPipelinesByRepositoryAsync(string repositoryUrl)
    {
        await using var context = new AppDbContext();
        
        // Normalize the repository URL for comparison
        string normalizedRepoUrl = NormalizeRepositoryUrl(repositoryUrl);

        // First get all pipelines from database
        var allPipelines = await context.AzureDevOpsPipelines
            .Where(p => p.RepositoryUrl != null && p.RepositoryUrl != string.Empty)
            .ToListAsync();

        // Then filter on client-side with normalized URLs
        return allPipelines
            .Where(p => NormalizeRepositoryUrl(p.RepositoryUrl!)
                .Equals(normalizedRepoUrl, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .ToList();
    }

    public async Task<DateTime?> GetLastAzureDevOpsPipelineFetchTimeAsync(string organization, string project)
    {
        await using var context = new AppDbContext();
        var orgParam = organization;
        var projParam = project;
        var lastFetch = await context.AzureDevOpsPipelines
            .Where(p => p.Organization == orgParam && p.Project == projParam)
            .OrderByDescending(p => p.LastFetchedAt)
            .Select(p => p.LastFetchedAt)
            .FirstOrDefaultAsync();

        return lastFetch == default ? null : lastFetch;
    }

    public async Task ClearAzureDevOpsPipelinesAsync(string organization, string project)
    {
        await using var context = new AppDbContext();
        var orgParam = organization;
        var projParam = project;
        var pipelines = await context.AzureDevOpsPipelines
            .Where(p => p.Organization == orgParam && p.Project == projParam)
            .ToListAsync();

        context.AzureDevOpsPipelines.RemoveRange(pipelines);
        await context.SaveChangesAsync();
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
}
