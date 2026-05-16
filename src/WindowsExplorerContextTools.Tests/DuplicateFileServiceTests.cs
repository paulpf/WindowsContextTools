using WindowsExplorerContextTools.Commands;
using WindowsExplorerContextTools.Services;
using Xunit;

namespace WindowsExplorerContextTools.Tests;

public class DuplicateFileServiceTests
{
    [Fact]
    public async Task FindDuplicatesAsync_GroupsFilesBySizeAndHash()
    {
        var duplicateA = @"C:\TestDir\a.txt";
        var duplicateB = @"C:\TestDir\sub\b.txt";
        var sameSizeDifferentHash = @"C:\TestDir\c.txt";
        var uniqueSize = @"C:\TestDir\d.txt";
        var fs = new FakeFileSystemService(
            files: [duplicateA, duplicateB, sameSizeDifferentHash, uniqueSize],
            fileBytes: new Dictionary<string, byte[]>
            {
                [duplicateA] = [1, 2, 3],
                [duplicateB] = [1, 2, 3],
                [sameSizeDifferentHash] = [3, 2, 1],
                [uniqueSize] = [1, 2, 3, 4]
            });
        var service = new DuplicateFileService(fs);

        var result = await service.FindDuplicatesAsync(
            [@"C:\TestDir"],
            progress: null,
            new PauseTokenSource().Token,
            CancellationToken.None);

        var group = Assert.Single(result.FileGroups);
        Assert.Equal(3, group.FileSize);
        Assert.Equal(2, group.DuplicateCount);
        Assert.Equal(3, group.PotentialReclaimableSize);
        Assert.Contains(duplicateA, group.FilePaths);
        Assert.Contains(duplicateB, group.FilePaths);
        Assert.DoesNotContain(sameSizeDifferentHash, group.FilePaths);
        Assert.DoesNotContain(uniqueSize, group.FilePaths);
    }

    [Fact]
    public async Task FindDuplicatesAsync_SkipsLockedFiles()
    {
        var readableA = @"C:\TestDir\a.txt";
        var readableB = @"C:\TestDir\b.txt";
        var locked = @"C:\TestDir\locked.txt";
        var fs = new FakeFileSystemService(
            files: [readableA, readableB, locked],
            fileBytes: new Dictionary<string, byte[]>
            {
                [readableA] = [7, 7, 7],
                [readableB] = [7, 7, 7],
                [locked] = [7, 7, 7]
            },
            lockedFiles: [locked]);
        var service = new DuplicateFileService(fs);

        var result = await service.FindDuplicatesAsync(
            [@"C:\TestDir"],
            progress: null,
            new PauseTokenSource().Token,
            CancellationToken.None);

        var group = Assert.Single(result.FileGroups);
        Assert.Equal(2, group.DuplicateCount);
        Assert.DoesNotContain(locked, group.FilePaths);
    }

    [Fact]
    public async Task FindDuplicatesAsync_GroupsFoldersWithSameContentAndDifferentNames()
    {
        var folderA = @"C:\TestDir\CopyA";
        var folderB = @"C:\TestDir\CopyB";
        var folderC = @"C:\TestDir\Different";
        var fs = new FakeFileSystemService(
            files:
            [
                @$"{folderA}\readme.txt",
                @$"{folderA}\src\app.cs",
                @$"{folderB}\readme.txt",
                @$"{folderB}\src\app.cs",
                @$"{folderC}\readme.txt"
            ],
            directories:
            [
                folderA,
                @$"{folderA}\src",
                folderB,
                @$"{folderB}\src",
                folderC
            ],
            fileBytes: new Dictionary<string, byte[]>
            {
                [@$"{folderA}\readme.txt"] = [1, 2, 3],
                [@$"{folderA}\src\app.cs"] = [4, 5],
                [@$"{folderB}\readme.txt"] = [1, 2, 3],
                [@$"{folderB}\src\app.cs"] = [4, 5],
                [@$"{folderC}\readme.txt"] = [1, 2, 3]
            });
        var service = new DuplicateFileService(fs);

        var result = await service.FindDuplicatesAsync(
            [@"C:\TestDir"],
            progress: null,
            new PauseTokenSource().Token,
            CancellationToken.None);

        var group = Assert.Single(result.FolderGroups.Where(group => group.TotalSize == 5));
        Assert.Equal(2, group.DuplicateCount);
        Assert.Equal(5, group.PotentialReclaimableSize);
        Assert.Contains(folderA, group.FolderPaths);
        Assert.Contains(folderB, group.FolderPaths);
        Assert.DoesNotContain(folderC, group.FolderPaths);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var fs = new FakeFileSystemService(
            files: [@"C:\TestDir\a.txt"],
            fileBytes: new Dictionary<string, byte[]>
            {
                [@"C:\TestDir\a.txt"] = [1]
            });
        var service = new DuplicateFileService(fs);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.FindDuplicatesAsync([@"C:\TestDir"], null, new PauseTokenSource().Token, cts.Token));
    }
}
