namespace WindowsExplorerContextTools.Services;

public record DuplicateScanResult(
    IReadOnlyList<DuplicateFileGroup> FileGroups,
    IReadOnlyList<DuplicateFolderGroup> FolderGroups,
    IReadOnlyList<SkippedFileEntry> SkippedFiles);

public record SkippedFileEntry(string FilePath, string Reason);
