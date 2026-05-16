using System.IO;
using System.Text;
using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Tests;

public class FakeFileSystemService : IFileSystemService
{
    private readonly List<string> m_Files;
    private readonly List<string> m_Directories;
    private readonly Dictionary<string, string> m_FileContents;
    private readonly Dictionary<string, byte[]> m_FileBytes;
    private readonly HashSet<string> m_LockedFiles;

    public FakeFileSystemService(
        List<string>? files = null,
        List<string>? directories = null,
        Dictionary<string, string>? fileContents = null,
        Dictionary<string, byte[]>? fileBytes = null,
        HashSet<string>? lockedFiles = null)
    {
        m_Files = files ?? [];
        m_Directories = directories ?? [];
        m_FileContents = fileContents ?? [];
        m_FileBytes = fileBytes ?? [];
        m_LockedFiles = lockedFiles ?? [];
    }

    public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken)
    {
        foreach (var file in m_Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return file;
        }
    }

    public IEnumerable<string> GetFilesSafe(string path, CancellationToken cancellationToken)
    {
        foreach (var file in m_Files.Where(file => IsSameOrUnderPath(file, path)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return file;
        }
    }

    public IEnumerable<string> GetDirectories(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken)
    {
        foreach (var dir in m_Directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return dir;
        }
    }

    public IEnumerable<string> GetDirectoriesSafe(string path, CancellationToken cancellationToken)
    {
        foreach (var dir in m_Directories.Where(dir => IsUnderPath(dir, path)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return dir;
        }
    }

    public Task<List<string>> FindSolutionFilesAsync(string path, CancellationToken cancellationToken)
    {
        var slnFiles = m_Files.Where(f => f.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult(slnFiles);
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
    {
        return Task.FromResult(m_FileContents.GetValueOrDefault(path, string.Empty));
    }

    public long GetFileSize(string path)
    {
        if (m_LockedFiles.Contains(path))
        {
            throw new IOException("File is locked.");
        }

        return GetBytes(path).Length;
    }

    public Stream OpenRead(string path)
    {
        if (m_LockedFiles.Contains(path))
        {
            throw new IOException("File is locked.");
        }

        return new MemoryStream(GetBytes(path), writable: false);
    }

    public bool DirectoryExists(string? path) => !string.IsNullOrWhiteSpace(path);

    public bool IsSolidStateDrive(string path) => true;

    private byte[] GetBytes(string path)
    {
        if (m_FileBytes.TryGetValue(path, out var bytes))
        {
            return bytes;
        }

        return Encoding.UTF8.GetBytes(m_FileContents.GetValueOrDefault(path, string.Empty));
    }

    private static bool IsSameOrUnderPath(string candidatePath, string parentPath)
    {
        return string.Equals(candidatePath, parentPath, StringComparison.OrdinalIgnoreCase)
            || IsUnderPath(candidatePath, parentPath);
    }

    private static bool IsUnderPath(string candidatePath, string parentPath)
    {
        var normalizedParent = parentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return candidatePath.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
    }
}
