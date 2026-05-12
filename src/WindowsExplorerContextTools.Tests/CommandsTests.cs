using Xunit;
using WindowsExplorerContextTools.Commands;
using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools.Tests;

public class CommandsTests
{
    private static List<IToolCommand> CreateAllCommands()
    {
        var fs = new FileSystemService();
        var rs = new ResultOutputService();
        return
        [
            new ListFilesAndFoldersCommand(fs, rs),
            new ListFilesCommand(fs, rs),
            new ListFoldersCommand(fs, rs),
            new ListFoldersCommand(fs, rs, includeSubfolders: true),
            new FindSmallestSolutionCommand(fs, rs)
        ];
    }

    [Fact]
    public void AllCommands_HaveUniqueNames()
    {
        var commands = CreateAllCommands();
        var names = commands.Select(c => c.Name).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void AllCommands_ReturnsCorrectCount()
    {
        var commands = CreateAllCommands();
        Assert.Equal(5, commands.Count);
    }

    [Fact]
    public void CommandNames_HaveExpectedValues()
    {
        var commands = CreateAllCommands();
        var names = commands.Select(c => c.Name).ToList();

        Assert.Contains("Create a list of files and folders", names);
        Assert.Contains("Create a list of all files", names);
        Assert.Contains("Create a list of all folders", names);
        Assert.Contains("Create a list of all folders and subfolders", names);
        Assert.Contains("Find the smallest solution for the project", names);
    }
}
