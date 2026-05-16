namespace WindowsExplorerContextTools.Commands;

public record ProgressInfo(int ProcessedCount, int? TotalCount = null, string? OutputFilePath = null, int? DuplicateCount = null);
