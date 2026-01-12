using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using GitHubDevOpsLink.Services;

namespace GitHubDevOpsLink.Pages;

internal sealed partial class AppDataFolderPage : ListPage
{
    public AppDataFolderPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "App Data Folders";
        Name = "App Data";
    }

    private static AnonymousCommand CreateCopyToClipboardCommand(string folderPath, string folderName)
    {
        return new AnonymousCommand(() =>
        {
            try
            {
                // Copy to clipboard
                var dataPackage = new DataPackage();
                dataPackage.SetText(folderPath);
                Clipboard.SetContent(dataPackage);

                var toast = new ToastStatusMessage($"{folderName} path copied to clipboard!");
                toast.Show();
            }
            catch (Exception ex)
            {
                var errorToast = new ToastStatusMessage($"Failed to copy: {ex.Message}");
                errorToast.Show();
            }
        })
        {
            Name = "Copy Path",
            Result = CommandResult.KeepOpen()
        };
    }

    private static AnonymousCommand CreateOpenFolderCommand(string folderPath, string folderName)
    {
        return new AnonymousCommand(() =>
        {
            try
            {
                // Ensure folder exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Open folder in File Explorer
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });

                var toast = new ToastStatusMessage($"Opening {folderName} in File Explorer");
                toast.Show();
            }
            catch (Exception ex)
            {
                var errorToast = new ToastStatusMessage($"Failed to open folder: {ex.Message}");
                errorToast.Show();
            }
        })
        {
            Name = "Open Folder",
            Result = CommandResult.KeepOpen()
        };
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();

        // Get paths from centralized manager
        string rootFolder = AppDataPathManager.GetRootFolderPath();
        string databasePath = AppDataPathManager.GetDatabasePath();
        string githubConfigPath = AppDataPathManager.GetGitHubConfigPath();
        string devopsConfigPath = AppDataPathManager.GetAzureDevOpsConfigPath();
        
        // Add header
        items.Add(
            new ListItem(new NoOpCommand())
            {
                Title = "Application Data Folders",
                Subtitle = "Copy paths or open folders where configurations and database are stored"
            });

        items.Add(
            new ListItem(new CopyTextCommand(rootFolder))
            {
                Title = "Copy Application Data Folder Path",
                Subtitle = rootFolder
            });

        items.Add(
            new ListItem(new OpenFileCommand(rootFolder))
            {
                Title = "Open Application Data Folder",
                Subtitle = "Opens folder in File Explorer"
            });

        // Check if database exists
        bool dbExists = File.Exists(databasePath);
        long dbSize = 0;
        if (dbExists)
        {
            try
            {
                var fileInfo = new FileInfo(databasePath);
                dbSize = fileInfo.Length;
            }
            catch { }
        }

        items.Add(
            new ListItem(new NoOpCommand())
            {
                Title = "Database Status",
                Subtitle = dbExists 
                    ? $"? Database exists ({FormatFileSize(dbSize)}) - {databasePath}" 
                    : $"? Database not created yet - {databasePath}"
            });

        // Check configuration files
        bool githubConfigExists = File.Exists(githubConfigPath);
        bool devopsConfigExists = File.Exists(devopsConfigPath);

        items.Add(
            new ListItem(new NoOpCommand())
            {
                Title = "Configuration Status",
                Subtitle = $"GitHub: {(githubConfigExists ? "? Configured" : "? Not configured")} | " +
                          $"Azure DevOps: {(devopsConfigExists ? "? Configured" : "? Not configured")}"
            });

        items.Add(
            new ListItem(new NoOpCommand())
            {
                Title = "Configuration Files",
                Subtitle = $"GitHub: {githubConfigPath}\nAzure DevOps: {devopsConfigPath}"
            });

        return items.ToArray();
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
