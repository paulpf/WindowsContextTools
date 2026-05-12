namespace WindowsExplorerContextTools.Commands;

public interface IToolCommand
{
    string Name { get; }
    Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken);
}
