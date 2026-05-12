using WindowsExplorerContextTools.Commands;
using Xunit;

namespace WindowsExplorerContextTools.Tests;

public class CommandExecutionTests
{
    private static CommandContext CreateContext(List<string>? selectedPaths = null)
    {
        return new CommandContext
        {
            SelectedPaths = selectedPaths ?? [@"C:\TestDir"],
            CurrentPath = selectedPaths?.FirstOrDefault() ?? @"C:\TestDir",
            InputText = string.Empty,
            PauseToken = new PauseTokenSource().Token
        };
    }

    [Fact]
    public async Task ListFilesCommand_WithInvalidPath_ReturnsError()
    {
        var fs = new FakeFileSystemService { };
        var rs = new FakeResultOutputService();
        var command = new ListFilesCommand(fs, rs);

        var context = CreateContext([""]);
        var result = await command.ExecuteAsync(context, CancellationToken.None);

        Assert.False(result.ShouldClose);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ListFilesCommand_ReturnsFilesAndShowsInEditor()
    {
        var files = new List<string> { @"C:\TestDir\a.txt", @"C:\TestDir\b.cs" };
        var fs = new FakeFileSystemService(files: files);
        var rs = new FakeResultOutputService();
        var command = new ListFilesCommand(fs, rs);

        var result = await command.ExecuteAsync(CreateContext(), CancellationToken.None);

        Assert.True(result.ShouldClose);
        Assert.Equal(1, rs.ShowInEditorCallCount);
        Assert.Equal(2, rs.LastOutput.Count);
    }

    [Fact]
    public async Task ListFilesCommand_PopulatesCollectedResults()
    {
        var files = new List<string> { @"C:\TestDir\a.txt", @"C:\TestDir\b.cs" };
        var fs = new FakeFileSystemService(files: files);
        var rs = new FakeResultOutputService();
        var command = new ListFilesCommand(fs, rs);
        var context = CreateContext();

        await command.ExecuteAsync(context, CancellationToken.None);

        Assert.Equal(2, context.CollectedResults.Count);
    }

    [Fact]
    public async Task ListFoldersCommand_ReturnsFolders()
    {
        var dirs = new List<string> { @"C:\TestDir\sub1", @"C:\TestDir\sub2" };
        var fs = new FakeFileSystemService(directories: dirs);
        var rs = new FakeResultOutputService();
        var command = new ListFoldersCommand(fs, rs, includeSubfolders: true);

        var result = await command.ExecuteAsync(CreateContext(), CancellationToken.None);

        Assert.True(result.ShouldClose);
        Assert.Equal(2, rs.LastOutput.Count);
    }

    [Fact]
    public async Task ListFilesAndFoldersCommand_ReturnsCombinedAndSorted()
    {
        var files = new List<string> { @"C:\TestDir\b.txt" };
        var dirs = new List<string> { @"C:\TestDir\a_dir" };
        var fs = new FakeFileSystemService(files: files, directories: dirs);
        var rs = new FakeResultOutputService();
        var command = new ListFilesAndFoldersCommand(fs, rs);

        var result = await command.ExecuteAsync(CreateContext(), CancellationToken.None);

        Assert.True(result.ShouldClose);
        Assert.Equal(2, rs.LastOutput.Count);
        Assert.Equal(@"C:\TestDir\a_dir", rs.LastOutput[0]);
        Assert.Equal(@"C:\TestDir\b.txt", rs.LastOutput[1]);
    }

    [Fact]
    public async Task ListFilesCommand_WithCancellation_ReturnsCanceledWithPartialResults()
    {
        var cts = new CancellationTokenSource();
        var files = new List<string> { @"C:\TestDir\a.txt", @"C:\TestDir\b.txt", @"C:\TestDir\c.txt" };
        var fs = new FakeFileSystemService(files: files);
        var rs = new FakeResultOutputService();
        var command = new ListFilesCommand(fs, rs);

        cts.Cancel();
        var result = await command.ExecuteAsync(CreateContext(), cts.Token);

        Assert.True(result.WasCanceled);
        Assert.Equal(0, rs.ShowInEditorCallCount);
    }

    [Fact]
    public async Task FindSmallestSolutionCommand_FindsSmallestSolution()
    {
        var bigSln = @"C:\TestDir\big.sln";
        var smallSln = @"C:\TestDir\small.sln";

        var bigContent = @"Project(""{FAE04EC0}"") = ""Proj1"", ""Proj1\Proj1.csproj""
Project(""{FAE04EC0}"") = ""Proj2"", ""Proj2\Proj2.csproj""
Project(""{FAE04EC0}"") = ""MyProject"", ""MyProject\MyProject.csproj""";

        var smallContent = @"Project(""{FAE04EC0}"") = ""MyProject"", ""MyProject\MyProject.csproj""";

        var fs = new FakeFileSystemService(
            files: [bigSln, smallSln],
            fileContents: new Dictionary<string, string>
            {
                [bigSln] = bigContent,
                [smallSln] = smallContent
            });
        var rs = new FakeResultOutputService();
        var command = new FindSmallestSolutionCommand(fs, rs);

        var context = CreateContext();
        context = new CommandContext
        {
            SelectedPaths = context.SelectedPaths,
            CurrentPath = context.CurrentPath,
            InputText = "MyProject",
            PauseToken = context.PauseToken
        };

        var result = await command.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result.ShouldClose);
        Assert.Equal(smallSln, rs.LastExplorerFilePath);
    }

    [Fact]
    public async Task FindSmallestSolutionCommand_NoMatchingProject_ReturnsError()
    {
        var sln = @"C:\TestDir\test.sln";
        var fs = new FakeFileSystemService(
            files: [sln],
            fileContents: new Dictionary<string, string>
            {
                [sln] = @"Project = ""Other\Other.csproj"""
            });
        var rs = new FakeResultOutputService();
        var command = new FindSmallestSolutionCommand(fs, rs);

        var context = new CommandContext
        {
            SelectedPaths = [@"C:\TestDir"],
            CurrentPath = @"C:\TestDir",
            InputText = "Missing",
            PauseToken = new PauseTokenSource().Token
        };

        var result = await command.ExecuteAsync(context, CancellationToken.None);

        Assert.False(result.ShouldClose);
        Assert.Contains("Missing.csproj", result.ErrorMessage);
    }
}
