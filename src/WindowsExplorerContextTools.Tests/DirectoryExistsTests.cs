using System.IO;
using WindowsExplorerContextTools.Services;
using Xunit;

namespace WindowsExplorerContextTools.Tests;

public class DirectoryExistsTests
{
    private readonly FileSystemService m_FileSystemService = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DirectoryExists_NullOrWhitespace_ReturnsFalse(string? path)
    {
        Assert.False(m_FileSystemService.DirectoryExists(path));
    }

    [Theory]
    [InlineData(@"Z:\nonexistent\path\that\does\not\exist")]
    [InlineData(@"C:\this_folder_should_not_exist_12345")]
    public void DirectoryExists_NonExistentPath_ReturnsFalse(string path)
    {
        Assert.False(m_FileSystemService.DirectoryExists(path));
    }

    [Theory]
    [InlineData(@"Y:\""")]
    [InlineData(@"C:\test""path")]
    public void DirectoryExists_InvalidPathCharacters_ReturnsFalse(string path)
    {
        Assert.False(m_FileSystemService.DirectoryExists(path));
    }

    [Fact]
    public void DirectoryExists_TempDirectory_ReturnsTrue()
    {
        var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        Assert.True(m_FileSystemService.DirectoryExists(tempDir));
    }
}
