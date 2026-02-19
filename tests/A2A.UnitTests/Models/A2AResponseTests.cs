using System.Text.Json;

namespace A2A.UnitTests.Models;

public class A2AResponseTests
{
    private static readonly Dictionary<string, string> expectedMetadata = new()
    {
        ["createdAt"] = "2023-01-01T00:00:00Z"
    };

    [Fact]
    public void SendMessageResponse_Deserialize_Message_Succeeds()
    {
        // Arrange
        const string json = """
        {
            "message": {
                "role": "ROLE_USER",
                "messageId": "m-2",
                "taskId": "t-2",
                "contextId": "c-2",
                "referenceTaskIds": [ "r-3", "r-4" ],
                "parts": [ { "text": "hi" } ],
                "extensions": [ "foo", "bar" ],
                "metadata": {
                    "createdAt": "2023-01-01T00:00:00Z"
                }
            }
        }
        """;
        var expectedReferenceTaskIds = new[] { "r-3", "r-4" };
        var expectedParts = new[] { new TextPart { Text = "hi" } };
        var expectedExtensions = new[] { "foo", "bar" };

        // Act
        var sendMessageResponse = JsonSerializer.Deserialize<SendMessageResponse>(json, A2AJsonUtilities.DefaultOptions);
        Assert.NotNull(sendMessageResponse);
        Assert.Equal(SendMessageResponseCase.Message, sendMessageResponse.PayloadCase);
        var message = sendMessageResponse.Message!;

        // Assert
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("m-2", message.MessageId);
        Assert.Equal("t-2", message.TaskId);
        Assert.Equal("c-2", message.ContextId);
        Assert.Equal(expectedReferenceTaskIds, message.ReferenceTaskIds);
        Assert.Single(message.Parts);
        Assert.Equal(expectedParts[0].Text, message.Parts[0].Text);
        Assert.Equal(expectedExtensions, message.Extensions);
        Assert.NotNull(message.Metadata);
        Assert.Single(message.Metadata);
        Assert.Equal(expectedMetadata["createdAt"], message.Metadata["createdAt"].GetString());
    }

    [Fact]
    public void SendMessageResponse_Deserialize_AgentTask_Succeeds()
    {
        // Arrange
        const string json = """
        {
            "task": {
                "id": "t-4",
                "contextId": "c-4",
                "status": { "state": "TASK_STATE_SUBMITTED" },
                "artifacts": [
                    { "artifactId": "f-2", "name": "file2.txt", "description": "A text file", "parts": [] }
                ],
                "history": [
                    { "role": "ROLE_USER", "messageId": "m-4", "parts": [ { "text": "go" } ] }
                ],
                "metadata": {
                    "createdAt": "2023-01-01T00:00:00Z"
                }
            }
        }
        """;

        // Act
        var sendMessageResponse = JsonSerializer.Deserialize<SendMessageResponse>(json, A2AJsonUtilities.DefaultOptions);
        Assert.NotNull(sendMessageResponse);
        Assert.Equal(SendMessageResponseCase.Task, sendMessageResponse.PayloadCase);
        var agentTask = sendMessageResponse.Task!;

        // Assert
        Assert.Equal("t-4", agentTask.Id);
        Assert.Equal("c-4", agentTask.ContextId);
        Assert.Equal(TaskState.Submitted, agentTask.Status.State);
        Assert.NotNull(agentTask.Artifacts);
        Assert.Single(agentTask.Artifacts);
        Assert.Equal("f-2", agentTask.Artifacts[0].ArtifactId);
        Assert.Equal("file2.txt", agentTask.Artifacts[0].Name);
        Assert.Equal("A text file", agentTask.Artifacts[0].Description);
        Assert.NotNull(agentTask.History);
        Assert.Single(agentTask.History);
        Assert.Equal(MessageRole.User, agentTask.History![0].Role);
        Assert.Equal("m-4", agentTask.History![0].MessageId);
        Assert.NotNull(agentTask.Metadata);
        Assert.Single(agentTask.Metadata);
        Assert.Equal(expectedMetadata["createdAt"], agentTask.Metadata["createdAt"].GetString());
    }

    [Fact]
    public void SendMessageResponse_Deserialize_Empty_ReturnsNone()
    {
        // Arrange
        const string json = "{}";

        // Act
        var sendMessageResponse = JsonSerializer.Deserialize<SendMessageResponse>(json, A2AJsonUtilities.DefaultOptions);

        // Assert
        Assert.NotNull(sendMessageResponse);
        Assert.Equal(SendMessageResponseCase.None, sendMessageResponse.PayloadCase);
    }

    [Fact]
    public void SendMessageResponse_Serialize_AllKnownTypes_Succeeds()
    {
        // Arrange
        var responses = new SendMessageResponse[] {
            new() { Message = new AgentMessage { Role = MessageRole.User, MessageId = "m-8", Parts = [new TextPart { Text = "hello" }] } },
            new() { Task = new AgentTask { Id = "t-12", ContextId = "c-12", Status = new AgentTaskStatus { State = TaskState.Submitted, Timestamp = DateTimeOffset.Parse("2023-01-01T00:00:00+00:00", null) } } }
        };

        for (var i = 0; i < responses.Length; i++)
        {
            // Act
            var json = JsonSerializer.Serialize(responses[i], A2AJsonUtilities.DefaultOptions);

            // Assert - verify round-trip
            var deserialized = JsonSerializer.Deserialize<SendMessageResponse>(json, A2AJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            Assert.Equal(responses[i].PayloadCase, deserialized.PayloadCase);
        }
    }
}
