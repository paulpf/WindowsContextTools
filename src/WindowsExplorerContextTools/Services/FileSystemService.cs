using System.IO;

namespace WindowsExplorerContextTools.Services;

public class FileSystemService : IFileSystemService
{
    public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken)
    {
        foreach (var file in Directory.EnumerateFiles(path, searchPattern, searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return file;
        }
    }

    public IEnumerable<string> GetDirectories(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken)
    {
        foreach (var directory in Directory.EnumerateDirectories(path, searchPattern, searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return directory;
        }
    }

    public async Task<List<string>> FindSolutionFilesAsync(string path, CancellationToken cancellationToken)
    {
        return await Task.Run(() => Directory.EnumerateFiles(path, "*.sln", SearchOption.AllDirectories).ToList(), cancellationToken);
    }

    public async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
    {
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public bool IsSolidStateDrive(string path) => DriveTypeDetector.IsSolidStateDrive(path);

    public bool DirectoryExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (Directory.Exists(path))
        {
            return true;
        }

        // Directory.Exists can return false for network paths due to permissions/timeouts.
        // Try to actually access the directory as a fallback.
        try
        {
            Directory.EnumerateFileSystemEntries(path).Any();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
