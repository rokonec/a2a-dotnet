using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// A task delta where an artifact has been generated.
/// </summary>
public sealed class TaskArtifactUpdateEvent
{
    /// <summary>The id of the task for this artifact.</summary>
    [JsonPropertyName("taskId")]
    [JsonRequired]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>The id of the context that this task belongs to.</summary>
    [JsonPropertyName("contextId")]
    [JsonRequired]
    public string ContextId { get; set; } = string.Empty;

    /// <summary>The artifact that was generated or updated.</summary>
    [JsonPropertyName("artifact")]
    public Artifact Artifact { get; set; } = new Artifact();

    /// <summary>If true, the content of this artifact should be appended to a previously sent artifact with the same ID.</summary>
    [JsonPropertyName("append")]
    public bool? Append { get; set; }

    /// <summary>If true, this is the final chunk of the artifact.</summary>
    [JsonPropertyName("lastChunk")]
    public bool? LastChunk { get; set; }

    /// <summary>Optional metadata associated with the artifact update.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}