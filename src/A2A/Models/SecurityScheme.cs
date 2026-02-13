using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Defines a security scheme that can be used to secure an agent's endpoints.
/// This is a discriminated union type based on the OpenAPI 3.2 Security Scheme Object.
/// Exactly one of the scheme properties must be set.
/// </summary>
[JsonConverter(typeof(SecuritySchemeConverter))]
public sealed class SecurityScheme
{
    /// <summary>
    /// API key-based authentication.
    /// </summary>
    [JsonPropertyName("apiKeySecurityScheme")]
    public ApiKeySecurityScheme? ApiKeySecurityScheme { get; set; }

    /// <summary>
    /// HTTP authentication (Basic, Bearer, etc.).
    /// </summary>
    [JsonPropertyName("httpAuthSecurityScheme")]
    public HttpAuthSecurityScheme? HttpAuthSecurityScheme { get; set; }

    /// <summary>
    /// OAuth 2.0 authentication.
    /// </summary>
    [JsonPropertyName("oauth2SecurityScheme")]
    public OAuth2SecurityScheme? OAuth2SecurityScheme { get; set; }

    /// <summary>
    /// OpenID Connect authentication.
    /// </summary>
    [JsonPropertyName("openIdConnectSecurityScheme")]
    public OpenIdConnectSecurityScheme? OpenIdConnectSecurityScheme { get; set; }

    /// <summary>
    /// Mutual TLS authentication.
    /// </summary>
    [JsonPropertyName("mtlsSecurityScheme")]
    public MutualTlsSecurityScheme? MtlsSecurityScheme { get; set; }
}

/// <summary>
/// API Key security scheme.
/// </summary>
public sealed class ApiKeySecurityScheme
{
    /// <summary>An optional description for the security scheme.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The location of the API key. Valid values are "query", "header", or "cookie".</summary>
    [JsonPropertyName("location")]
    [JsonRequired]
    public string Location { get; set; } = string.Empty;

    /// <summary>The name of the header, query, or cookie parameter to be used.</summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// HTTP Authentication security scheme.
/// </summary>
public sealed class HttpAuthSecurityScheme
{
    /// <summary>An optional description for the security scheme.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The name of the HTTP Authentication scheme (e.g., "Bearer").</summary>
    [JsonPropertyName("scheme")]
    [JsonRequired]
    public string Scheme { get; set; } = string.Empty;

    /// <summary>A hint to the client to identify how the bearer token is formatted (e.g., "JWT").</summary>
    [JsonPropertyName("bearerFormat")]
    public string? BearerFormat { get; set; }
}

/// <summary>
/// OAuth2.0 security scheme configuration.
/// </summary>
public sealed class OAuth2SecurityScheme
{
    /// <summary>An optional description for the security scheme.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>An object containing configuration information for the supported OAuth 2.0 flows.</summary>
    [JsonPropertyName("flows")]
    [JsonRequired]
    public OAuthFlows Flows { get; set; } = new();

    /// <summary>URL to the OAuth2 authorization server metadata (RFC8414).</summary>
    [JsonPropertyName("oauth2MetadataUrl")]
    public string? OAuth2MetadataUrl { get; set; }
}

/// <summary>
/// OpenID Connect security scheme configuration.
/// </summary>
public sealed class OpenIdConnectSecurityScheme
{
    /// <summary>An optional description for the security scheme.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The OpenID Connect Discovery URL for the OIDC provider's metadata.</summary>
    [JsonPropertyName("openIdConnectUrl")]
    [JsonRequired]
    public string OpenIdConnectUrl { get; set; } = string.Empty;
}

/// <summary>
/// Mutual TLS security scheme configuration.
/// </summary>
public sealed class MutualTlsSecurityScheme
{
    /// <summary>An optional description for the security scheme.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Defines the configuration for the supported OAuth 2.0 flows.
/// Exactly one flow must be set (oneof).
/// </summary>
public sealed class OAuthFlows
{
    /// <summary>Configuration for the OAuth Authorization Code flow.</summary>
    [JsonPropertyName("authorizationCode")]
    public AuthorizationCodeOAuthFlow? AuthorizationCode { get; set; }

    /// <summary>Configuration for the OAuth Client Credentials flow.</summary>
    [JsonPropertyName("clientCredentials")]
    public ClientCredentialsOAuthFlow? ClientCredentials { get; set; }

