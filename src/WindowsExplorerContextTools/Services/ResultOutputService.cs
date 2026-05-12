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
