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
		await using var writer = resultOutputService.CreateStreamingWriter(cancellationToken);

		// Zeige Output-Link sofort an
		context.Progress?.Report(new ProgressInfo(0, OutputFilePath: writer.FilePath));

		try
		{
			await Task.Run(() =>
			{
				foreach (var file in fileSystemService.GetFiles(selectedPath, "*.*", SearchOption.AllDirectories, cancellationToken))
				{
					context.PauseToken.WaitIfPaused(cancellationToken);
					files.Add(file);
					context.CollectedResults.Add(file);
					context.Progress?.Report(new ProgressInfo(files.Count, OutputFilePath: writer.FilePath));

					// Schreibe sofort zur Datei
					writer.WriteLineAsync(file).GetAwaiter().GetResult();
				}
			}, cancellationToken);

			await writer.FlushAsync();
		}
		catch (OperationCanceledException)
		{
			return CommandResult.Canceled(files);
		}

		// Datei im Explorer zeigen
		resultOutputService.ShowFileInExplorer(writer.FilePath);

		return CommandResult.Success();
	}
}

