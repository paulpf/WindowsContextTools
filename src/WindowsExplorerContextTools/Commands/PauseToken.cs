namespace WindowsExplorerContextTools.Commands;

public class PauseTokenSource
{
    private volatile ManualResetEventSlim? m_PauseEvent;

    public PauseToken Token => new(this);

    public bool IsPaused => m_PauseEvent != null;

    public void Pause()
    {
        Interlocked.CompareExchange(ref m_PauseEvent, new ManualResetEventSlim(false), null);
    }

    public void Resume()
    {
        var evt = Interlocked.Exchange(ref m_PauseEvent, null);
        evt?.Set();
        evt?.Dispose();
    }

    internal void WaitIfPaused(CancellationToken cancellationToken)
    {
        m_PauseEvent?.Wait(cancellationToken);
    }
}

public readonly struct PauseToken(PauseTokenSource source)
{
    private readonly PauseTokenSource m_Source = source;

    public bool IsPaused => m_Source.IsPaused;

    public void WaitIfPaused(CancellationToken cancellationToken)
    {
        m_Source.WaitIfPaused(cancellationToken);
    }
}
