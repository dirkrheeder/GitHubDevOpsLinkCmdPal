using System.Collections.Generic;
using System.Linq;
using GitHubDevOpsLink.Services;
using GitHubDevOpsLink.Services.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class GitHubRepoActionsPage : ListPage
{
    private readonly GitHubRepositoryEntity _repository;
    private readonly ILocalRepositoryService _localRepositoryService;

    public GitHubRepoActionsPage(GitHubRepositoryEntity repository)
    {
        _repository = repository;
        _localRepositoryService = ServiceContainer.GetService<ILocalRepositoryService>();
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = repository.Name;
        Name = "Repo Actions";
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();

        // Check if local path is available
        string? localPath = _repository.LocalPath;
        
        
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            // Add local IDE options
            items.Add(new ListItem(
                new AnonymousCommand(() =>
                {
                    bool success = _localRepositoryService.OpenInVSCodeAsync(localPath).GetAwaiter().GetResult();
                    if (!success)
                    {
                        var errorToast = new ToastStatusMessage("Failed to open in VS Code. Please ensure VS Code is installed and 'code' command is in PATH.");
                        errorToast.Show();
                    }
                })
                {
                    Name = "Open in VS Code",
                    Result = CommandResult.KeepOpen()
                })
            {
                Title = "Open in VS Code",
                Subtitle = $"Open locally at: {localPath}"
            });

            // Only show "Open in Visual Studio" if there's a solution file
            if (_localRepositoryService.HasSolutionFile(localPath))
            {
                items.Add(new ListItem(
                    new AnonymousCommand(() =>
                    {
                        bool success = _localRepositoryService.OpenInVisualStudioAsync(localPath).GetAwaiter().GetResult();
                        if (!success)
                        {
                            var errorToast = new ToastStatusMessage("Failed to open in Visual Studio.");
                            errorToast.Show();
                        }
                    })
                    {
                        Name = "Open in Visual Studio",
                        Result = CommandResult.KeepOpen()
                    })
                {
                    Title = "Open in Visual Studio",
                    Subtitle = $"Open locally at: {localPath}"
                });
            }

            // Add "Open Folder in File Explorer" option
            items.Add(new ListItem(
                new AnonymousCommand(() =>
                {
                    bool success = _localRepositoryService.OpenInFileExplorerAsync(localPath).GetAwaiter().GetResult();
                    if (!success)
                    {
                        var errorToast = new ToastStatusMessage("Failed to open in File Explorer.");
                        errorToast.Show();
                    }
                })
                {
                    Name = "Open Folder in File Explorer",
                    Result = CommandResult.KeepOpen()
                })
            {
                Title = "Open Folder in File Explorer",
                Subtitle = $"Browse files at: {localPath}"
            });

            // Add "Open Folder in Terminal" option
            items.Add(new ListItem(
                new AnonymousCommand(() =>
                {
                    bool success = _localRepositoryService.OpenInTerminalAsync(localPath).GetAwaiter().GetResult();
                    if (!success)
                    {
                        var errorToast = new ToastStatusMessage("Failed to open in Terminal.");
                        errorToast.Show();
                    }
                })
                {
                    Name = "Open Folder in Terminal",
                    Result = CommandResult.KeepOpen()
                })
            {
                Title = "Open Folder in Terminal",
                Subtitle = $"Open command prompt at: {localPath}"
            });
        }
        else
        {
            // Add clone option if work folder is configured
            var githubService = ServiceContainer.GetService<IGitHubService>();
            string? workFolderPath = githubService.Configuration?.WorkFolderPath;
            
            if (!string.IsNullOrWhiteSpace(workFolderPath))
            {
                items.Add(new ListItem(
                    new AnonymousCommand(() =>
                    {
                        var infoToast = new ToastStatusMessage($"Cloning {_repository.Name}... This may take a few minutes.");
                        infoToast.Show();
                        
                        var clonedPath = _localRepositoryService.CloneRepositoryAsync(
                            _repository.HtmlUrl + ".git",
                            workFolderPath,
                            _repository.Name,
                            _repository.Id).GetAwaiter().GetResult();
                        
                        if (clonedPath != null)
                        {
                            // Update the current repository entity's local path
                            // This will make it available immediately when the page refreshes
                            _repository.LocalPath = clonedPath;
                            
                            var successToast = new ToastStatusMessage($"Successfully cloned to {clonedPath}! You can now open it locally.");
                            successToast.Show();
                        }
                        else
                        {
                            var errorToast = new ToastStatusMessage($"Failed to clone {_repository.Name}. Check logs for details.");
                            errorToast.Show();
                        }
                    })
                    {
                        Name = "Clone Repository",
                        Result = CommandResult.KeepOpen()
                    })
                {
                    Title = "Clone Repository",
                    Subtitle = $"Clone to: {workFolderPath}\\{_repository.Name}"
                });
            }
            else
            {
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "Configure Work Folder",
                    Subtitle = "Set work folder path in GitHub configuration to enable cloning"
                });
            }
        }

        items.Add(new ListItem(new OpenUrlCommand(_repository.HtmlUrl))
        {
            Title = "Open Repository",
            Subtitle = "View repository in GitHub"
        });
        
        items.Add(new ListItem(new OpenUrlCommand(_repository.HtmlUrl.Replace("github.com", "github.dev")))
        {
            Title = "Open Repository in Web Editor",
            Subtitle = "Web based editor using github.dev"
        });
        
        items.Add(new ListItem(new GitHubRepoPullRequestsPage(_repository))
        {
            Title = "View Pull Requests",
            Subtitle = "See open pull requests for this repository"
        });
        
        items.Add(new ListItem(new GitHubRepoPipelinesPage(_repository))
        {
            Title = "View Azure DevOps Pipelines",
            Subtitle = "See pipelines that use this repository"
        });
        
        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "Repository Information",
            Subtitle = _repository.Description ?? "No description available"
        });

        string visibility = _repository.Private ? "Private" : "Public";
        items.Add(
            new ListItem(new NoOpCommand())
            {
                Title = "Details",
                Subtitle = $"{visibility} | ⭐ {_repository.StargazersCount} stars | Language: {_repository.Language ?? "N/A"}"
            });

        return items.ToArray();
    }
}