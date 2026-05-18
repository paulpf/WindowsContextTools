using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Commands;

public class FindSmallestSolutionCommand(IFileSystemService fileSystemService, IResultOutputService resultOutputService) : IToolCommand
{
	public string Name => "Find the smallest solution for the project";

	public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
	{
		string projectName = context.InputText;

		if (!fileSystemService.DirectoryExists(context.CurrentPath))
		{
			return CommandResult.StayOpen("The selected path is not a folder.");
		}

		context.Progress?.Report(new ProgressInfo(0));
		var solutionFiles = await fileSystemService.FindSolutionFilesAsync(context.CurrentPath, cancellationToken);

		if (solutionFiles.Count == 0)
		{
			return CommandResult.StayOpen("No solution files found in the specified path.");
		}

		if (!projectName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
		{
			projectName += ".csproj";
		}

		string? smallestSolution = await FindSmallestSolutionAsync(solutionFiles, projectName, fileSystemService, context.Progress, cancellationToken);

		if (smallestSolution != null)
		{
			resultOutputService.OpenFileInEditor(smallestSolution);
			return CommandResult.Success();
		}

		return CommandResult.StayOpen($"No solution containing the project '{projectName}' was found.");
	}

	private static async Task<string?> FindSmallestSolutionAsync(
		List<string> solutionFiles,
		string projectName,
		IFileSystemService fileSystemService,
		IProgress<ProgressInfo>? progress,
		CancellationToken cancellationToken)
	{
		string? smallestSolution = null;
		int smallestProjectCount = int.MaxValue;
		int processed = 0;

		await Parallel.ForEachAsync(solutionFiles,
			new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount },
			async (solutionFile, ct) =>
			{
				var currentProcessed = Interlocked.Increment(ref processed);
				progress?.Report(new ProgressInfo(currentProcessed, solutionFiles.Count));

				var contents = await fileSystemService.ReadAllTextAsync(solutionFile, ct);

				if (!contents.Contains(projectName, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				var projectCount = CountOccurrences(contents, ".csproj");

				lock (solutionFiles)
				{
					if (projectCount < smallestProjectCount)
					{
						smallestProjectCount = projectCount;
						smallestSolution = solutionFile;
					}
				}
			});

		return smallestSolution;
	}

	private static int CountOccurrences(string text, string value)
	{
		int count = 0;
		int index = 0;

		while ((index = text.IndexOf(value, index, StringComparison.OrdinalIgnoreCase)) != -1)
		{
			count++;
			index += value.Length;
		}

		return count;
	}
}

