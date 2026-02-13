using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// An event sent by the agent to notify the client of a change in a task's status.
/// </summary>
public sealed class TaskStatusUpdateEvent
{
    /// <summary>The id of the task that is changed.</summary>
    [JsonPropertyName("taskId")]
    [JsonRequired]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>The id of the context that the task belongs to.</summary>
    [JsonPropertyName("contextId")]
    [JsonRequired]
    public string ContextId { get; set; } = string.Empty;

    /// <summary>The new status of the task.</summary>
    [JsonPropertyName("status")]
    [JsonRequired]
    public AgentTaskStatus Status { get; set; } = new();

    /// <summary>Optional metadata to associate with the task update.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}