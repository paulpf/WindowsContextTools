using System.IO;

namespace WindowsExplorerContextTools.Services;

public interface IFileSystemService
{
    IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken);
    IEnumerable<string> GetFilesSafe(string path, CancellationToken cancellationToken);
    IEnumerable<string> GetDirectories(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken);
    IEnumerable<string> GetDirectoriesSafe(string path, CancellationToken cancellationToken);
    Task<List<string>> FindSolutionFilesAsync(string path, CancellationToken cancellationToken);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken);
    long GetFileSize(string path);
    Stream OpenRead(string path);
    bool DirectoryExists(string? path);
    bool IsSolidStateDrive(string path);
}
