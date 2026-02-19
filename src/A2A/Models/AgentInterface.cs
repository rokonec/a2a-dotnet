using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Declares a combination of a target URL, transport and protocol version for interacting with the agent.
/// </summary>
public sealed class AgentInterface
{
    /// <summary>
    /// The URL where this interface is available.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonRequired]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The protocol binding supported at this URL.
    /// Core values: "JSONRPC", "GRPC", "HTTP+JSON".
    /// </summary>
    [JsonPropertyName("protocolBinding")]
    [JsonRequired]
    public string ProtocolBinding { get; set; } = "JSONRPC";

    /// <summary>
    /// Tenant to be set in the request when calling the agent.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// The version of the A2A protocol this interface exposes.
    /// Examples: "0.3", "1.0"
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    [JsonRequired]
    public string ProtocolVersion { get; set; } = "1.0";
}