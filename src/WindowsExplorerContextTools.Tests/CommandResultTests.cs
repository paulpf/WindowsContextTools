using WindowsExplorerContextTools.Commands;
using Xunit;

namespace WindowsExplorerContextTools.Tests;

public class CommandResultTests
{
    [Fact]
    public void Success_ShouldClose_IsTrue()
    {
        var result = CommandResult.Success();

        Assert.True(result.ShouldClose);
        Assert.Null(result.ErrorMessage);
        Assert.False(result.WasCanceled);
    }

    [Fact]
    public void StayOpen_ShouldClose_IsFalse()
    {
        var result = CommandResult.StayOpen("error");

        Assert.False(result.ShouldClose);
        Assert.Equal("error", result.ErrorMessage);
    }

    [Fact]
    public void StayOpen_WithoutMessage_HasNullError()
    {
        var result = CommandResult.StayOpen();

        Assert.False(result.ShouldClose);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Canceled_ContainsPartialResults()
    {
        var partial = new List<string> { "a", "b" };
        var result = CommandResult.Canceled(partial);

        Assert.False(result.ShouldClose);
        Assert.True(result.WasCanceled);
        Assert.Equal(2, result.PartialResults!.Count);
    }
}
