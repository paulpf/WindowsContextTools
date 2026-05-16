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

    public IEnumerable<string> GetFilesSafe(string path, CancellationToken cancellationToken)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(path);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDirectory = pendingDirectories.Pop();

            List<string> files = [];
            try
            {
                files = Directory.EnumerateFiles(currentDirectory).ToList();
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return file;
            }

            List<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(currentDirectory).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var directory in directories)
            {
                pendingDirectories.Push(directory);
            }
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

    public IEnumerable<string> GetDirectoriesSafe(string path, CancellationToken cancellationToken)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(path);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDirectory = pendingDirectories.Pop();

            List<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(currentDirectory).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var directory in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                pendingDirectories.Push(directory);
                yield return directory;
            }
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

    public long GetFileSize(string path) => new FileInfo(path).Length;

    public Stream OpenRead(string path) => File.OpenRead(path);

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
