using WindowsExplorerContextTools.Commands;

namespace WindowsExplorerContextTools.Services;

public interface IDuplicateFileService
{
    Task<DuplicateScanResult> FindDuplicatesAsync(
        IEnumerable<string> rootPaths,
        IProgress<ProgressInfo>? progress,
        PauseToken pauseToken,
        CancellationToken cancellationToken);
}
