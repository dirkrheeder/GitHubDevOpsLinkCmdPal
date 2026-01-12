using GitHubDevOpsLink.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitHubDevOpsLink;

public partial class GitHubDevOpsLinkCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public GitHubDevOpsLinkCommandsProvider()
    {
        DisplayName = "GitHub & DevOps Link";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new GitHubReposPage()) { Title = "GitHub Repositories" },
            new CommandItem(new GitHubPullRequestsPage()) { Title = "GitHub Pull Requests" },
            new CommandItem(new DevOpsPipelinesPage()) { Title = "Azure DevOps Pipelines" },
            #if DEBUG
            new CommandItem(new AppDataFolderPage()) { Title = "View App Data Folders" },
            #endif
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}
