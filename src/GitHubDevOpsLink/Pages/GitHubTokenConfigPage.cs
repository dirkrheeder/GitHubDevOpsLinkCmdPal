using GitHubDevOpsLink.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json.Nodes;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class GitHubTokenConfigPage : ContentPage
{
    private readonly GitHubTokenForm _tokenForm = new();

    public GitHubTokenConfigPage()
    {
        Name = "Configure";
        Title = "GitHub Configuration";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
    }

    public override IContent[] GetContent()
    {
        return [_tokenForm];
    }
}

internal sealed partial class GitHubTokenForm : FormContent
{
    private readonly ILogger<GitHubTokenForm> _logger;
    private readonly IGitHubService gitHubService;

    public GitHubTokenForm()
    {
        _logger = ServiceContainer.GetService<ILoggerFactory>().CreateLogger<GitHubTokenForm>();
        gitHubService = ServiceContainer.GetService<IGitHubService>();

        _logger.LogDebug("GitHubTokenForm initialized");
        
        // Load existing configuration if available
        var existingConfig = gitHubService.Configuration;
        string tokenValue = existingConfig?.Token ?? "";
        string orgValue = existingConfig?.Organization ?? "";
        string teamNamesValue = existingConfig?.TeamNames != null ? string.Join(",", existingConfig.TeamNames) : "";
        string topicsValue = existingConfig?.Topics != null ? string.Join(",", existingConfig.Topics) : "";
        string workFolderPathValue = existingConfig?.WorkFolderPath ?? "";
        
        _logger.LogInformation("Loaded existing configuration - Organization: {Organization}, Teams: {TeamCount}, Topics: {TopicCount}, WorkFolder: {WorkFolder}", 
            string.IsNullOrEmpty(orgValue) ? "(not set)" : orgValue,
            existingConfig?.TeamNames?.Length ?? 0,
            existingConfig?.Topics?.Length ?? 0,
            string.IsNullOrEmpty(workFolderPathValue) ? "(not set)" : workFolderPathValue);

        TemplateJson = $$"""
                       {
                         "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
                         "type": "AdaptiveCard",
                         "version": "1.6",
                         "body": [
                           {
                             "type": "TextBlock",
                             "size": "large",
                             "weight": "bolder",
                             "text": "GitHub Configuration",
                             "horizontalAlignment": "center",
                             "wrap": true,
                             "style": "heading"
                           },
                           {
                             "type": "TextBlock",
                             "text": "To access your GitHub repositories, you need to provide a Personal Access Token and optional filtering configuration.",
                             "wrap": true,
                             "spacing": "medium"
                           },
                           {
                             "type": "TextBlock",
                             "text": "**Steps to create a token:**",
                             "wrap": true,
                             "weight": "bolder",
                             "spacing": "medium"
                           },
                           {
                             "type": "TextBlock",
                             "text": "1. Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)",
                             "wrap": true
                           },
                           {
                             "type": "TextBlock",
                             "text": "2. Click 'Generate new token (classic)'",
                             "wrap": true
                           },
                           {
                             "type": "TextBlock",
                             "text": "3. Select scopes: **repo** (Full control of private repositories) and **read:org** (Read organization membership)",
                             "wrap": true
                           },
                           {
                             "type": "TextBlock",
                             "text": "4. Generate token and copy it",
                             "wrap": true
                           },
                           {
                             "type": "Input.Text",
                             "label": "GitHub Personal Access Token",
                             "style": "password",
                             "id": "Token",
                             "isRequired": true,
                             "errorMessage": "Token is required",
                             "placeholder": "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
                             "spacing": "large",
                             "value": "{{tokenValue}}"
                           },
                           {
                             "type": "Input.Text",
                             "label": "Organization (optional)",
                             "style": "text",
                             "id": "Organization",
                             "isRequired": false,
                             "placeholder": "your-organization",
                             "spacing": "medium",
                             "value": "{{orgValue}}"
                           },
                           {
                             "type": "Input.Text",
                             "label": "Team Names (comma-separated, optional)",
                             "style": "text",
                             "id": "TeamNames",
                             "isRequired": false,
                             "placeholder": "team1,team2,team3",
                             "spacing": "medium",
                             "value": "{{teamNamesValue}}"
                           },
                           {
                             "type": "Input.Text",
                             "label": "Topics (comma-separated, optional)",
                             "style": "text",
                             "id": "Topics",
                             "isRequired": false,
                             "placeholder": "topic1,topic2,topic3",
                             "spacing": "medium",
                             "value": "{{topicsValue}}"
                           },
                           {
                             "type": "Input.Text",
                             "label": "Work Folder Path (optional)",
                             "style": "text",
                             "id": "WorkFolderPath",
                             "isRequired": false,
                             "placeholder": "C:\\Work\\Projects",
                             "spacing": "medium",
                             "value": "{{workFolderPathValue}}"
                           },
                           {
                             "type": "TextBlock",
                             "text": "Work Folder Path is where your GitHub repositories are cloned locally. This enables opening repos in VS Code or Visual Studio.",
                             "wrap": true,
                             "size": "small",
                             "color": "default"
                           },
                           {
                             "type": "TextBlock",
                             "text": "Your configuration will be stored securely in your local application data folder.",
                             "wrap": true,
                             "size": "small",
                             "color": "attention"
                           }
                         ],
                         "actions": [
                           {
                             "type": "Action.Submit",
                             "title": "Save Configuration",
                             "style": "positive"
                           }
                         ]
                       }
                       """;
    }

