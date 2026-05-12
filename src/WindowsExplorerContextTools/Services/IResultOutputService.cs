namespace WindowsExplorerContextTools.Services;

public interface IResultOutputService
{
    Task ShowInEditorAsync(IEnumerable<string> lines, CancellationToken cancellationToken);
    void ShowFileInExplorer(string filePath);
}
