using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Defines optional capabilities supported by an agent.
/// </summary>
public sealed class AgentCapabilities
{
    /// <summary>
    /// Indicates if the agent supports streaming responses.
    /// </summary>
    [JsonPropertyName("streaming")]
    public bool? Streaming { get; set; }

    /// <summary>
    /// Indicates if the agent supports sending push notifications for asynchronous task updates.
    /// </summary>
    [JsonPropertyName("pushNotifications")]
    public bool? PushNotifications { get; set; }

    /// <summary>
    /// A list of protocol extensions supported by the agent.
    /// </summary>
    [JsonPropertyName("extensions")]
    public List<AgentExtension>? Extensions { get; set; }

    /// <summary>
    /// Indicates if the agent supports providing an extended agent card when authenticated.
    /// </summary>
    [JsonPropertyName("extendedAgentCard")]
    public bool? ExtendedAgentCard { get; set; }
}
