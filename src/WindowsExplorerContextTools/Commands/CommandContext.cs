using System.Collections.Concurrent;

namespace WindowsExplorerContextTools.Commands;

public class CommandContext
{
    public required List<string> SelectedPaths { get; init; }
    public required string CurrentPath { get; init; }
    public string InputText { get; init; } = string.Empty;
    public IProgress<ProgressInfo>? Progress { get; init; }
    public PauseToken PauseToken { get; init; }
    public ConcurrentBag<string> CollectedResults { get; } = [];
}
