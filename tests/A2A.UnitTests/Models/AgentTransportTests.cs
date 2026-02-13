using System.Text.Json;

namespace A2A.UnitTests.Models;

public class AgentInterfaceProtocolBindingTests
{
    [Fact]
    public void ProtocolBinding_SetsCorrectly()
    {
        // Arrange & Act
        var agentInterface = new AgentInterface
        {
            Url = "https://example.com/agent",
            ProtocolBinding = "JSONRPC"
        };

        // Assert
        Assert.Equal("JSONRPC", agentInterface.ProtocolBinding);
    }

    [Fact]
    public void SerializesCorrectlyWithinAgentInterface()
    {
        // Arrange
        var agentInterface = new AgentInterface
        {
            ProtocolBinding = "GRPC",
            Url = "https://example.com/agent"
        };

        // Act
        var json = JsonSerializer.Serialize(agentInterface, A2AJsonUtilities.DefaultOptions);

        // Assert
        Assert.Contains("\"protocolBinding\":\"GRPC\"", json);
    }

    [Fact]
    public void CanSerializeAndDeserializeAgentInterface()
    {
        // Arrange
        var agentInterface = new AgentInterface
        {
            Url = "https://example.com/agent",
            ProtocolBinding = "JSONRPC",
            ProtocolVersion = "1.0"
        };

        // Act
        var json = JsonSerializer.Serialize(agentInterface, A2AJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<AgentInterface>(json, A2AJsonUtilities.DefaultOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("https://example.com/agent", deserialized.Url);
        Assert.Equal("JSONRPC", deserialized.ProtocolBinding);
        Assert.Equal("1.0", deserialized.ProtocolVersion);
    }
}
