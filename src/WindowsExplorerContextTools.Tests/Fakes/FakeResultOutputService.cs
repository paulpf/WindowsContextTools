using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Tests;

public class FakeResultOutputService : IResultOutputService
{
    public List<string> LastOutput { get; private set; } = [];
    public int ShowInEditorCallCount { get; private set; }
    public string? LastExplorerFilePath { get; private set; }

    public Task ShowInEditorAsync(IEnumerable<string> lines, CancellationToken cancellationToken)
    {
        LastOutput = lines.ToList();
        ShowInEditorCallCount++;
        return Task.CompletedTask;
    }

    public void ShowFileInExplorer(string filePath)
    {
        LastExplorerFilePath = filePath;
    }
}
