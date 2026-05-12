using System.IO;
using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Commands;

public class ListFilesCommand(IFileSystemService fileSystemService, IResultOutputService resultOutputService)
	: IToolCommand
{
	public string Name => "Create a list of all files";

    public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var selectedPath = context.SelectedPaths.FirstOrDefault();

        if (string.IsNullOrEmpty(selectedPath) || !fileSystemService.DirectoryExists(selectedPath))
        {
            return CommandResult.StayOpen("The selected path is not a folder.");
        }

        var files = new List<string>();

        try
        {
            await Task.Run(() =>
            {
                foreach (var file in fileSystemService.GetFiles(selectedPath, "*.*", SearchOption.AllDirectories, cancellationToken))
                {
                    context.PauseToken.WaitIfPaused(cancellationToken);
                    files.Add(file);
                    context.CollectedResults.Add(file);
                    context.Progress?.Report(new ProgressInfo(files.Count));
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Canceled(files);
        }

        await resultOutputService.ShowInEditorAsync(files, cancellationToken);

        return CommandResult.Success();
    }
}
