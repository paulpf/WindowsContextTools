using System.IO;
using System.Security.Cryptography;
using WindowsExplorerContextTools.Commands;

namespace WindowsExplorerContextTools.Services;

public class DuplicateFileService(IFileSystemService fileSystemService) : IDuplicateFileService
{
    public async Task<DuplicateScanResult> FindDuplicatesAsync(
        IEnumerable<string> rootPaths,
        IProgress<ProgressInfo>? progress,
        PauseToken pauseToken,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var processedCount = 0;
            var duplicateCount = 0;
            var fileEntries = new List<FileEntry>();
            var filesBySize = new Dictionary<long, List<string>>();
            var validRootPaths = rootPaths
                .Where(fileSystemService.DirectoryExists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var rootPath in validRootPaths)
            {
                foreach (var file in fileSystemService.GetFilesSafe(rootPath, cancellationToken))
                {
                    pauseToken.WaitIfPaused(cancellationToken);

                    if (!TryGetFileSize(file, out var fileSize))
                    {
                        continue;
                    }

                    if (!filesBySize.TryGetValue(fileSize, out var files))
                    {
                        files = [];
                        filesBySize[fileSize] = files;
                    }

                    files.Add(file);
                    fileEntries.Add(new FileEntry(file, fileSize));
                    progress?.Report(new ProgressInfo(++processedCount, DuplicateCount: duplicateCount));
                }
            }

            var duplicateCandidates = filesBySize
                .Where(group => group.Value.Count > 1)
                .SelectMany(group => group.Value.Select(file => (Size: group.Key, File: file)));

            var filesByHash = new Dictionary<(long Size, string Hash), List<string>>();

            foreach (var candidate in duplicateCandidates)
            {
                pauseToken.WaitIfPaused(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (!TryComputeHash(candidate.File, out var hash))
                {
                    continue;
                }

                var key = (candidate.Size, hash);
                if (!filesByHash.TryGetValue(key, out var files))
                {
                    files = [];
                    filesByHash[key] = files;
                }
                else if (files.Count >= 1)
                {
                    // Die neue Datei ist ein Duplikat (mindestens eine andere Datei mit gleichem Hash existiert)
                    duplicateCount++;
                }

                files.Add(candidate.File);
                progress?.Report(new ProgressInfo(++processedCount, DuplicateCount: duplicateCount));
            }

            var groupId = 1;
            var duplicateFileGroups = filesByHash
                .Where(group => group.Value.Count > 1)
                .OrderByDescending(group => group.Key.Size)
                .ThenBy(group => group.Key.Hash, StringComparer.Ordinal)
                .Select(group => new DuplicateFileGroup(
                    groupId++,
                    group.Key.Size,
                    group.Key.Hash,
                    group.Value.Order(StringComparer.OrdinalIgnoreCase).ToList()))
                .ToList();

            var duplicateFolderGroups = FindDuplicateFolders(validRootPaths, fileEntries, progress, pauseToken, ref processedCount, ref duplicateCount, cancellationToken);

            return new DuplicateScanResult(duplicateFileGroups, duplicateFolderGroups);
        }, cancellationToken);
    }

    private List<DuplicateFolderGroup> FindDuplicateFolders(
        IReadOnlyList<string> rootPaths,
        IReadOnlyList<FileEntry> fileEntries,
        IProgress<ProgressInfo>? progress,
        PauseToken pauseToken,
        ref int processedCount,
        ref int duplicateCount,
        CancellationToken cancellationToken)
    {
        var foldersByManifest = new Dictionary<string, List<FolderCandidate>>();
        var folders = EnumerateFolders(rootPaths, cancellationToken)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var folder in folders)
        {
            pauseToken.WaitIfPaused(cancellationToken);

            var candidate = BuildFolderManifest(folder, fileEntries, folders);

            if (!foldersByManifest.TryGetValue(candidate.Manifest, out var candidates))
            {
                candidates = [];
                foldersByManifest[candidate.Manifest] = candidates;
            }

            candidates.Add(candidate);
            progress?.Report(new ProgressInfo(++processedCount, DuplicateCount: duplicateCount));
        }

        var foldersByHash = new Dictionary<(long TotalSize, string Hash), List<string>>();

        foreach (var candidate in foldersByManifest
            .Where(group => group.Value.Count > 1)
            .SelectMany(group => group.Value))
        {
            pauseToken.WaitIfPaused(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryComputeFolderHash(candidate, out var hash))
            {
                continue;
            }

            var key = (candidate.TotalSize, hash);
            if (!foldersByHash.TryGetValue(key, out var duplicateFolders))
            {
                duplicateFolders = [];
                foldersByHash[key] = duplicateFolders;
            }
            else if (duplicateFolders.Count >= 1)
            {
                // Diese Folder ist ein Duplikat
                duplicateCount++;
            }

            duplicateFolders.Add(candidate.FolderPath);
            progress?.Report(new ProgressInfo(++processedCount, DuplicateCount: duplicateCount));
        }

        var groupId = 1;
        return foldersByHash
            .Where(group => group.Value.Count > 1)
            .OrderByDescending(group => group.Key.TotalSize)
            .ThenBy(group => group.Key.Hash, StringComparer.Ordinal)
            .Select(group => new DuplicateFolderGroup(
                groupId++,
                group.Key.TotalSize,
                group.Key.Hash,
                group.Value.Order(StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();
    }

    private IEnumerable<string> EnumerateFolders(IReadOnlyList<string> rootPaths, CancellationToken cancellationToken)
    {
        foreach (var rootPath in rootPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return rootPath;

            foreach (var directory in fileSystemService.GetDirectoriesSafe(rootPath, cancellationToken))
            {
                yield return directory;
            }
        }
    }

    private FolderCandidate BuildFolderManifest(string folder, IReadOnlyList<FileEntry> fileEntries, IReadOnlyList<string> folders)
    {
        var entries = new List<string>();
        var folderFileEntries = new List<FolderFileEntry>();
        long totalSize = 0;

        foreach (var childFolder in folders.Where(childFolder => IsUnderPath(childFolder, folder)))
        {
            entries.Add($"D:{NormalizePath(Path.GetRelativePath(folder, childFolder))}");
        }

        foreach (var fileEntry in fileEntries.Where(fileEntry => IsUnderPath(fileEntry.Path, folder)))
        {
            var relativePath = NormalizePath(Path.GetRelativePath(folder, fileEntry.Path));
            entries.Add($"F:{relativePath}:{fileEntry.Size}");
            folderFileEntries.Add(new FolderFileEntry(relativePath, fileEntry.Path, fileEntry.Size));
            totalSize += fileEntry.Size;
        }

        entries.Sort(StringComparer.OrdinalIgnoreCase);
        folderFileEntries.Sort((left, right) => string.Compare(left.RelativePath, right.RelativePath, StringComparison.OrdinalIgnoreCase));

        return new FolderCandidate(folder, string.Join("|", entries), totalSize, folderFileEntries);
    }

    private bool TryComputeFolderHash(FolderCandidate candidate, out string hash)
    {
        try
        {
            using var sha256 = SHA256.Create();
            AddToHash(sha256, candidate.Manifest);

            foreach (var file in candidate.Files)
            {
                AddToHash(sha256, file.RelativePath);
                AddToHash(sha256, file.Size.ToString());

                using var stream = fileSystemService.OpenRead(file.FilePath);
                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
            }

            sha256.TransformFinalBlock([], 0, 0);
            hash = Convert.ToHexString(sha256.Hash ?? []);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            hash = string.Empty;
            return false;
        }
        catch (IOException)
        {
            hash = string.Empty;
            return false;
        }
    }

    private static void AddToHash(HashAlgorithm hashAlgorithm, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        hashAlgorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
        hashAlgorithm.TransformBlock(new byte[] { 0 }, 0, 1, null, 0);
    }

    private static string NormalizePath(string path) => path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

    private static bool IsUnderPath(string candidatePath, string parentPath)
    {
        var normalizedParent = parentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return candidatePath.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetFileSize(string file, out long fileSize)
    {
        try
        {
            fileSize = fileSystemService.GetFileSize(file);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            fileSize = 0;
            return false;
        }
        catch (IOException)
        {
            fileSize = 0;
            return false;
        }
    }

    private bool TryComputeHash(string file, out string hash)
    {
        try
        {
            using var stream = fileSystemService.OpenRead(file);
            hash = Convert.ToHexString(SHA256.HashData(stream));
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            hash = string.Empty;
            return false;
        }
        catch (IOException)
        {
            hash = string.Empty;
            return false;
        }
    }

    private record struct FileEntry(string Path, long Size);

    private record struct FolderCandidate(string FolderPath, string Manifest, long TotalSize, List<FolderFileEntry> Files);

    private record struct FolderFileEntry(string RelativePath, string FilePath, long Size);
}
