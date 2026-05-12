using WindowsExplorerContextTools.Services;
using Xunit;

namespace WindowsExplorerContextTools.Tests;

public class PathCheckerTests
{
    [Theory]
    [InlineData(@"C:\folder\file.txt", true)]
    [InlineData(@"C:\folder\file.cs", true)]
    [InlineData(@"C:\folder\archive.tar.gz", true)]
    [InlineData(@"C:\folder\document.pdf", true)]
    [InlineData(@"file.txt", true)]
    public void IsFile_WithFilePaths_ReturnsTrue(string path, bool expected)
    {
        Assert.Equal(expected, PathChecker.IsFile(path));
    }

    [Theory]
    [InlineData(@"C:\folder\subfolder")]
    [InlineData(@"C:\folder")]
    [InlineData(@"C:\")]
    public void IsFile_WithFolderPaths_ReturnsFalse(string path)
    {
        Assert.False(PathChecker.IsFile(path));
    }

    [Theory]
    [InlineData(@"C:\folder\subfolder")]
    [InlineData(@"C:\folder")]
    [InlineData(@"C:\")]
    public void IsFolder_WithFolderPaths_ReturnsTrue(string path)
    {
        Assert.True(PathChecker.IsFolder(path));
    }

    [Theory]
    [InlineData(@"C:\folder\file.txt")]
    [InlineData(@"C:\folder\file.cs")]
    [InlineData(@"file.txt")]
    public void IsFolder_WithFilePaths_ReturnsFalse(string path)
    {
        Assert.False(PathChecker.IsFolder(path));
    }

    [Fact]
    public void IsFile_And_IsFolder_AreOpposites()
    {
        var paths = new[]
        {
            @"C:\folder\file.txt",
            @"C:\folder\subfolder",
            @"C:\test.log",
            @"C:\Users"
        };

        foreach (var path in paths)
        {
            Assert.NotEqual(PathChecker.IsFile(path), PathChecker.IsFolder(path));
        }
    }
}
