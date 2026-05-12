using System.IO;
using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Commands;

public class ListFoldersCommand(
	IFileSystemService fileSystemService,
	IResultOutputService resultOutputService,
	bool includeSubfolders = false)
	: IToolCommand
{
	public string Name => includeSubfolders
        ? "Create a list of all folders and subfolders"
        : "Create a list of all folders";

    public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var selectedPath = context.SelectedPaths.FirstOrDefault();

        if (string.IsNullOrEmpty(selectedPath) || !fileSystemService.DirectoryExists(selectedPath))
        {
            return CommandResult.StayOpen("The selected path is not a folder.");
        }

        var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var folders = new List<string>();

        try
        {
            await Task.Run(() =>
            {
                foreach (var folder in fileSystemService.GetDirectories(selectedPath, "*", searchOption, cancellationToken))
                {
                    context.PauseToken.WaitIfPaused(cancellationToken);
                    folders.Add(folder);
                    context.CollectedResults.Add(folder);
                    context.Progress?.Report(new ProgressInfo(folders.Count));
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Canceled(folders);
        }

        await resultOutputService.ShowInEditorAsync(folders, cancellationToken);

        return CommandResult.Success();
    }
}