    public override CommandResult SubmitForm(string payload)
    {
        _logger.LogInformation("GitHubTokenForm.SubmitForm() - Form submission started");
        
        try
        {
            var formInput = JsonNode.Parse(payload)
                                    ?.AsObject();
            _logger.LogDebug("Form submitted with payload: {Payload}", payload);

            if (formInput == null)
            {
                _logger.LogError("Failed to parse form data");
                var errorToast = new ToastStatusMessage("Failed to parse form data");
                errorToast.Show();
                return CommandResult.KeepOpen();
            }

            string? token = formInput["Token"]
                            ?.ToString()
                            ?.Trim();
            string? organization = formInput["Organization"]
                            ?.ToString()
                            ?.Trim();
            string? teamNamesInput = formInput["TeamNames"]
                            ?.ToString()
                            ?.Trim();
            string? topicsInput = formInput["Topics"]
                            ?.ToString()
                            ?.Trim();
            string? workFolderPath = formInput["WorkFolderPath"]
                            ?.ToString()
                            ?.Trim();

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Validation failed: Token is empty");
                var errorToast = new ToastStatusMessage("Token cannot be empty");
                errorToast.Show();
                return CommandResult.KeepOpen();
            }

            // Parse team names if provided
            string[]? teamNames = null;
            if (!string.IsNullOrWhiteSpace(teamNamesInput))
            {
                teamNames = teamNamesInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _logger.LogDebug("Parsed {TeamCount} team names from input", teamNames.Length);
            }

            // Parse topics if provided
            string[]? topics = null;
            if (!string.IsNullOrWhiteSpace(topicsInput))
            {
                topics = topicsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _logger.LogDebug("Parsed {TopicCount} topics from input", topics.Length);
            }

            _logger.LogInformation("Saving GitHub configuration - Organization: {Organization}, Teams: {TeamCount}, Topics: {TopicCount}, WorkFolder: {WorkFolder}",
                string.IsNullOrWhiteSpace(organization) ? "(not set)" : organization,
                teamNames?.Length ?? 0,
                topics?.Length ?? 0,
                string.IsNullOrWhiteSpace(workFolderPath) ? "(not set)" : workFolderPath);

            // Save using the new SaveConfig method
            gitHubService.SaveConfig(token, organization, teamNames, topics, workFolderPath);
            
            _logger.LogInformation("GitHub configuration saved successfully");

            // Show confirmation
            var confirmArgs = new ConfirmationArgs
            {
                PrimaryCommand = new AnonymousCommand(() =>
                {
                    var successToast = new ToastStatusMessage("GitHub configuration saved successfully!");
                    successToast.Show();
                })
                {
                    Name = "OK",
                    Result = CommandResult.GoHome()
                },
                Title = "Configuration Saved",
                Description = "Your GitHub configuration has been saved securely. You can now access your filtered GitHub repositories."
            };

            return CommandResult.Confirm(confirmArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving GitHub configuration");
            var errorToast = new ToastStatusMessage($"Error: {ex.Message}");
            errorToast.Show();
            return CommandResult.KeepOpen();
        }
    }
}
