using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GitHubDevOpsLink.Services;
using GitHubDevOpsLink.Services.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Logging;
using AzureDevOpsJsonContext = GitHubDevOpsLink.Services.Models.AzureDevOpsJsonContext;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class DevOpsConfigPage : ContentPage
{
    private readonly DevOpsConfigForm _configForm = new();

    public DevOpsConfigPage()
    {
        Name = "Configure";
        Title = "Azure DevOps Configuration";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
    }

    public override IContent[] GetContent()
    {
        return [_configForm];
    }
}

internal sealed partial class DevOpsConfigForm : FormContent
{
    private readonly ILogger<DevOpsConfigForm> _logger;
    private readonly IAzureDevOpsService _azureDevOpsService;

    public DevOpsConfigForm()
    {
        _logger = ServiceContainer.GetService<ILoggerFactory>().CreateLogger<DevOpsConfigForm>();
        _azureDevOpsService = ServiceContainer.GetService<IAzureDevOpsService>();
        
        _logger.LogDebug("DevOpsConfigForm initialized");
        
        // Load existing configuration from service
        var config = _azureDevOpsService.Configuration;
        string orgValue = config?.Organization ?? "";
        string projectValue = config?.Project ?? "";
        string tokenValue = ""; // Token cannot be retrieved for security reasons
        string pathsValue = config?.Paths != null ? string.Join(",", config.Paths.Select(x => x.Replace("\\", "/"))) : "";
        
        _logger.LogInformation("Loaded existing configuration - Organization: {Organization}, Project: {Project}", 
            string.IsNullOrEmpty(orgValue) ? "(not set)" : orgValue, 
            string.IsNullOrEmpty(projectValue) ? "(not set)" : projectValue);

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
                             "text": "Azure DevOps Configuration",
                             "horizontalAlignment": "center",
                             "wrap": true,
                             "style": "heading"
                           },
                           {
                             "type": "TextBlock",
                             "text": "To access your Azure DevOps pipelines, you need to provide your organization name, project name, and a Personal Access Token (PAT).",
                             "wrap": true,
                             "spacing": "medium"
                           },
                           {
                             "type": "TextBlock",
                             "text": "**Steps to create a PAT:**",
                             "wrap": true,
                             "weight": "bolder",
                             "spacing": "medium"
                           },
                           {
                             "type": "TextBlock",
                             "text": "1. Go to Azure DevOps ? User Settings ? Personal access tokens",
                             "wrap": true
                           },
                           {
                             "type": "TextBlock",
                             "text": "2. Click '+ New Token'",
                             "wrap": true
                           },
                           {
                             "type": "TextBlock",
                             "text": "3. Select scopes: **Build (Read)** and **Project and Team (Read)**",
                             "wrap": true
                           },
                           {
                             "type": "TextBlock",
                             "text": "4. Create token and copy it",
                             "wrap": true
                           },
                           {
                             "type": "Input.Text",
                             "label": "Organization Name",
                             "style": "text",
                             "id": "Organization",
                             "isRequired": true,
                             "errorMessage": "Organization name is required",
                             "placeholder": "your-organization",
                             "spacing": "large",
                             "value": "{{orgValue}}"
                           },
                           {
                             "type": "Input.Text",
                             "label": "Project Name",
                             "style": "text",
                             "id": "Project",
                             "isRequired": true,
                             "errorMessage": "Project name is required",
                             "placeholder": "your-project",
                             "spacing": "medium",
                             "value": "{{projectValue}}"
                           },
                           {
                             "type": "Input.Text",
                             "label": "Personal Access Token",
                             "style": "password",
                             "id": "Token",
                             "isRequired": true,
                             "errorMessage": "Token is required",
                             "placeholder": "your-pat-token",
                             "spacing": "medium",
                             "value": "{{tokenValue}}"
                           },
                           {
                             "type": "Input.Text",
                             "label": "Paths (comma-separated, optional)",
                             "style": "text",
                             "id": "Paths",
                             "isRequired": false,
                             "placeholder": "/path1,/path2,/path3",
                             "spacing": "medium",
                             "value": "{{pathsValue}}"
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
        _logger.LogInformation("DevOpsConfigForm.SubmitForm() - Form submission started");
        
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

            string? organization = formInput["Organization"]
                                   ?.ToString()
                                   ?.Trim();
            string? project = formInput["Project"]
                              ?.ToString()
                              ?.Trim();
            string? token = formInput["Token"]
                            ?.ToString()
                            ?.Trim();
            string? pathsInput = formInput["Paths"]
                                 ?.ToString()
                                 ?.Trim();

            if (string.IsNullOrWhiteSpace(organization))
            {
                _logger.LogWarning("Validation failed: Organization name is empty");
                var errorToast = new ToastStatusMessage("Organization name cannot be empty");
                errorToast.Show();
                return CommandResult.KeepOpen();
            }

            if (string.IsNullOrWhiteSpace(project))
            {
                _logger.LogWarning("Validation failed: Project name is empty");
                var errorToast = new ToastStatusMessage("Project name cannot be empty");
                errorToast.Show();
                return CommandResult.KeepOpen();
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Validation failed: Token is empty");
                var errorToast = new ToastStatusMessage("Token cannot be empty");
                errorToast.Show();
                return CommandResult.KeepOpen();
            }

            // Parse paths if provided
            string[]? paths = null;
            if (!string.IsNullOrWhiteSpace(pathsInput))
            {
                paths = pathsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _logger.LogDebug("Parsed {PathCount} paths from input", paths.Length);
            }

            _logger.LogInformation("Creating configuration for Organization: {Organization}, Project: {Project}", organization, project);

            // Save configuration using AzureDevOpsService
            _azureDevOpsService.SaveConfig(organization, project, token, paths);
            _logger.LogInformation("Configuration saved successfully");

            // Show confirmation
            var confirmArgs = new ConfirmationArgs
            {
                PrimaryCommand = new AnonymousCommand(() =>
                {
                    var successToast = new ToastStatusMessage("Azure DevOps configuration saved successfully! ?");
                    successToast.Show();
                })
                {
                    Name = "OK",
                    Result = CommandResult.GoHome()
                },
                Title = "Configuration Saved",
                Description = "Your Azure DevOps configuration has been saved securely. You can now access your pipelines."
            };

            return CommandResult.Confirm(confirmArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            var errorToast = new ToastStatusMessage($"Error: {ex.Message}");
            errorToast.Show();
            return CommandResult.KeepOpen();
        }
    }
}
