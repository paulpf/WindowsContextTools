namespace WindowsExplorerContextTools.Services;

public record DuplicateFileGroup(
    int GroupId,
    long FileSize,
    string Hash,
    IReadOnlyList<string> FilePaths)
{
    public int DuplicateCount => FilePaths.Count;
    public long PotentialReclaimableSize => FileSize * Math.Max(0, FilePaths.Count - 1);
}
