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

		// Erstelle StreamingWriter sofort und zeige Link
		await using var writer = resultOutputService.CreateStreamingWriter(cancellationToken);
		context.Progress?.Report(new ProgressInfo(0, OutputFilePath: writer.FilePath));

		try
		{
			if (fileSystemService.IsSolidStateDrive(selectedPath))
			{
				var filesTask = Task.Run(() =>
				{
					foreach (var file in fileSystemService.GetFilesSafe(selectedPath, cancellationToken))
					{
						context.PauseToken.WaitIfPaused(cancellationToken);
						files.Add(file);
						context.CollectedResults.Add(file);
						context.Progress?.Report(new ProgressInfo(Interlocked.Increment(ref processedCount), OutputFilePath: writer.FilePath));
					}
				}, cancellationToken);

				var foldersTask = Task.Run(() =>
				{
					foreach (var folder in fileSystemService.GetDirectoriesSafe(selectedPath, cancellationToken))
					{
						context.PauseToken.WaitIfPaused(cancellationToken);
						folders.Add(folder);
						context.CollectedResults.Add(folder);
						context.Progress?.Report(new ProgressInfo(Interlocked.Increment(ref processedCount), OutputFilePath: writer.FilePath));
					}
				}, cancellationToken);

				await filesTask;
				await foldersTask;
			}
			else
			{
				await Task.Run(() =>
				{
					foreach (var file in fileSystemService.GetFilesSafe(selectedPath, cancellationToken))
					{
						context.PauseToken.WaitIfPaused(cancellationToken);
						files.Add(file);
						context.CollectedResults.Add(file);
						context.Progress?.Report(new ProgressInfo(++processedCount, OutputFilePath: writer.FilePath));
					}

					foreach (var folder in fileSystemService.GetDirectoriesSafe(selectedPath, cancellationToken))
					{
						context.PauseToken.WaitIfPaused(cancellationToken);
						folders.Add(folder);
						context.CollectedResults.Add(folder);
						context.Progress?.Report(new ProgressInfo(++processedCount, OutputFilePath: writer.FilePath));
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

		// Kombiniere und sortiere die Liste
		var combinedList = new List<string>();
		combinedList.AddRange(files);
		combinedList.AddRange(folders);
		combinedList.Sort();

		// Schreibe sortierte Liste zur Datei mit Streaming
		foreach (var item in combinedList)
		{
			await writer.WriteLineAsync(item);
		}
		await writer.FlushAsync();

		// Datei im Explorer zeigen
		resultOutputService.ShowFileInExplorer(writer.FilePath);

		return CommandResult.Success();
	}
}
