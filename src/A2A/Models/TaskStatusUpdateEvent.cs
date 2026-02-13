using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Event sent by server during streaming or subscribe requests.
/// </summary>
public sealed class TaskStatusUpdateEvent() : TaskUpdateEvent(A2AEventKind.StatusUpdate)
{
    /// <summary>
    /// Gets or sets the current status of the task.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonRequired]
    public AgentTaskStatus Status { get; set; } = new();
}