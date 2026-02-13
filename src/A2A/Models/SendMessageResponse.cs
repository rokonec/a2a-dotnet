using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Identifies which payload field is set in a <see cref="SendMessageResponse"/>.
/// </summary>
public enum SendMessageResponseCase
{
    /// <summary>No payload field is set.</summary>
    None,
    /// <summary>The <see cref="SendMessageResponse.Task"/> field is set.</summary>
    Task,
    /// <summary>The <see cref="SendMessageResponse.Message"/> field is set.</summary>
    Message
}

/// <summary>
/// Response from a SendMessage operation. Contains exactly one of Task or Message.
/// </summary>
public sealed class SendMessageResponse
{
    /// <summary>A Task object representing the processing of the message.</summary>
    [JsonPropertyName("task")]
    public AgentTask? Task { get; set; }

    /// <summary>A direct response message (for simple interactions).</summary>
    [JsonPropertyName("message")]
    public AgentMessage? Message { get; set; }

    /// <summary>Identifies which payload field is set.</summary>
    [JsonIgnore]
    public SendMessageResponseCase PayloadCase =>
        Task is not null ? SendMessageResponseCase.Task :
        Message is not null ? SendMessageResponseCase.Message :
        SendMessageResponseCase.None;
}

/// <summary>
/// Identifies which payload field is set in a <see cref="StreamResponse"/>.
/// </summary>
public enum StreamResponseCase
{
    /// <summary>No payload field is set.</summary>
    None,
    /// <summary>The <see cref="StreamResponse.Task"/> field is set.</summary>
    Task,
    /// <summary>The <see cref="StreamResponse.Message"/> field is set.</summary>
    Message,
    /// <summary>The <see cref="StreamResponse.StatusUpdate"/> field is set.</summary>
    StatusUpdate,
    /// <summary>The <see cref="StreamResponse.ArtifactUpdate"/> field is set.</summary>
    ArtifactUpdate
}

/// <summary>
/// A wrapper object used in streaming operations to encapsulate different types of response data.
/// Contains exactly one of Task, Message, StatusUpdate, or ArtifactUpdate.
/// </summary>
public sealed class StreamResponse
{
    /// <summary>A Task object containing the current state of the task.</summary>
    [JsonPropertyName("task")]
    public AgentTask? Task { get; set; }

    /// <summary>A Message object containing a message from the agent.</summary>
    [JsonPropertyName("message")]
    public AgentMessage? Message { get; set; }

    /// <summary>An event indicating a task status update.</summary>
    [JsonPropertyName("statusUpdate")]
    public TaskStatusUpdateEvent? StatusUpdate { get; set; }

    /// <summary>An event indicating a task artifact update.</summary>
    [JsonPropertyName("artifactUpdate")]
    public TaskArtifactUpdateEvent? ArtifactUpdate { get; set; }

    /// <summary>Identifies which payload field is set.</summary>
    [JsonIgnore]
    public StreamResponseCase PayloadCase =>
        Task is not null ? StreamResponseCase.Task :
        Message is not null ? StreamResponseCase.Message :
        StatusUpdate is not null ? StreamResponseCase.StatusUpdate :
        ArtifactUpdate is not null ? StreamResponseCase.ArtifactUpdate :
        StreamResponseCase.None;
}
