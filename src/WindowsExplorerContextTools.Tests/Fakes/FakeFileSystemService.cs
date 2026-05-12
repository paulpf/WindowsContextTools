using System.IO;
using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Tests;

public class FakeFileSystemService : IFileSystemService
{
    private readonly List<string> m_Files;
    private readonly List<string> m_Directories;
    private readonly Dictionary<string, string> m_FileContents;

    public FakeFileSystemService(
        List<string>? files = null,
        List<string>? directories = null,
        Dictionary<string, string>? fileContents = null)
    {
        m_Files = files ?? [];
        m_Directories = directories ?? [];
        m_FileContents = fileContents ?? [];
    }

    public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken)
    {
        foreach (var file in m_Files)
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

    public Task<List<string>> FindSolutionFilesAsync(string path, CancellationToken cancellationToken)
    {
        var slnFiles = m_Files.Where(f => f.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult(slnFiles);
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
    {
        return Task.FromResult(m_FileContents.GetValueOrDefault(path, string.Empty));
    }

    public bool DirectoryExists(string? path) => !string.IsNullOrWhiteSpace(path);

    public bool IsSolidStateDrive(string path) => true;
}
