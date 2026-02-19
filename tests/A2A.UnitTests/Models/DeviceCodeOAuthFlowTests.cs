using System.Text.Json;

namespace A2A.UnitTests.Models;

public class DeviceCodeOAuthFlowTests
{
    [Fact]
    public void DeviceCodeOAuthFlow_Serializes()
    {
        var flow = new DeviceCodeOAuthFlow
        {
            DeviceAuthorizationUrl = "https://auth.example.com/device",
            TokenUrl = "https://auth.example.com/token",
            RefreshUrl = "https://auth.example.com/refresh",
            Scopes = new Dictionary<string, string> { ["read"] = "Read access" }
        };

        var json = JsonSerializer.Serialize(flow, A2AJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<DeviceCodeOAuthFlow>(json, A2AJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("https://auth.example.com/device", deserialized.DeviceAuthorizationUrl);
        Assert.Equal("https://auth.example.com/token", deserialized.TokenUrl);
        Assert.Equal("https://auth.example.com/refresh", deserialized.RefreshUrl);
        Assert.Single(deserialized.Scopes);
    }

    [Fact]
    public void OAuthFlows_DeviceCode_InSecurityScheme()
    {
        var scheme = new SecurityScheme
        {
            OAuth2SecurityScheme = new OAuth2SecurityScheme
            {
                Flows = new OAuthFlows
                {
                    DeviceCode = new DeviceCodeOAuthFlow
                    {
                        DeviceAuthorizationUrl = "https://auth.example.com/device",
                        TokenUrl = "https://auth.example.com/token",
                        Scopes = new Dictionary<string, string> { ["openid"] = "OpenID" }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(scheme, A2AJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<SecurityScheme>(json, A2AJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized?.OAuth2SecurityScheme?.Flows?.DeviceCode);
        Assert.Equal("https://auth.example.com/device",
            deserialized.OAuth2SecurityScheme.Flows.DeviceCode.DeviceAuthorizationUrl);
    }

    [Fact]
    public void AuthorizationCodeOAuthFlow_PkceRequired()
    {
        var flow = new AuthorizationCodeOAuthFlow
        {
            AuthorizationUrl = "https://auth.example.com/authorize",
            TokenUrl = "https://auth.example.com/token",
            Scopes = new Dictionary<string, string> { ["read"] = "Read" },
            PkceRequired = true
        };

        var json = JsonSerializer.Serialize(flow, A2AJsonUtilities.DefaultOptions);
        Assert.Contains("\"pkceRequired\":true", json);

        var deserialized = JsonSerializer.Deserialize<AuthorizationCodeOAuthFlow>(json, A2AJsonUtilities.DefaultOptions);
        Assert.True(deserialized?.PkceRequired);
    }
}