    /// <summary>Configuration for the OAuth Implicit flow (deprecated).</summary>
    [JsonPropertyName("implicit")]
    public ImplicitOAuthFlow? Implicit { get; set; }

    /// <summary>Configuration for the OAuth Resource Owner Password flow (deprecated).</summary>
    [JsonPropertyName("password")]
    public PasswordOAuthFlow? Password { get; set; }

    /// <summary>Configuration for the OAuth Device Code flow (RFC 8628).</summary>
    [JsonPropertyName("deviceCode")]
    public DeviceCodeOAuthFlow? DeviceCode { get; set; }
}

/// <summary>
/// Configuration details for the OAuth 2.0 Authorization Code flow.
/// </summary>
public sealed class AuthorizationCodeOAuthFlow
{
    /// <summary>The authorization URL to be used for this flow.</summary>
    [JsonPropertyName("authorizationUrl")]
    [JsonRequired]
    public string AuthorizationUrl { get; set; } = string.Empty;

    /// <summary>The token URL to be used for this flow.</summary>
    [JsonPropertyName("tokenUrl")]
    [JsonRequired]
    public string TokenUrl { get; set; } = string.Empty;

    /// <summary>The URL to be used for obtaining refresh tokens.</summary>
    [JsonPropertyName("refreshUrl")]
    public string? RefreshUrl { get; set; }

    /// <summary>The available scopes for the OAuth2 security scheme.</summary>
    [JsonPropertyName("scopes")]
    [JsonRequired]
    public Dictionary<string, string> Scopes { get; set; } = [];

    /// <summary>Indicates if PKCE (RFC 7636) is required for this flow.</summary>
    [JsonPropertyName("pkceRequired")]
    public bool? PkceRequired { get; set; }
}

/// <summary>
/// Configuration details for the OAuth 2.0 Client Credentials flow.
/// </summary>
public sealed class ClientCredentialsOAuthFlow
{
    /// <summary>The token URL to be used for this flow.</summary>
    [JsonPropertyName("tokenUrl")]
    [JsonRequired]
    public string TokenUrl { get; set; } = string.Empty;

    /// <summary>The URL to be used for obtaining refresh tokens.</summary>
    [JsonPropertyName("refreshUrl")]
    public string? RefreshUrl { get; set; }

    /// <summary>The available scopes for the OAuth2 security scheme.</summary>
    [JsonPropertyName("scopes")]
    [JsonRequired]
    public Dictionary<string, string> Scopes { get; set; } = [];
}

/// <summary>
/// Configuration details for the OAuth 2.0 Implicit flow (deprecated).
/// </summary>
public sealed class ImplicitOAuthFlow
{
    /// <summary>The authorization URL to be used for this flow.</summary>
    [JsonPropertyName("authorizationUrl")]
    [JsonRequired]
    public string AuthorizationUrl { get; set; } = string.Empty;

    /// <summary>The URL to be used for obtaining refresh tokens.</summary>
    [JsonPropertyName("refreshUrl")]
    public string? RefreshUrl { get; set; }

    /// <summary>The available scopes for the OAuth2 security scheme.</summary>
    [JsonPropertyName("scopes")]
    public Dictionary<string, string> Scopes { get; set; } = [];
}

/// <summary>
/// Configuration details for the OAuth 2.0 Resource Owner Password flow (deprecated).
/// </summary>
public sealed class PasswordOAuthFlow
{
    /// <summary>The token URL to be used for this flow.</summary>
    [JsonPropertyName("tokenUrl")]
    [JsonRequired]
    public string TokenUrl { get; set; } = string.Empty;

    /// <summary>The URL to be used for obtaining refresh tokens.</summary>
    [JsonPropertyName("refreshUrl")]
    public string? RefreshUrl { get; set; }

    /// <summary>The available scopes for the OAuth2 security scheme.</summary>
    [JsonPropertyName("scopes")]
    public Dictionary<string, string> Scopes { get; set; } = [];
}

/// <summary>
/// Configuration details for the OAuth 2.0 Device Code flow (RFC 8628).
/// </summary>
public sealed class DeviceCodeOAuthFlow
{
    /// <summary>The device authorization endpoint URL.</summary>
    [JsonPropertyName("deviceAuthorizationUrl")]
    [JsonRequired]
    public string DeviceAuthorizationUrl { get; set; } = string.Empty;

