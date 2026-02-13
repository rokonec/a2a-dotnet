using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// A self-describing manifest for an agent. It provides essential
/// metadata including the agent's identity, capabilities, skills, supported
/// communication methods, and security requirements.
/// </summary>
public sealed class AgentCard
{
    /// <summary>
    /// A human readable name for the agent.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the agent.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonRequired]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Ordered list of supported interfaces. First entry is preferred.
    /// </summary>
    [JsonPropertyName("supportedInterfaces")]
    [JsonRequired]
    public List<AgentInterface> SupportedInterfaces { get; set; } = [];

    /// <summary>
    /// The service provider of the agent.
    /// </summary>
    [JsonPropertyName("provider")]
    public AgentProvider? Provider { get; set; }

    /// <summary>
    /// The version of the agent.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonRequired]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// A URL to documentation for the agent.
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// A2A Capability set supported by the agent.
    /// </summary>
    [JsonPropertyName("capabilities")]
    [JsonRequired]
    public AgentCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// The security scheme details used for authenticating with this agent.
    /// </summary>
    [JsonPropertyName("securitySchemes")]
    public Dictionary<string, SecurityScheme>? SecuritySchemes { get; set; }

    /// <summary>
    /// Security requirements for contacting the agent.
    /// </summary>
    [JsonPropertyName("securityRequirements")]
    public List<Dictionary<string, List<string>>>? SecurityRequirements { get; set; }

    /// <summary>
    /// The set of interaction modes that the agent supports across all skills.
    /// </summary>
    [JsonPropertyName("defaultInputModes")]
    [JsonRequired]
    public List<string> DefaultInputModes { get; set; } = ["text/plain"];

    /// <summary>
    /// The media types supported as outputs from this agent.
    /// </summary>
    [JsonPropertyName("defaultOutputModes")]
    [JsonRequired]
    public List<string> DefaultOutputModes { get; set; } = ["text/plain"];

    /// <summary>
    /// Skills represent an ability of an agent.
    /// </summary>
    [JsonPropertyName("skills")]
    [JsonRequired]
    public List<AgentSkill> Skills { get; set; } = [];

    /// <summary>
    /// JSON Web Signatures computed for this AgentCard.
    /// </summary>
    [JsonPropertyName("signatures")]
    public List<AgentCardSignature>? Signatures { get; set; }

    /// <summary>
    /// An optional URL to an icon for the agent.
    /// </summary>
    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }
}
