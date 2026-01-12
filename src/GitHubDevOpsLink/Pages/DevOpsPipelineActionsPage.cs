using System.Collections.Generic;
using GitHubDevOpsLink.Services.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class DevOpsPipelineActionsPage : ListPage
{
    private readonly PipelineViewModel _pipeline;
    private readonly string _organization;
    private readonly string _project;

    public DevOpsPipelineActionsPage(
        PipelineViewModel pipeline,
        string organization,
        string project)
    {
        _pipeline = pipeline;
        _organization = organization;
        _project = project;

        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = pipeline.Name;
        Name = "Pipeline Actions";
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();

        // Build the pipeline URL
        string pipelineUrl = $"https://dev.azure.com/{_organization}/{_project}/_build?definitionId={_pipeline.Id}";

        // Add pipeline link
        items.Add(
            new ListItem(new OpenUrlCommand(pipelineUrl))
            {
                Title = "Open Pipeline",
                Subtitle = "View pipeline definition in Azure DevOps"
            });

        // Add repository link if available
        if (!string.IsNullOrEmpty(_pipeline.RepositoryUrl))
        {
            items.Add(
                new ListItem(new OpenUrlCommand(_pipeline.RepositoryUrl))
                {
                    Title = "Open Repository",
                    Subtitle = "View source code repository"
                });
        }

        // Add last run link if available
        if (_pipeline.LastBuildId.HasValue)
        {
            string lastRunUrl = $"https://dev.azure.com/{_organization}/{_project}/_build/results?buildId={_pipeline.LastBuildId.Value}";
            items.Add(
                new ListItem(new OpenUrlCommand(lastRunUrl))
                {
                    Title = "▶️ Open Last Run",
                    Subtitle = $"View latest build results (Build ID: {_pipeline.LastBuildId.Value})"
                });
        }

        // Add pipeline info
        items.Add(
            new ListItem(new NoOpCommand())
            {
                Title = "Pipeline Information",
                Subtitle = _pipeline.Subtitle
            });

        if (!string.IsNullOrEmpty(_pipeline.Path))
        {
            items.Add(
                new ListItem(new NoOpCommand())
                {
                    Title = "Path",
                    Subtitle = _pipeline.Path
                });
        }

        return items.ToArray();
    }
}