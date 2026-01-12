namespace GitHubDevOpsLink.Services;

/// <summary>
/// Centralized manager for all application data paths and configuration locations.
/// All services should use this class to get consistent folder paths.
/// </summary>
public static class AppDataPathManager
{
    private const string RootFolderName = "GitHubDevOpsLink";
    private const string LogsFolderName = "logs";
    private const string DatabaseFileName = "githubdevopslink.db";
    private const string GitHubConfigFileName = "github-config.json";
    private const string AzureDevOpsConfigFileName = "azuredevops-config.json";

    /// <summary>
    /// Gets the root application data folder path.
    /// </summary>
    public static string GetRootFolderPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string rootFolder = Path.Combine(appDataPath, RootFolderName);

        if (!Directory.Exists(rootFolder))
        {
            Directory.CreateDirectory(rootFolder);
        }

        return rootFolder;
    }

    /// <summary>
    /// Gets the logs folder path.
    /// </summary>
    public static string GetLogsFolderPath()
    {
        string logsFolder = Path.Combine(GetRootFolderPath(), LogsFolderName);

        if (!Directory.Exists(logsFolder))
        {
            Directory.CreateDirectory(logsFolder);
        }

        return logsFolder;
    }

    /// <summary>
    /// Gets the database file path.
    /// </summary>
    public static string GetDatabasePath()
    {
        return Path.Combine(GetRootFolderPath(), DatabaseFileName);
    }

    /// <summary>
    /// Gets the GitHub configuration file path.
   /// </summary>
    public static string GetGitHubConfigPath()
    {
        return Path.Combine(GetRootFolderPath(), GitHubConfigFileName);
    }

    /// <summary>
    /// Gets the Azure DevOps configuration file path.
    /// </summary>
    public static string GetAzureDevOpsConfigPath()
    {
        return Path.Combine(GetRootFolderPath(), AzureDevOpsConfigFileName);
    }
}