    /// <summary>The token URL to be used for this flow.</summary>
    [JsonPropertyName("tokenUrl")]
    [JsonRequired]
    public string TokenUrl { get; set; } = string.Empty;

    /// <summary>The URL to be used for obtaining refresh tokens.</summary>
    [JsonPropertyName("refreshUrl")]
    public string? RefreshUrl { get; set; }

    /// <summary>The available scopes for the OAuth2 security scheme.</summary>
    [JsonPropertyName("scopes")]
    [JsonRequired]
    public Dictionary<string, string> Scopes { get; set; } = [];
}

/// <summary>
/// Defines authentication details, used for push notifications.
/// </summary>
public sealed class AuthenticationInfo
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

internal sealed class SecuritySchemeConverter : JsonConverter<SecurityScheme>
{
    public override SecurityScheme Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new A2AException("Expected JSON object for SecurityScheme", A2AErrorCode.InvalidRequest);
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var scheme = new SecurityScheme();
        var raw = root.GetRawText();

        if (root.TryGetProperty("apiKeySecurityScheme", out _))
        {
            scheme.ApiKeySecurityScheme = JsonSerializer.Deserialize(
                root.GetProperty("apiKeySecurityScheme").GetRawText(),
                options.GetTypeInfo(typeof(ApiKeySecurityScheme))) as ApiKeySecurityScheme;
        }
        else if (root.TryGetProperty("httpAuthSecurityScheme", out _))
        {
            scheme.HttpAuthSecurityScheme = JsonSerializer.Deserialize(
                root.GetProperty("httpAuthSecurityScheme").GetRawText(),
                options.GetTypeInfo(typeof(HttpAuthSecurityScheme))) as HttpAuthSecurityScheme;
        }
        else if (root.TryGetProperty("oauth2SecurityScheme", out _))
        {
            scheme.OAuth2SecurityScheme = JsonSerializer.Deserialize(
                root.GetProperty("oauth2SecurityScheme").GetRawText(),
                options.GetTypeInfo(typeof(OAuth2SecurityScheme))) as OAuth2SecurityScheme;
        }
        else if (root.TryGetProperty("openIdConnectSecurityScheme", out _))
        {
            scheme.OpenIdConnectSecurityScheme = JsonSerializer.Deserialize(
                root.GetProperty("openIdConnectSecurityScheme").GetRawText(),
                options.GetTypeInfo(typeof(OpenIdConnectSecurityScheme))) as OpenIdConnectSecurityScheme;
        }
        else if (root.TryGetProperty("mtlsSecurityScheme", out _))
        {
            scheme.MtlsSecurityScheme = JsonSerializer.Deserialize(
                root.GetProperty("mtlsSecurityScheme").GetRawText(),
                options.GetTypeInfo(typeof(MutualTlsSecurityScheme))) as MutualTlsSecurityScheme;
        }

        return scheme;
    }

    public override void Write(Utf8JsonWriter writer, SecurityScheme value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.ApiKeySecurityScheme is { } apiKey)
        {
            writer.WritePropertyName("apiKeySecurityScheme");
            JsonSerializer.Serialize(writer, apiKey, options.GetTypeInfo(typeof(ApiKeySecurityScheme)));
        }
        else if (value.HttpAuthSecurityScheme is { } httpAuth)
        {
            writer.WritePropertyName("httpAuthSecurityScheme");
            JsonSerializer.Serialize(writer, httpAuth, options.GetTypeInfo(typeof(HttpAuthSecurityScheme)));
        }
        else if (value.OAuth2SecurityScheme is { } oauth2)
        {
            writer.WritePropertyName("oauth2SecurityScheme");
            JsonSerializer.Serialize(writer, oauth2, options.GetTypeInfo(typeof(OAuth2SecurityScheme)));
        }
        else if (value.OpenIdConnectSecurityScheme is { } oidc)
        {
            writer.WritePropertyName("openIdConnectSecurityScheme");
            JsonSerializer.Serialize(writer, oidc, options.GetTypeInfo(typeof(OpenIdConnectSecurityScheme)));
        }
        else if (value.MtlsSecurityScheme is { } mtls)
        {
            writer.WritePropertyName("mtlsSecurityScheme");
            JsonSerializer.Serialize(writer, mtls, options.GetTypeInfo(typeof(MutualTlsSecurityScheme)));
        }

        writer.WriteEndObject();
    }
}