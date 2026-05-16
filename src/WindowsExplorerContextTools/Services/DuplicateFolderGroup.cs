namespace WindowsExplorerContextTools.Services;

public record DuplicateFolderGroup(
    int GroupId,
    long TotalSize,
    string Hash,
    IReadOnlyList<string> FolderPaths)
{
    public int DuplicateCount => FolderPaths.Count;
    public long PotentialReclaimableSize => TotalSize * Math.Max(0, FolderPaths.Count - 1);
}
