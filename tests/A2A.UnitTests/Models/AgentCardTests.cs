using System.Text.Json;

namespace A2A.UnitTests.Models;

public class AgentCardTests
{
    private const string ExpectedJson = """
        {
          "name": "Test Agent",
          "description": "A test agent for MVP serialization",
          "supportedInterfaces": [
            {
              "url": "https://example.com/agent",
              "protocolBinding": "JSONRPC",
              "protocolVersion": "1.0"
            }
          ],
          "provider": {
            "organization": "Test Org",
            "url": "https://testorg.com"
          },
          "version": "1.0.0",
          "documentationUrl": "https://docs.example.com",
          "capabilities": {
            "streaming": true,
            "pushNotifications": false
          },
          "securitySchemes": {
            "apiKey": {
              "apiKeySecurityScheme": {
                "name": "X-API-Key",
                "location": "header"
              }
            }
          },
          "securityRequirements": [
            {
              "apiKey": []
            }
          ],
          "defaultInputModes": ["text", "image"],
          "defaultOutputModes": ["text", "json"],
          "skills": [
            {
              "id": "test-skill",
              "name": "Test Skill",
              "description": "A test skill",
              "tags": ["test", "skill"],
              "examples": ["Example usage"],
              "inputModes": ["text"],
              "outputModes": ["text"],
              "security": [
                {
                  "oauth": ["read"]
                }
              ]
            }
          ],
          "signatures": [
            {
              "protected": "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpPU0UiLCJraWQiOiJrZXktMSIsImprdSI6Imh0dHBzOi8vZXhhbXBsZS5jb20vYWdlbnQvandrcy5qc29uIn0",
              "signature": "QFdkNLNszlGj3z3u0YQGt_T9LixY3qtdQpZmsTdDHDe3fXV9y9-B3m2-XgCpzuhiLt8E0tV6HXoZKHv4GtHgKQ"
            }
          ]
        }
        """;

    [Fact]
    public void AgentCard_Deserialize_AllPropertiesCorrect()
    {
        // Act
        var deserializedCard = JsonSerializer.Deserialize<AgentCard>(ExpectedJson, A2AJsonUtilities.DefaultOptions);

        // Assert
        Assert.NotNull(deserializedCard);
        Assert.Equal("Test Agent", deserializedCard.Name);
        Assert.Equal("A test agent for MVP serialization", deserializedCard.Description);
        Assert.Equal("https://example.com/agent", deserializedCard.SupportedInterfaces[0].Url);
        Assert.Equal("1.0.0", deserializedCard.Version);
        Assert.Equal("1.0", deserializedCard.SupportedInterfaces[0].ProtocolVersion);
        Assert.Equal("https://docs.example.com", deserializedCard.DocumentationUrl);

        // Provider
        Assert.NotNull(deserializedCard.Provider);
        Assert.Equal("Test Org", deserializedCard.Provider.Organization);
        Assert.Equal("https://testorg.com", deserializedCard.Provider.Url);

        // Capabilities
        Assert.NotNull(deserializedCard.Capabilities);
        Assert.True(deserializedCard.Capabilities.Streaming);
        Assert.False(deserializedCard.Capabilities.PushNotifications);

        // Security
        Assert.NotNull(deserializedCard.SecuritySchemes);
        Assert.Single(deserializedCard.SecuritySchemes);
        Assert.Contains("apiKey", deserializedCard.SecuritySchemes.Keys);
        var apisec = deserializedCard.SecuritySchemes["apiKey"].ApiKeySecurityScheme;
        Assert.NotNull(apisec);
        Assert.False(string.IsNullOrWhiteSpace(apisec.Name));
        Assert.False(string.IsNullOrWhiteSpace(apisec.Location));

        Assert.NotNull(deserializedCard.SecurityRequirements);
        Assert.Single(deserializedCard.SecurityRequirements);

        var securityRequirement = deserializedCard.SecurityRequirements[0];
        Assert.NotNull(securityRequirement);
        Assert.Single(securityRequirement);
        Assert.True(securityRequirement.ContainsKey("apiKey"));
        Assert.Empty(securityRequirement["apiKey"]);

        // Input/Output modes
        Assert.Equal(new List<string> { "text", "image" }, deserializedCard.DefaultInputModes);
        Assert.Equal(new List<string> { "text", "json" }, deserializedCard.DefaultOutputModes);

        // Skills
        Assert.NotNull(deserializedCard.Skills);
        Assert.Single(deserializedCard.Skills);
        var skill = deserializedCard.Skills[0];
        Assert.Equal("test-skill", skill.Id);
        Assert.Equal("Test Skill", skill.Name);
        Assert.Equal("A test skill", skill.Description);
        Assert.NotNull(skill.Tags);
        Assert.Equal(2, skill.Tags.Count);
        Assert.Contains("test", skill.Tags);
        Assert.Contains("skill", skill.Tags);

        // Skill Security
        Assert.NotNull(skill.Security);
        Assert.Single(skill.Security);
        var skillSecurityRequirement = skill.Security[0];
        Assert.NotNull(skillSecurityRequirement);
        Assert.Single(skillSecurityRequirement);
        Assert.True(skillSecurityRequirement.ContainsKey("oauth"));
        Assert.Equal(["read"], skillSecurityRequirement["oauth"]);

        // SupportedInterfaces
        Assert.NotNull(deserializedCard.SupportedInterfaces);
        Assert.Single(deserializedCard.SupportedInterfaces);
        Assert.Equal("JSONRPC", deserializedCard.SupportedInterfaces[0].ProtocolBinding);
        Assert.Equal("https://example.com/agent", deserializedCard.SupportedInterfaces[0].Url);

        // Signatures
        Assert.NotNull(deserializedCard.Signatures);
        Assert.Single(deserializedCard.Signatures);
        var signature = deserializedCard.Signatures[0];
        Assert.Equal("eyJhbGciOiJFUzI1NiIsInR5cCI6IkpPU0UiLCJraWQiOiJrZXktMSIsImprdSI6Imh0dHBzOi8vZXhhbXBsZS5jb20vYWdlbnQvandrcy5qc29uIn0", signature.Protected);
        Assert.Equal("QFdkNLNszlGj3z3u0YQGt_T9LixY3qtdQpZmsTdDHDe3fXV9y9-B3m2-XgCpzuhiLt8E0tV6HXoZKHv4GtHgKQ", signature.Signature);
        Assert.Null(signature.Header);
    }

