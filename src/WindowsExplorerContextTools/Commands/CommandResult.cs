namespace WindowsExplorerContextTools.Commands;

public class CommandResult
{
    public bool ShouldClose { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public bool WasCanceled { get; init; }
    public List<string>? PartialResults { get; init; }

    public static CommandResult Success() => new() { ShouldClose = true };
    public static CommandResult StayOpen(string? errorMessage = null) => new() { ShouldClose = false, ErrorMessage = errorMessage };
    public static CommandResult Canceled(List<string> partialResults) => new() { ShouldClose = false, WasCanceled = true, PartialResults = partialResults };
}
