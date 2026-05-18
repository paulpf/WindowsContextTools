using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Commands;

public class FindDuplicateFilesCommand(
    IFileSystemService fileSystemService,
    IDuplicateFileService duplicateFileService,
    IResultOutputService resultOutputService) : IToolCommand
{
    public string Name => "Find duplicate files";

    public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var selectedDirectories = context.SelectedPaths
            .Where(fileSystemService.DirectoryExists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedDirectories.Count == 0)
        {
            return CommandResult.StayOpen("Select at least one folder or drive.");
        }

        // Erstelle StreamingWriter sofort und zeige Link in UI
        await using var writer = resultOutputService.CreateStreamingWriter(cancellationToken);
        context.Progress?.Report(new ProgressInfo(0, OutputFilePath: writer.FilePath));

        DuplicateScanResult duplicateScanResult;
        try
        {
            duplicateScanResult = await duplicateFileService.FindDuplicatesAsync(
                selectedDirectories,
                context.Progress,
                context.PauseToken,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Canceled(context.CollectedResults.ToList());
        }

        var report = CreateReport(selectedDirectories, duplicateScanResult);

        foreach (var line in report)
        {
            context.CollectedResults.Add(line);
            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync();

        // Datei im Editor öffnen
        resultOutputService.OpenFileInEditor(writer.FilePath);

        return CommandResult.Success();
    }

    private static List<string> CreateReport(IReadOnlyList<string> selectedDirectories, DuplicateScanResult duplicateScanResult)
    {
        var duplicateFileGroups = duplicateScanResult.FileGroups;
        var duplicateFolderGroups = duplicateScanResult.FolderGroups;
        var skippedFilesCount = duplicateScanResult.SkippedFiles.Count;
        var report = new List<string>
        {
            "Duplicate file report",
            $"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}",
            $"Scanned paths: {selectedDirectories.Count}",
            $"Duplicate file groups: {duplicateFileGroups.Count}",
            $"Duplicate folder groups: {duplicateFolderGroups.Count}",
            $"Potential file reclaimable size: {duplicateFileGroups.Sum(group => group.PotentialReclaimableSize)} bytes",
            $"Potential folder reclaimable size: {duplicateFolderGroups.Sum(group => group.PotentialReclaimableSize)} bytes",
            $"Skipped files (access errors): {skippedFilesCount}",
            string.Empty
        };

        report.AddRange(selectedDirectories.Select(path => $"Path: {path}"));
        report.Add(string.Empty);

        report.Add("Duplicate files");
        report.Add(string.Empty);

        if (duplicateFileGroups.Count == 0)
        {
            report.Add("No duplicate files found.");
        }
        else
        {
            foreach (var group in duplicateFileGroups)
            {
                report.Add($"Group id: {group.GroupId}");
                report.Add($"File size: {group.FileSize} bytes");
                report.Add($"Hash: {group.Hash}");
                report.Add($"Duplicate count: {group.DuplicateCount}");
                report.Add($"Potential reclaimable size: {group.PotentialReclaimableSize} bytes");
                report.Add("File paths:");

                foreach (var filePath in group.FilePaths)
                {
                    report.Add($"  {filePath}");
                }

                report.Add(string.Empty);
            }
        }

        report.Add(string.Empty);
        report.Add("Duplicate folders");
        report.Add(string.Empty);

        if (duplicateFolderGroups.Count == 0)
        {
            report.Add("No duplicate folders found.");
            return report;
        }

        foreach (var group in duplicateFolderGroups)
        {
            report.Add($"Folder group id: {group.GroupId}");
            report.Add($"Total size: {group.TotalSize} bytes");
            report.Add($"Hash: {group.Hash}");
            report.Add($"Duplicate count: {group.DuplicateCount}");
            report.Add($"Potential reclaimable size: {group.PotentialReclaimableSize} bytes");
            report.Add("Folder paths:");

            foreach (var folderPath in group.FolderPaths)
            {
                report.Add($"  {folderPath}");
            }

            report.Add(string.Empty);
        }

        if (duplicateScanResult.SkippedFiles.Count > 0)
        {
            report.Add(string.Empty);
            report.Add("Skipped files (could not be read)");
            report.Add(string.Empty);

            foreach (var skipped in duplicateScanResult.SkippedFiles)
            {
                report.Add($"  {skipped.FilePath} - {skipped.Reason}");
            }
        }

        return report;
    }
}
