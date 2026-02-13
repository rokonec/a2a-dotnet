using System.Text.Json;

namespace A2A.UnitTests.Models
{
    public sealed class A2AEventTests
    {
        private static readonly Dictionary<string, string> expectedMetadata = new()
        {
            ["createdAt"] = "2023-01-01T00:00:00Z"
        };

        [Fact]
        public void StreamResponse_Deserialize_Message_Succeeds()
        {
            // Arrange
            const string json = """
            {
                "message": {
                    "role": "ROLE_USER",
                    "messageId": "m-1",
                    "taskId": "t-1",
                    "contextId": "c-1",
                    "referenceTaskIds": [ "r-1", "r-2" ],
                    "parts": [ { "text": "hi" } ],
                    "extensions": [ "foo", "bar" ],
                    "metadata": {
                        "createdAt": "2023-01-01T00:00:00Z"
                    }
                }
            }
            """;
            var expectedReferenceTaskIds = new[] { "r-1", "r-2" };
            var expectedParts = new[] { new TextPart() { Text = "hi" } };
            var expectedExtensions = new[] { "foo", "bar" };

            // Act
            var streamResponse = JsonSerializer.Deserialize<StreamResponse>(json, A2AJsonUtilities.DefaultOptions);
            Assert.NotNull(streamResponse);
            Assert.Equal(StreamResponseCase.Message, streamResponse.PayloadCase);
            var message = streamResponse.Message!;

            // Assert
            Assert.Equal(MessageRole.User, message.Role);
            Assert.Equal("m-1", message.MessageId);
            Assert.Equal("t-1", message.TaskId);
            Assert.Equal("c-1", message.ContextId);
            Assert.Equal(expectedReferenceTaskIds, message.ReferenceTaskIds);
            Assert.Single(message.Parts);
            Assert.Equal(expectedParts[0].Text, message.Parts[0].Text);
            Assert.Equal(expectedExtensions, message.Extensions);
            Assert.NotNull(message.Metadata);
            Assert.Single(message.Metadata);
            Assert.Equal(expectedMetadata["createdAt"], message.Metadata["createdAt"].GetString());
        }

        [Fact]
        public void StreamResponse_Deserialize_AgentTask_Succeeds()
        {
            // Arrange
            const string json = """
            {
                "task": {
                    "id": "t-3",
                    "contextId": "c-3",
                    "status": { "state": "TASK_STATE_SUBMITTED" },
                    "artifacts": [
                        { "artifactId": "f-1", "name": "file1.txt", "description": "A text file", "parts": [] }
                    ],
                    "history": [
                        { "role": "ROLE_USER", "messageId": "m-3", "parts": [] }
                    ],
                    "metadata": {
                        "createdAt": "2023-01-01T00:00:00Z"
                    }
                }
            }
            """;

            // Act
            var streamResponse = JsonSerializer.Deserialize<StreamResponse>(json, A2AJsonUtilities.DefaultOptions);
            Assert.NotNull(streamResponse);
            Assert.Equal(StreamResponseCase.Task, streamResponse.PayloadCase);
            var agentTask = streamResponse.Task!;

            // Assert
            Assert.Equal("t-3", agentTask.Id);
            Assert.Equal("c-3", agentTask.ContextId);
            Assert.Equal(TaskState.Submitted, agentTask.Status.State);
            Assert.NotNull(agentTask.Artifacts);
            Assert.Single(agentTask.Artifacts);
            Assert.Equal("f-1", agentTask.Artifacts[0].ArtifactId);
            Assert.Equal("file1.txt", agentTask.Artifacts[0].Name);
            Assert.Equal("A text file", agentTask.Artifacts[0].Description);
            Assert.NotNull(agentTask.History);
            Assert.Single(agentTask.History);
            Assert.Equal(MessageRole.User, agentTask.History![0].Role);
            Assert.Equal("m-3", agentTask.History![0].MessageId);
            Assert.NotNull(agentTask.Metadata);
            Assert.Single(agentTask.Metadata);
            Assert.Equal(expectedMetadata["createdAt"], agentTask.Metadata["createdAt"].GetString());
        }

