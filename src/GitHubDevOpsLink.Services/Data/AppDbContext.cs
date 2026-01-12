using GitHubDevOpsLink.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace GitHubDevOpsLink.Services.Data;

public class AppDbContext : DbContext
{
    public DbSet<GitHubRepositoryEntity> GitHubRepositories { get; set; }
    public DbSet<GitHubPullRequestEntity> GitHubPullRequests { get; set; }
    public DbSet<AzureDevOpsPipelineEntity> AzureDevOpsPipelines { get; set; }

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string dbPath = AppDataPathManager.GetDatabasePath();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure GitHubRepositoryEntity
        modelBuilder.Entity<GitHubRepositoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.FullName).IsRequired();
            entity.Property(e => e.HtmlUrl).IsRequired();
            entity.Property(e => e.Owner).IsRequired();
            entity.HasIndex(e => e.FullName);
            entity.HasIndex(e => e.LastFetchedAt);
            entity.Property(e => e.LocalPath).IsRequired(false);
        });

        // Configure GitHubPullRequestEntity
        modelBuilder.Entity<GitHubPullRequestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.HtmlUrl).IsRequired();
            entity.Property(e => e.RepositoryFullName).IsRequired();
            entity.Property(e => e.Author).IsRequired();
            entity.Property(e => e.Owner).IsRequired();
            entity.HasIndex(e => e.RepositoryFullName);
            entity.HasIndex(e => e.Owner);
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => e.LastFetchedAt);
        });

        // Configure AzureDevOpsPipelineEntity
        modelBuilder.Entity<AzureDevOpsPipelineEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Organization).IsRequired();
            entity.Property(e => e.Project).IsRequired();
            entity.HasIndex(e => new { e.Organization, e.Project });
            entity.HasIndex(e => e.RepositoryUrl);
            entity.HasIndex(e => e.LastFetchedAt);
        });
    }
}
