namespace WindowsExplorerContextTools.Services;

public interface IResultOutputService
{
    Task ShowInEditorAsync(IEnumerable<string> lines, CancellationToken cancellationToken);
    void ShowFileInExplorer(string filePath);
    IStreamingResultWriter CreateStreamingWriter(CancellationToken cancellationToken);
}

public interface IStreamingResultWriter : IAsyncDisposable
{
    string FilePath { get; }
    Task WriteLineAsync(string line);
    Task FlushAsync();
}
