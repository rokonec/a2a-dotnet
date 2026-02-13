using System.Threading.Channels;

namespace A2A;

/// <summary>
/// Enumerator for streaming task update events to clients.
/// </summary>
public sealed class TaskUpdateEventEnumerator : IAsyncEnumerable<StreamResponse>, IDisposable, IAsyncDisposable
{
    private readonly Channel<StreamResponse> _channel = Channel.CreateUnbounded<StreamResponse>();

    /// <summary>
    /// Gets or sets the processing task to prevent garbage collection.
    /// </summary>
    public Task? ProcessingTask { get; set; }

    /// <summary>
    /// Notifies of a new event in the task stream.
    /// </summary>
    /// <param name="taskUpdateEvent">The event to notify.</param>
    public void NotifyEvent(StreamResponse taskUpdateEvent)
    {
        if (taskUpdateEvent is null)
        {
            throw new ArgumentNullException(nameof(taskUpdateEvent));
        }

        if (!_channel.Writer.TryWrite(taskUpdateEvent))
        {
            throw new InvalidOperationException("Unable to write to the event channel.");
        }
    }

    /// <summary>
    /// Notifies of the final event in the task stream.
    /// </summary>
    /// <param name="taskUpdateEvent">The final event to notify.</param>
    public void NotifyFinalEvent(StreamResponse taskUpdateEvent)
    {
        if (taskUpdateEvent is null)
        {
            throw new ArgumentNullException(nameof(taskUpdateEvent));
        }

        if (!_channel.Writer.TryWrite(taskUpdateEvent))
        {
            throw new InvalidOperationException("Unable to write to the event channel.");
        }

        _channel.Writer.TryComplete();
    }

    /// <inheritdoc />
    public IAsyncEnumerator<StreamResponse> GetAsyncEnumerator(CancellationToken cancellationToken = default) => _channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        _channel.Writer.TryComplete();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        this.Dispose();
        return default;
    }
}