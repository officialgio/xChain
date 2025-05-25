namespace ServiceNode;

/// <summary>
/// Tracks performance metrics for a Service Node.
/// </summary>
public class PerformanceTracker
{
    private int _messagesProcessed;
    private DateTime _startTime;

    public PerformanceTracker()
    {
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Tracks a processed message.
    /// </summary>
    public void TrackMessageProcessed()
    {
        _messagesProcessed++;
    }

    /// <summary>
    /// Calculates the average throughput (messages per second).
    /// </summary>
    public double GetThroughput()
    {
        var elapsedTime = DateTime.UtcNow - _startTime;
        return _messagesProcessed / elapsedTime.TotalSeconds;
    }
}