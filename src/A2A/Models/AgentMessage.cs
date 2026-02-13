using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Defines the sender of a message in A2A protocol communication.
/// </summary>
[JsonConverter(typeof(ProtoJsonEnumConverter<MessageRole>))]
public enum MessageRole
{
    /// <summary>
    /// Unspecified role.
    /// </summary>
    [EnumMember(Value = "ROLE_UNSPECIFIED")]
    Unspecified,

    /// <summary>
    /// USER role refers to communication from the client to the server.
    /// </summary>
    [EnumMember(Value = "ROLE_USER")]
    User,

    /// <summary>
    /// AGENT role refers to communication from the server to the client.
    /// </summary>
    [EnumMember(Value = "ROLE_AGENT")]
    Agent
}

/// <summary>
/// Represents a single message exchanged between user and agent.
/// </summary>
public sealed class AgentMessage() : A2AResponse(A2AEventKind.Message)
{
    /// <summary>
    /// Message sender's role.
    /// </summary>
    [JsonPropertyName("role")]
    [JsonRequired]
    public MessageRole Role { get; set; } = MessageRole.User;

    /// <summary>
    /// Message content.
    /// </summary>
    [JsonPropertyName("parts")]
    [JsonRequired]
    public List<Part> Parts { get; set; } = [];

    /// <summary>
    /// Extension metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    /// <summary>
    /// List of tasks referenced as context by this message.
    /// </summary>
    [JsonPropertyName("referenceTaskIds")]
    public List<string>? ReferenceTaskIds { get; set; }

    /// <summary>
    /// Identifier created by the message creator.
    /// </summary>
    [JsonPropertyName("messageId")]
    [JsonRequired]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of task the message is related to.
    /// </summary>
    [JsonPropertyName("taskId")]
    public string? TaskId { get; set; }

    /// <summary>
    /// The context the message is associated with.
    /// </summary>
    [JsonPropertyName("contextId")]
    public string? ContextId { get; set; }

    /// <summary>
    /// The URIs of extensions that are present or contributed to this Message.
    /// </summary>
    [JsonPropertyName("extensions")]
    public List<string>? Extensions { get; set; }
}