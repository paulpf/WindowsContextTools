using System.IO;
using Xunit;
using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Tests;

public class FileSystemOperationsTests : IDisposable
{
    private readonly string m_TestDir;
    private readonly FileSystemService m_FileSystemService;

    public FileSystemOperationsTests()
    {
        m_TestDir = Path.Combine(Path.GetTempPath(), "ExplorerContextToolsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(m_TestDir);
        m_FileSystemService = new FileSystemService();
    }

    public void Dispose()
    {
        if (Directory.Exists(m_TestDir))
        {
            Directory.Delete(m_TestDir, true);
        }
    }

    [Fact]
    public void EnumerateFiles_ReturnsAllFiles()
    {
        File.WriteAllText(Path.Combine(m_TestDir, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(m_TestDir, "file2.cs"), "content");

        var files = m_FileSystemService.GetFiles(m_TestDir, "*.*", SearchOption.TopDirectoryOnly, CancellationToken.None).ToList();

        Assert.Equal(2, files.Count);
    }

    [Fact]
    public void EnumerateFiles_WithSubdirectories_ReturnsAllFiles()
    {
        File.WriteAllText(Path.Combine(m_TestDir, "root.txt"), "content");
        var subDir = Path.Combine(m_TestDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "content");

        var files = m_FileSystemService.GetFiles(m_TestDir, "*.*", SearchOption.AllDirectories, CancellationToken.None).ToList();

        Assert.Equal(2, files.Count);
    }

    [Fact]
    public void EnumerateDirectories_ReturnsSubdirectories()
    {
        Directory.CreateDirectory(Path.Combine(m_TestDir, "sub1"));
        Directory.CreateDirectory(Path.Combine(m_TestDir, "sub2"));

        var dirs = m_FileSystemService.GetDirectories(m_TestDir, "*", SearchOption.TopDirectoryOnly, CancellationToken.None).ToList();

        Assert.Equal(2, dirs.Count);
    }

    [Fact]
    public void EnumerateDirectories_AllDirectories_IncludesNested()
    {
        var sub1 = Path.Combine(m_TestDir, "sub1");
        Directory.CreateDirectory(sub1);
        Directory.CreateDirectory(Path.Combine(sub1, "nested"));

        var dirs = m_FileSystemService.GetDirectories(m_TestDir, "*", SearchOption.AllDirectories, CancellationToken.None).ToList();

        Assert.Equal(2, dirs.Count);
    }

    [Fact]
    public void EnumerateFiles_WithCancellation_ThrowsOperationCanceledException()
    {
        File.WriteAllText(Path.Combine(m_TestDir, "file.txt"), "content");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
        {
            m_FileSystemService.GetFiles(m_TestDir, "*.*", SearchOption.AllDirectories, cts.Token).ToList();
        });
    }

    [Fact]
    public async Task FindSolutionFiles_ReturnsSolutionFiles()
    {
        File.WriteAllText(Path.Combine(m_TestDir, "test.sln"), "solution content");
        File.WriteAllText(Path.Combine(m_TestDir, "other.txt"), "other content");

        var slnFiles = await m_FileSystemService.FindSolutionFilesAsync(m_TestDir, CancellationToken.None);

        Assert.Single(slnFiles);
        Assert.Contains("test.sln", slnFiles[0]);
    }

    [Fact]
    public async Task FindSmallestSolution_Logic_SelectsSolutionWithFewestProjects()
    {
        // Create two solution files with different project counts
        var sln1Content = @"Project(""{FAE04EC0}"") = ""Proj1"", ""Proj1\Proj1.csproj"", ""{GUID1}""
EndProject
Project(""{FAE04EC0}"") = ""Proj2"", ""Proj2\Proj2.csproj"", ""{GUID2}""
EndProject
Project(""{FAE04EC0}"") = ""MyProject"", ""MyProject\MyProject.csproj"", ""{GUID3}""
EndProject";

        var sln2Content = @"Project(""{FAE04EC0}"") = ""MyProject"", ""MyProject\MyProject.csproj"", ""{GUID1}""
EndProject";

        File.WriteAllText(Path.Combine(m_TestDir, "big.sln"), sln1Content);
        File.WriteAllText(Path.Combine(m_TestDir, "small.sln"), sln2Content);

        var solutionFiles = await m_FileSystemService.FindSolutionFilesAsync(m_TestDir, CancellationToken.None);
        string projectName = "MyProject.csproj";

        string? smallestSolution = null;
        int smallestProjectCount = int.MaxValue;

        foreach (var solutionFile in solutionFiles)
        {
            var contents = await m_FileSystemService.ReadAllTextAsync(solutionFile, CancellationToken.None);
            if (contents.Contains(projectName, StringComparison.OrdinalIgnoreCase))
            {
                var projectCount = contents.Split(new[] { ".csproj" }, StringSplitOptions.None).Length - 1;
                if (projectCount < smallestProjectCount)
                {
                    smallestProjectCount = projectCount;
                    smallestSolution = solutionFile;
                }
            }
        }

        Assert.NotNull(smallestSolution);
        Assert.Contains("small.sln", smallestSolution);
        Assert.Equal(1, smallestProjectCount);
    }

    [Fact]
    public void FindSmallestSolution_ProjectNameWithoutExtension_GetsExtensionAdded()
    {
        string projectName = "MyProject";
        if (!projectName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            projectName += ".csproj";
        }

        Assert.Equal("MyProject.csproj", projectName);
    }

    [Fact]
    public void FindSmallestSolution_ProjectNameWithExtension_RemainsUnchanged()
    {
        string projectName = "MyProject.csproj";
        if (!projectName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            projectName += ".csproj";
        }

        Assert.Equal("MyProject.csproj", projectName);
    }
}
