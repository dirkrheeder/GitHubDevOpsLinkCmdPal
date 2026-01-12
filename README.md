# GitHub and DevOps Link

A PowerToys Command Palette extension that provides quick access to your GitHub repositories, pull requests, and Azure DevOps pipelines. This extension extends the capabilities of PowerToys Command Palette, allowing you to search and manage your development resources directly from the Command Palette interface.

## Features

### GitHub Integration

- **Repository Browser**: View all your GitHub repositories with filtering by organization, teams, and topics
- **Pull Request Viewer**: Quick access to open pull requests across your repositories
- **Repository Actions**:
  - Open repository in GitHub
  - Open repository in github.dev (web editor)
  - View pull requests for a specific repository
  - View Azure DevOps pipelines associated with a repository
  - See repository details (visibility, stars, language)
- **Smart Caching**: Local database caching for faster load times with auto-refresh every 10 minutes

### Azure DevOps Integration

- **Pipeline Browser**: View all pipelines in your Azure DevOps project
- **Pipeline Actions**:
  - Open pipeline definition in Azure DevOps
  - Open source code repository
  - View latest build results
  - See pipeline details and paths
- **Smart Caching**: Local database caching for faster load times with auto-refresh every 10 minutes

### User Experience

- Quick access via PowerToys Command Palette (default: **Win+Alt+Space**)
- Mouse and keyboard navigation support
- Offline support with cached data
- Real-time status indicators for cached data
- Manual refresh options when needed
- Intuitive navigation and search
- Customizable keyboard shortcuts through Command Palette settings

## Installation

### Prerequisites

