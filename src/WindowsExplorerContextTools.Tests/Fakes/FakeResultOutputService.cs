using System.IO;
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
        ShowInEditorCallCount++; // Track for backward compatibility with tests
    }

    public IStreamingResultWriter CreateStreamingWriter(CancellationToken cancellationToken)
    {
        return new FakeStreamingResultWriter(this);
    }

    private class FakeStreamingResultWriter : IStreamingResultWriter
    {
        private readonly FakeResultOutputService _parent;
        private List<string> _lines = [];

        public string FilePath { get; }

        public FakeStreamingResultWriter(FakeResultOutputService parent)
        {
            _parent = parent;
            FilePath = Path.GetTempFileName();
        }

        public Task WriteLineAsync(string line)
        {
            _lines.Add(line);
            return Task.CompletedTask;
        }

        public Task FlushAsync()
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            // Set LastOutput for test compatibility
            _parent.LastOutput = _lines;
            return ValueTask.CompletedTask;
        }
    }
}
