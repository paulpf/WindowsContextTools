using WindowsExplorerContextTools.Commands;
using Xunit;

namespace WindowsExplorerContextTools.Tests;

public class PauseTokenTests
{
    [Fact]
    public void NewPauseTokenSource_IsNotPaused()
    {
        var source = new PauseTokenSource();

        Assert.False(source.IsPaused);
        Assert.False(source.Token.IsPaused);
    }

    [Fact]
    public void Pause_SetsPausedState()
    {
        var source = new PauseTokenSource();

        source.Pause();

        Assert.True(source.IsPaused);
        Assert.True(source.Token.IsPaused);
    }

    [Fact]
    public void Resume_ClearsPausedState()
    {
        var source = new PauseTokenSource();

        source.Pause();
        source.Resume();

        Assert.False(source.IsPaused);
        Assert.False(source.Token.IsPaused);
    }

    [Fact]
    public void Resume_WithoutPause_DoesNotThrow()
    {
        var source = new PauseTokenSource();

        var exception = Record.Exception(() => source.Resume());

        Assert.Null(exception);
    }

    [Fact]
    public void WaitIfPaused_WhenNotPaused_ReturnsImmediately()
    {
        var source = new PauseTokenSource();

        var exception = Record.Exception(() => source.Token.WaitIfPaused(CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task WaitIfPaused_WhenPaused_BlocksUntilResumed()
    {
        var source = new PauseTokenSource();
        source.Pause();
        var wasBlocked = false;

        var task = Task.Run(() =>
        {
            wasBlocked = true;
            source.Token.WaitIfPaused(CancellationToken.None);
            wasBlocked = false;
        });

        await Task.Delay(100);
        Assert.True(wasBlocked);

        source.Resume();
        await task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(wasBlocked);
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public async Task WaitIfPaused_WhenPausedAndCancelled_ThrowsOperationCanceledException()
    {
        var source = new PauseTokenSource();
        var cts = new CancellationTokenSource();
        source.Pause();

        var task = Task.Run(() => source.Token.WaitIfPaused(cts.Token));

        Thread.Sleep(50);
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public void DoublePause_DoesNotThrow()
    {
        var source = new PauseTokenSource();

        source.Pause();
        var exception = Record.Exception(() => source.Pause());

        Assert.Null(exception);
        Assert.True(source.IsPaused);
    }
}
