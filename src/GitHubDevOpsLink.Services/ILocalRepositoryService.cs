using GitHubDevOpsLink.Services.Models;

namespace GitHubDevOpsLink.Services;

public interface ILocalRepositoryService
{
    Task ScanAndLinkRepositoriesAsync(string workFolderPath, string owner);
    Task<string?> GetLocalPathForRepositoryAsync(long repositoryId);
    Task<bool> OpenInVSCodeAsync(string localPath);
    Task<bool> OpenInVisualStudioAsync(string localPath);
    Task<string?> CloneRepositoryAsync(string cloneUrl, string workFolderPath, string repositoryName, long repositoryId);
    bool HasSolutionFile(string localPath);
    Task<bool> OpenInFileExplorerAsync(string localPath);
    Task<bool> OpenInTerminalAsync(string localPath);
    Task<int> CleanupInvalidLinkedRepositoriesAsync(string owner);
}
