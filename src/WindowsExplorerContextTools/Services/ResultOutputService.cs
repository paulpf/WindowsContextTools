using System.Diagnostics;
using System.IO;
using System.Windows;
using Path = System.IO.Path;

namespace WindowsExplorerContextTools.Services;

public class ResultOutputService : IResultOutputService
{
    private const string NotepadPlusPlusPath = @"C:\Program Files\Notepad++\notepad++.exe";

    public async Task ShowInEditorAsync(IEnumerable<string> lines, CancellationToken cancellationToken)
    {
        var lineList = lines.ToList();

        string tempFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tempFile, lineList, cancellationToken);

        Clipboard.SetText(string.Join(Environment.NewLine, lineList));

        OpenInTextEditor(tempFile);
    }

    public void ShowFileInExplorer(string filePath)
    {
        Process.Start("explorer.exe", $"/select,\"{filePath}\"");
    }

    public void OpenFileInEditor(string filePath)
    {
        OpenInTextEditor(filePath);
    }

    public IStreamingResultWriter CreateStreamingWriter(CancellationToken cancellationToken)
    {
        return new StreamingResultWriter(cancellationToken);
    }

    private static void OpenInTextEditor(string filePath)
    {
        if (File.Exists(NotepadPlusPlusPath))
        {
            Process.Start(NotepadPlusPlusPath, filePath);
        }
        else
        {
            Process.Start("notepad.exe", filePath);
        }
    }
}

public class StreamingResultWriter : IStreamingResultWriter
{
    private readonly StreamWriter _writer;
    private readonly CancellationToken _cancellationToken;
    private int _lineCount;
    private const int BufferFlushInterval = 100;

    public string FilePath { get; }

    public StreamingResultWriter(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        FilePath = Path.GetTempFileName();

        _writer = new StreamWriter(FilePath, false, System.Text.Encoding.UTF8, bufferSize: 65536)
        {
            AutoFlush = false
        };

        _lineCount = 0;
    }

    public async Task WriteLineAsync(string line)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        await _writer.WriteLineAsync(line);
        _lineCount++;

        // Flush nach jedem BufferFlushInterval oder bei Cancellation
        if (_lineCount % BufferFlushInterval == 0)
        {
            await _writer.FlushAsync();
        }
    }

    public async Task FlushAsync()
    {
        await _writer.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.FlushAsync();
        _writer.Dispose();
    }
}
