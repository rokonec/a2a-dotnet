using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Defines authentication details, used for push notifications.
/// </summary>
public sealed class PushNotificationAuthenticationInfo
{
    /// <summary>
    /// HTTP Authentication Scheme from the IANA registry.
    /// Common values: "Bearer", "Basic", "Digest".
    /// </summary>
    [JsonPropertyName("scheme")]
    [JsonRequired]
    public string Scheme { get; set; } = string.Empty;

    /// <summary>
    /// Push Notification credentials. Format depends on the scheme.
    /// </summary>
    [JsonPropertyName("credentials")]
    public string? Credentials { get; set; }
}