        [Fact]
        public void StreamResponse_Deserialize_TaskStatusUpdateEvent_Succeeds()
        {
            // Arrange
            const string json = """
            {
                "statusUpdate": {
                    "taskId": "t-5",
                    "contextId": "c-5",
                    "status": { "state": "TASK_STATE_WORKING" },
                    "metadata": {
                        "createdAt": "2023-01-01T00:00:00Z"
                    }
                }
            }
            """;

            // Act
            var streamResponse = JsonSerializer.Deserialize<StreamResponse>(json, A2AJsonUtilities.DefaultOptions);
            Assert.NotNull(streamResponse);
            Assert.Equal(StreamResponseCase.StatusUpdate, streamResponse.PayloadCase);
            var taskStatusUpdateEvent = streamResponse.StatusUpdate!;

            // Assert
            Assert.Equal("t-5", taskStatusUpdateEvent.TaskId);
            Assert.Equal("c-5", taskStatusUpdateEvent.ContextId);
            Assert.Equal(TaskState.Working, taskStatusUpdateEvent.Status.State);
            Assert.NotNull(taskStatusUpdateEvent.Metadata);
            Assert.Single(taskStatusUpdateEvent.Metadata);
            Assert.Equal(expectedMetadata["createdAt"], taskStatusUpdateEvent.Metadata["createdAt"].GetString());
        }

        [Fact]
        public void StreamResponse_Deserialize_TaskArtifactUpdateEvent_Succeeds()
        {
            // Arrange
            const string json = """
            {
                "artifactUpdate": {
                    "taskId": "t-7",
                    "contextId": "c-7",
                    "artifact": {
                        "artifactId": "a-1",
                        "parts": [ { "text": "chunk" } ]
                    },
                    "append": true,
                    "lastChunk": false,
                    "metadata": {
                        "createdAt": "2023-01-01T00:00:00Z"
                    }
                }
            }
            """;

            // Act
            var streamResponse = JsonSerializer.Deserialize<StreamResponse>(json, A2AJsonUtilities.DefaultOptions);
            Assert.NotNull(streamResponse);
            Assert.Equal(StreamResponseCase.ArtifactUpdate, streamResponse.PayloadCase);
            var taskArtifactUpdateEvent = streamResponse.ArtifactUpdate!;

            // Assert
            Assert.Equal("t-7", taskArtifactUpdateEvent.TaskId);
            Assert.Equal("c-7", taskArtifactUpdateEvent.ContextId);
            Assert.Equal("a-1", taskArtifactUpdateEvent.Artifact.ArtifactId);
            Assert.Single(taskArtifactUpdateEvent.Artifact.Parts);
            Assert.Equal("chunk", taskArtifactUpdateEvent.Artifact.Parts[0].Text);
            Assert.True(taskArtifactUpdateEvent.Append);
            Assert.False(taskArtifactUpdateEvent.LastChunk);
            Assert.NotNull(taskArtifactUpdateEvent.Metadata);
            Assert.Single(taskArtifactUpdateEvent.Metadata);
            Assert.Equal(expectedMetadata["createdAt"], taskArtifactUpdateEvent.Metadata["createdAt"].GetString());
        }

        [Fact]
        public void StreamResponse_Deserialize_Empty_ReturnsNone()
        {
            // Arrange
            const string json = "{}";

            // Act
            var streamResponse = JsonSerializer.Deserialize<StreamResponse>(json, A2AJsonUtilities.DefaultOptions);

            // Assert
            Assert.NotNull(streamResponse);
            Assert.Equal(StreamResponseCase.None, streamResponse.PayloadCase);
        }

        [Fact]
        public void StreamResponse_Serialize_AllKnownTypes_Succeeds()
        {
            // Arrange
            var streamResponses = new StreamResponse[]
            {
                new() { Message = new AgentMessage { Role = MessageRole.User, MessageId = "m-7", Parts = [new TextPart { Text = "hello" }] } },
                new() { Task = new AgentTask { Id = "t-9", ContextId = "c-9", Status = new AgentTaskStatus { State = TaskState.Submitted, Timestamp = DateTimeOffset.Parse("2023-01-01T00:00:00+00:00", null) } } },
                new() { StatusUpdate = new TaskStatusUpdateEvent { TaskId = "t-10", ContextId = "c-10", Status = new AgentTaskStatus { State = TaskState.Working, Timestamp = DateTimeOffset.Parse("2023-01-01T00:00:00+00:00", null) } } },
                new() { ArtifactUpdate = new TaskArtifactUpdateEvent { TaskId = "t-11", ContextId = "c-11" } }
            };

            for (var i = 0; i < streamResponses.Length; i++)
            {
                // Act
                var json = JsonSerializer.Serialize(streamResponses[i], A2AJsonUtilities.DefaultOptions);

                // Assert - verify round-trip
                var deserialized = JsonSerializer.Deserialize<StreamResponse>(json, A2AJsonUtilities.DefaultOptions);
                Assert.NotNull(deserialized);
                Assert.Equal(streamResponses[i].PayloadCase, deserialized.PayloadCase);
            }
        }
    }
}
