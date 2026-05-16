namespace WindowsExplorerContextTools.Services;

public record DuplicateScanResult(
    IReadOnlyList<DuplicateFileGroup> FileGroups,
    IReadOnlyList<DuplicateFolderGroup> FolderGroups);