**PowerToys Command Palette must be installed and running** for this extension to work. Install PowerToys from:
- [Microsoft PowerToys Official Page](https://learn.microsoft.com/en-us/windows/powertoys/install)
- [GitHub Releases](https://github.com/microsoft/PowerToys/releases)

After installing PowerToys, enable Command Palette in PowerToys Settings.

### Method 1: Deploy and Test in Debug Mode (Visual Studio)

For development and testing:

1. **Enable Developer Mode on Windows**:
   - Open Windows Settings > Privacy & Security > For developers
   - Enable **Developer Mode**
   - This is required for deploying packaged applications from Visual Studio

2. **Open the Solution**:
   - Open `GithubDevOpsLink.slnx` in Visual Studio
   - Ensure you have the following workloads installed:
     - C# development workload
     - Windows application development workload

3. **Configure Platform**:
   - Select the appropriate platform configuration (x64 or ARM64) from the toolbar
   - Do **NOT** select "Unpackaged" configuration

4. **Deploy the Extension**:
   - Right-click the `GitHubDevOpsLink` project in Solution Explorer
   - Select **Deploy**
   - ⚠️ **Important**: Just building the project is **not** sufficient - you must deploy it
   - Deploying registers the package with Windows and makes it discoverable by Command Palette

5. **Load the Extension in Command Palette**:
   - Open PowerToys Command Palette (**Win+Alt+Space**)
   - Type `Reload` and select **Reload Command Palette Extension**
   - Your extension should now appear in the Command Palette

6. **Test Your Extension**:
   - In Command Palette, type "GitHub" or "DevOps" to access your extension commands
   - The extension will appear at the bottom of the command list

7. **Making Changes**:
   - After making code changes, rebuild and redeploy the project
   - Run the **Reload** command in Command Palette to refresh the extension
   - Command Palette does not automatically detect package changes

> **Tip**: When debugging, always use **Deploy** rather than **Start** or **Run**. The "Run Unpackaged" option will not properly register the extension with Command Palette.

### Method 2: Build and Install from Source (Release Mode)

1. Open `GithubDevOpsLink.slnx` in Visual Studio
2. Select **Release** configuration and appropriate platform (x64 or ARM64)
3. Right-click the `GitHubDevOpsLink` project and select **Deploy**
4. Alternatively, build the project in Release mode and run the generated setup file from `setup-x64.iss` or `setup-arm64.iss`

## Configuration

### First Time Setup

#### GitHub Configuration

1. Open PowerToys Command Palette (default: **Win+Alt+Space**)
2. Type "GitHub" and select **GitHub Repositories**
3. Select **Configure GitHub**
4. Provide the following:
   - **Personal Access Token (PAT)**: Generate one at <https://github.com/settings/tokens>
     - Required scopes: `repo`, `read:org`, `read:user`
   - **Organization** (optional): Filter repositories by organization
   - **Team Names** (optional): Filter repositories by team membership (comma-separated)
   - **Topics** (optional): Filter repositories by topics (comma-separated)
5. Save your configuration

#### Azure DevOps Configuration

1. Open PowerToys Command Palette (**Win+Alt+Space**)
2. Type "DevOps" and select **Azure DevOps Pipelines**
3. Select **Configure Azure DevOps**
4. Provide the following:
   - **Organization**: Your Azure DevOps organization name (e.g., from `https://dev.azure.com/YOUR_ORG`)
   - **Project**: Your project name
   - **Personal Access Token (PAT)**: Generate one at `https://dev.azure.com/YOUR_ORG/_usersSettings/tokens`
     - Required scopes: `Build (Read)`, `Code (Read)`
5. Save your configuration

## Usage

### Accessing GitHub Repositories

1. Open Command Palette (**Win+Alt+Space**)
2. Type "GitHub Repositories" or simply "github"
3. Browse your repositories
4. Select a repository to see available actions:
   - Open in GitHub
   - Open in github.dev
   - View pull requests
   - View related pipelines

### Viewing Pull Requests

1. Open Command Palette
2. Type "GitHub Pull Requests" or "pr"
3. Browse all open pull requests across your repositories
4. Select a pull request to view details or open in GitHub

### Accessing Azure DevOps Pipelines

1. Open Command Palette
2. Type "Azure DevOps Pipelines" or "pipelines"
3. Browse your pipelines
4. Select a pipeline to see available actions:
   - Open pipeline definition
   - Open source repository
   - View latest build results

### Tips

- Data is cached locally and refreshed automatically every 10 minutes
- Use the "Refresh" option to manually update cached data
- Cache timestamp is displayed to show data freshness
- All data is stored locally in your AppData folder for offline access

## Data Storage

The extension stores configuration and cached data in:

```
%LOCALAPPDATA%\Packages\GitHubDevOpsLink_[random]\LocalState\
```

This includes:

- GitHub and Azure DevOps configurations (encrypted tokens)
- Cached repository and pipeline data
- Local SQLite database

## Troubleshooting

### Extension not appearing in Command Palette

- Verify PowerToys is installed and Command Palette is enabled in PowerToys Settings
- Ensure PowerToys is running (check system tray for PowerToys icon)
- Verify the extension is installed: Check Windows Settings > Apps
- Restart PowerToys from the system tray menu
- Re-register the extension by reinstalling the MSIX package
- Check Command Palette settings to ensure the extension is not disabled

### No repositories/pipelines showing

- Verify your Personal Access Token is valid
- Check token scopes/permissions
- Ensure you have access to the configured organization/project
- Try the "Configure" option to update your settings

### Data not refreshing

- Use the manual "Refresh" option from the repository or pipeline list
- Check your internet connection
- Verify your PAT hasn't expired

## Requirements

- Windows 10 version 19041 (20H1) or higher
- **Microsoft PowerToys** with Command Palette enabled and running
- .NET 10.0 runtime (Windows App SDK)
- Internet connection for initial data fetch (offline mode supported with cached data)

### About PowerToys Command Palette

PowerToys Command Palette is a quick launcher utility for Windows power users that provides access to commands, apps, and development tools from a single interface. This extension adds GitHub and Azure DevOps management capabilities to the Command Palette.

Learn more: [PowerToys Command Palette Documentation](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/overview)

## Development

Built with:

- .NET 10.0
- [Microsoft.CommandPalette.Extensions SDK](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/extensibility-overview) - PowerToys Command Palette extensibility framework
- Entity Framework Core for local caching
- Windows App SDK

### Extension Architecture

This extension follows the PowerToys Command Palette extensibility model, implementing custom command providers to add GitHub and Azure DevOps functionality to the Command Palette. For more information on building Command Palette extensions, see the [official documentation](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/samples).

## License

MIT

## Author

Soundar Anbalagan