    [Fact]
    public void AgentCard_Serialize_ProducesExpectedJson()
    {
        // Arrange
        var agentCard = new AgentCard
        {
            Name = "Test Agent",
            Description = "A test agent for MVP serialization",
            SupportedInterfaces = [
                new AgentInterface
                {
                    Url = "https://example.com/agent",
                    ProtocolBinding = "JSONRPC",
                    ProtocolVersion = "1.0"
                }
            ],
            Provider = new AgentProvider
            {
                Organization = "Test Org",
                Url = "https://testorg.com"
            },
            Version = "1.0.0",
            DocumentationUrl = "https://docs.example.com",
            Capabilities = new AgentCapabilities
            {
                Streaming = true,
                PushNotifications = false
            },
            SecuritySchemes = new Dictionary<string, SecurityScheme>
            {
                ["apiKey"] = new SecurityScheme { ApiKeySecurityScheme = new ApiKeySecurityScheme { Name = "X-API-Key", Location = "header" } }
            },
            SecurityRequirements = new List<Dictionary<string, List<string>>>
            {
                new()
                {
                    ["apiKey"] = []
                }
            },
            DefaultInputModes = ["text", "image"],
            DefaultOutputModes = ["text", "json"],
            Skills = [
                new AgentSkill
                {
                    Id = "test-skill",
                    Name = "Test Skill",
                    Description = "A test skill",
                    Tags = ["test", "skill"],
                    Examples = ["Example usage"],
                    InputModes = ["text"],
                    OutputModes = ["text"],
                    Security = [
                        new Dictionary<string, string[]>
                        {
                            ["oauth"] = ["read"]
                        }
                    ]
                }
            ],
            Signatures = [
                new AgentCardSignature
                {
                    Protected = "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpPU0UiLCJraWQiOiJrZXktMSIsImprdSI6Imh0dHBzOi8vZXhhbXBsZS5jb20vYWdlbnQvandrcy5qc29uIn0",
                    Signature = "QFdkNLNszlGj3z3u0YQGt_T9LixY3qtdQpZmsTdDHDe3fXV9y9-B3m2-XgCpzuhiLt8E0tV6HXoZKHv4GtHgKQ"
                }
            ]
        };

        // Act
        var serializedJson = JsonSerializer.Serialize(agentCard, A2AJsonUtilities.DefaultOptions);

        // Assert - Compare objects instead of raw JSON strings to avoid formatting/ordering issues
        // and provide more meaningful error messages when properties don't match
        var expectedCard = JsonSerializer.Deserialize<AgentCard>(ExpectedJson, A2AJsonUtilities.DefaultOptions);
        var actualCard = JsonSerializer.Deserialize<AgentCard>(serializedJson, A2AJsonUtilities.DefaultOptions);

        Assert.NotNull(actualCard);
        Assert.NotNull(expectedCard);

        // Compare key properties
        Assert.Equal(expectedCard.Name, actualCard.Name);
        Assert.Equal(expectedCard.Description, actualCard.Description);
        Assert.Equal(expectedCard.SupportedInterfaces[0].Url, actualCard.SupportedInterfaces[0].Url);
        Assert.Equal(expectedCard.Version, actualCard.Version);
        Assert.Equal(expectedCard.SupportedInterfaces[0].ProtocolVersion, actualCard.SupportedInterfaces[0].ProtocolVersion);
        Assert.Equal(expectedCard.DocumentationUrl, actualCard.DocumentationUrl);

        // Provider
        Assert.Equal(expectedCard.Provider?.Organization, actualCard.Provider?.Organization);
        Assert.Equal(expectedCard.Provider?.Url, actualCard.Provider?.Url);

        // Capabilities
        Assert.Equal(expectedCard.Capabilities?.Streaming, actualCard.Capabilities?.Streaming);
        Assert.Equal(expectedCard.Capabilities?.PushNotifications, actualCard.Capabilities?.PushNotifications);

        // Input/Output modes
        Assert.Equal(expectedCard.DefaultInputModes, actualCard.DefaultInputModes);
        Assert.Equal(expectedCard.DefaultOutputModes, actualCard.DefaultOutputModes);

        // Skills
        Assert.Equal(expectedCard.Skills?.Count, actualCard.Skills?.Count);
        if (expectedCard.Skills?.Count > 0 && actualCard.Skills?.Count > 0)
        {
            var expectedSkill = expectedCard.Skills[0];
            var actualSkill = actualCard.Skills[0];
            Assert.Equal(expectedSkill.Id, actualSkill.Id);
            Assert.Equal(expectedSkill.Name, actualSkill.Name);
            Assert.Equal(expectedSkill.Description, actualSkill.Description);
            Assert.Equal(expectedSkill.Tags, actualSkill.Tags);
            Assert.Equal(expectedSkill.Examples, actualSkill.Examples);
            Assert.Equal(expectedSkill.InputModes, actualSkill.InputModes);
            Assert.Equal(expectedSkill.OutputModes, actualSkill.OutputModes);

            // Skill Security
            Assert.Equal(expectedSkill.Security?.Count, actualSkill.Security?.Count);
            if (expectedSkill.Security?.Count > 0 && actualSkill.Security?.Count > 0)
            {
                Assert.Equal(expectedSkill.Security[0].Count, actualSkill.Security[0].Count);
                foreach (var kvp in expectedSkill.Security[0])
                {
                    Assert.True(actualSkill.Security[0].ContainsKey(kvp.Key));
                    Assert.Equal(kvp.Value, actualSkill.Security[0][kvp.Key]);
                }
            }
        }

        // SupportedInterfaces
        Assert.Equal(expectedCard.SupportedInterfaces?.Count, actualCard.SupportedInterfaces?.Count);
        if (expectedCard.SupportedInterfaces?.Count > 0 && actualCard.SupportedInterfaces?.Count > 0)
        {
            Assert.Equal(expectedCard.SupportedInterfaces[0].ProtocolBinding, actualCard.SupportedInterfaces[0].ProtocolBinding);
            Assert.Equal(expectedCard.SupportedInterfaces[0].Url, actualCard.SupportedInterfaces[0].Url);
        }

        // Security schemes
        Assert.Equal(expectedCard.SecuritySchemes?.Count, actualCard.SecuritySchemes?.Count);
        Assert.Equal(expectedCard.SecurityRequirements?.Count, actualCard.SecurityRequirements?.Count);
    }
}
