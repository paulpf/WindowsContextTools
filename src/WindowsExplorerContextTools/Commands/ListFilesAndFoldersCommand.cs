using System.IO;
using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Commands;

public class ListFilesAndFoldersCommand(IFileSystemService fileSystemService, IResultOutputService resultOutputService) : IToolCommand
{
	public string Name => "Create a list of files and folders";

    public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var selectedPath = context.SelectedPaths.FirstOrDefault();

        if (string.IsNullOrEmpty(selectedPath) || !fileSystemService.DirectoryExists(selectedPath))
        {
            return CommandResult.StayOpen("The selected path is not a folder.");
        }

        var files = new List<string>();
        var folders = new List<string>();
        int processedCount = 0;

        try
        {
            if (fileSystemService.IsSolidStateDrive(selectedPath))
            {
                var filesTask = Task.Run(() =>
                {
                    foreach (var file in fileSystemService.GetFiles(selectedPath, "*.*", SearchOption.AllDirectories, cancellationToken))
                    {
                        context.PauseToken.WaitIfPaused(cancellationToken);
                        files.Add(file);
                        context.CollectedResults.Add(file);
                        context.Progress?.Report(new ProgressInfo(Interlocked.Increment(ref processedCount)));
                    }
                }, cancellationToken);

                var foldersTask = Task.Run(() =>
                {
                    foreach (var folder in fileSystemService.GetDirectories(selectedPath, "*", SearchOption.AllDirectories, cancellationToken))
                    {
                        context.PauseToken.WaitIfPaused(cancellationToken);
                        folders.Add(folder);
                        context.CollectedResults.Add(folder);
                        context.Progress?.Report(new ProgressInfo(Interlocked.Increment(ref processedCount)));
                    }
                }, cancellationToken);

                await filesTask;
                await foldersTask;
            }
            else
            {
                await Task.Run(() =>
                {
                    foreach (var file in fileSystemService.GetFiles(selectedPath, "*.*", SearchOption.AllDirectories, cancellationToken))
                    {
                        context.PauseToken.WaitIfPaused(cancellationToken);
                        files.Add(file);
                        context.CollectedResults.Add(file);
                        context.Progress?.Report(new ProgressInfo(++processedCount));
                    }

                    foreach (var folder in fileSystemService.GetDirectories(selectedPath, "*", SearchOption.AllDirectories, cancellationToken))
                    {
                        context.PauseToken.WaitIfPaused(cancellationToken);
                        folders.Add(folder);
                        context.CollectedResults.Add(folder);
                        context.Progress?.Report(new ProgressInfo(++processedCount));
                    }
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            var partialList = new List<string>();
            partialList.AddRange(files);
            partialList.AddRange(folders);
            partialList.Sort();
            return CommandResult.Canceled(partialList);
        }

        var list = new List<string>();
        list.AddRange(files);
        list.AddRange(folders);
        list.Sort();

        await resultOutputService.ShowInEditorAsync(list, cancellationToken);

        return CommandResult.Success();
    }
}
