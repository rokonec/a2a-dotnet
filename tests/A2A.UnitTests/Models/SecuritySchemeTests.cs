using System.Text.Json;

namespace A2A.UnitTests.Models;

public class SecuritySchemeTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void SecurityScheme_DescriptionProperty_SerializesCorrectly()
    {
        // Arrange
        var scheme = new SecurityScheme { ApiKeySecurityScheme = new ApiKeySecurityScheme { Name = "X-API-Key", Location = "header", Description = "API key for authentication" } };

        // Act
        var json = JsonSerializer.Serialize(scheme, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SecurityScheme>(json, s_jsonOptions);

        // Assert
        Assert.Contains("\"apiKeySecurityScheme\"", json);
        Assert.Contains("\"description\": \"API key for authentication\"", json);
        Assert.NotNull(deserialized?.ApiKeySecurityScheme);
        Assert.Equal("API key for authentication", deserialized.ApiKeySecurityScheme.Description);
    }

    [Fact]
    public void SecurityScheme_DescriptionProperty_CanBeNull()
    {
        // Arrange
        var scheme = new SecurityScheme { HttpAuthSecurityScheme = new HttpAuthSecurityScheme { Scheme = "bearer" } };

        // Act
        var json = JsonSerializer.Serialize(scheme, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SecurityScheme>(json, s_jsonOptions);

        // Assert
        Assert.DoesNotContain("\"description\"", json);
        Assert.Contains("\"httpAuthSecurityScheme\"", json);
        Assert.NotNull(deserialized?.HttpAuthSecurityScheme);
        Assert.Null(deserialized.HttpAuthSecurityScheme.Description);
    }

    [Fact]
    public void ApiKeySecurityScheme_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var scheme = new SecurityScheme { ApiKeySecurityScheme = new ApiKeySecurityScheme { Name = "X-API-Key", Location = "header", Description = "API key for authentication" } };

        // Act
        var json = JsonSerializer.Serialize(scheme, s_jsonOptions);
        var d = JsonSerializer.Deserialize<SecurityScheme>(json, s_jsonOptions);

        // Assert
        Assert.Contains("\"apiKeySecurityScheme\"", json);
        Assert.Contains("\"description\":", json);

        Assert.NotNull(d?.ApiKeySecurityScheme);
        Assert.Equal("API key for authentication", d.ApiKeySecurityScheme.Description);
        Assert.Equal("X-API-Key", d.ApiKeySecurityScheme.Name);
        Assert.Equal("header", d.ApiKeySecurityScheme.Location);
    }

    [Fact]
    public void HttpAuthSecurityScheme_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var scheme = new SecurityScheme { HttpAuthSecurityScheme = new HttpAuthSecurityScheme { Scheme = "bearer" } };

        // Act
        var json = JsonSerializer.Serialize(scheme, s_jsonOptions);
        var d = JsonSerializer.Deserialize<SecurityScheme>(json, s_jsonOptions);

        // Assert
        Assert.Contains("\"httpAuthSecurityScheme\"", json);
        Assert.DoesNotContain("\"description\"", json);

        Assert.NotNull(d?.HttpAuthSecurityScheme);
        Assert.Equal("bearer", d.HttpAuthSecurityScheme.Scheme);
        Assert.Null(d.HttpAuthSecurityScheme.Description);
    }

    [Fact]
    public void OAuth2SecurityScheme_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var flows = new OAuthFlows
        {
            Password = new PasswordOAuthFlow { TokenUrl = "https://example.com/token", Scopes = new Dictionary<string, string>() { ["read"] = "Read access", ["write"] = "Write access" } },
        };
        var scheme = new SecurityScheme { OAuth2SecurityScheme = new OAuth2SecurityScheme { Flows = flows, Description = "OAuth2 authentication" } };

        // Act
        var json = JsonSerializer.Serialize(scheme, s_jsonOptions);
        var d = JsonSerializer.Deserialize<SecurityScheme>(json, s_jsonOptions);

        // Assert
        Assert.Contains("\"description\": \"OAuth2 authentication\"", json);

        Assert.NotNull(d?.OAuth2SecurityScheme);
        Assert.Contains("\"oauth2SecurityScheme\"", json);
        Assert.Equal("OAuth2 authentication", d.OAuth2SecurityScheme.Description);
        Assert.NotNull(d.OAuth2SecurityScheme.Flows);
        Assert.Null(d.OAuth2SecurityScheme.Flows.ClientCredentials);
        Assert.Null(d.OAuth2SecurityScheme.Flows.Implicit);
        Assert.Null(d.OAuth2SecurityScheme.Flows.AuthorizationCode);
        Assert.NotNull(d.OAuth2SecurityScheme.Flows.Password);
        Assert.Equal("https://example.com/token", d.OAuth2SecurityScheme.Flows.Password.TokenUrl);
        Assert.NotNull(d.OAuth2SecurityScheme.Flows.Password.Scopes);
        Assert.Equal(2, d.OAuth2SecurityScheme.Flows.Password.Scopes.Count);
        Assert.Contains("read", d.OAuth2SecurityScheme.Flows.Password.Scopes.Keys);
        Assert.Contains("write", d.OAuth2SecurityScheme.Flows.Password.Scopes.Keys);
        Assert.Equal("Read access", d.OAuth2SecurityScheme.Flows.Password.Scopes["read"]);
        Assert.Equal("Write access", d.OAuth2SecurityScheme.Flows.Password.Scopes["write"]);
    }

    [Fact]
    public void OAuth2SecurityScheme_DeserializesFromRawJsonCorrectly()
    {
        // Arrange
        var rawJson = """
        {
            "oauth2SecurityScheme": {
                "description": "OAuth2 authentication",
                "flows": {
                    "password": {
                        "tokenUrl": "https://example.com/token",
                        "scopes": {
                            "read": "Read access",
                            "write": "Write access"
                        }
                    }
                }
            }
        }
        """;

        // Act
        var d = JsonSerializer.Deserialize<SecurityScheme>(rawJson, s_jsonOptions);

        // Assert
        Assert.NotNull(d?.OAuth2SecurityScheme);
        Assert.Equal("OAuth2 authentication", d.OAuth2SecurityScheme.Description);
        Assert.NotNull(d.OAuth2SecurityScheme.Flows);
        Assert.Null(d.OAuth2SecurityScheme.Flows.ClientCredentials);
        Assert.Null(d.OAuth2SecurityScheme.Flows.Implicit);
        Assert.Null(d.OAuth2SecurityScheme.Flows.AuthorizationCode);
        Assert.NotNull(d.OAuth2SecurityScheme.Flows.Password);
        Assert.Equal("https://example.com/token", d.OAuth2SecurityScheme.Flows.Password.TokenUrl);
        Assert.NotNull(d.OAuth2SecurityScheme.Flows.Password.Scopes);
        Assert.Equal(2, d.OAuth2SecurityScheme.Flows.Password.Scopes.Count);
        Assert.Contains("read", d.OAuth2SecurityScheme.Flows.Password.Scopes.Keys);
        Assert.Contains("write", d.OAuth2SecurityScheme.Flows.Password.Scopes.Keys);
        Assert.Equal("Read access", d.OAuth2SecurityScheme.Flows.Password.Scopes["read"]);
        Assert.Equal("Write access", d.OAuth2SecurityScheme.Flows.Password.Scopes["write"]);
    }

    [Fact]
    public void OpenIdConnectSecurityScheme_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var scheme = new SecurityScheme { OpenIdConnectSecurityScheme = new OpenIdConnectSecurityScheme { OpenIdConnectUrl = "https://example.com/.well-known/openid_configuration", Description = "OpenID Connect authentication" } };

        // Act
        var json = JsonSerializer.Serialize(scheme, s_jsonOptions);
        var d = JsonSerializer.Deserialize<SecurityScheme>(json, s_jsonOptions);

        // Assert
        Assert.Contains("\"openIdConnectSecurityScheme\"", json);
        Assert.Contains("\"description\": \"OpenID Connect authentication\"", json);

        Assert.NotNull(d?.OpenIdConnectSecurityScheme);
        Assert.Equal("OpenID Connect authentication", d.OpenIdConnectSecurityScheme.Description);
        Assert.Equal("https://example.com/.well-known/openid_configuration", d.OpenIdConnectSecurityScheme.OpenIdConnectUrl);
    }

    [Fact]
    public void MutualTlsSecurityScheme_DeserializesFromBaseSecurityScheme()
    {
        // Arrange
        var scheme = new SecurityScheme { MtlsSecurityScheme = new MutualTlsSecurityScheme { Description = "Mutual TLS authentication" } };

        // Act
        var json = JsonSerializer.Serialize(scheme, s_jsonOptions);
        var d = JsonSerializer.Deserialize<SecurityScheme>(json, s_jsonOptions);

        // Assert
        Assert.NotNull(d?.MtlsSecurityScheme);
        Assert.Equal("Mutual TLS authentication", d.MtlsSecurityScheme.Description);
    }

    [Fact]
    public void OpenIdConnectSecurityScheme_DeserializesFromRawJsonCorrectly()
    {
        // Arrange
        var rawJson = """
        {
            "openIdConnectSecurityScheme": {
                "description": "OpenID Connect authentication",
                "openIdConnectUrl": "https://example.com/.well-known/openid_configuration"
            }
        }
        """;

        // Act
        var d = JsonSerializer.Deserialize<SecurityScheme>(rawJson, s_jsonOptions);

        // Assert
        Assert.NotNull(d?.OpenIdConnectSecurityScheme);
        Assert.Equal("OpenID Connect authentication", d.OpenIdConnectSecurityScheme.Description);
        Assert.Equal("https://example.com/.well-known/openid_configuration", d.OpenIdConnectSecurityScheme.OpenIdConnectUrl);
    }
}