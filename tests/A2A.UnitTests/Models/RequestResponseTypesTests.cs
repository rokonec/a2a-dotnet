using System.Text.Json;

namespace A2A.UnitTests.Models;

public class RequestResponseTypesTests
{
    private static readonly JsonSerializerOptions s_options = A2AJsonUtilities.DefaultOptions;

    [Fact]
    public void ListTasksRequest_Serializes_AllFields()
    {
        var request = new ListTasksRequest
        {
            ContextId = "ctx-1",
            Status = TaskState.Working,
            PageSize = 25,
            PageToken = "token-abc",
            HistoryLength = 5,
            IncludeArtifacts = true,
            Tenant = "tenant-1"
        };

        var json = JsonSerializer.Serialize(request, s_options);
        var deserialized = JsonSerializer.Deserialize<ListTasksRequest>(json, s_options);

        Assert.NotNull(deserialized);
        Assert.Equal("ctx-1", deserialized.ContextId);
        Assert.Equal(TaskState.Working, deserialized.Status);
        Assert.Equal(25, deserialized.PageSize);
        Assert.Equal("token-abc", deserialized.PageToken);
        Assert.Equal(5, deserialized.HistoryLength);
        Assert.True(deserialized.IncludeArtifacts);
        Assert.Equal("tenant-1", deserialized.Tenant);
    }

    [Fact]
    public void ListTasksResponse_Serializes()
    {
        var response = new ListTasksResponse
        {
            Tasks = [new AgentTask { Id = "t1", ContextId = "c1" }],
            NextPageToken = "",
            PageSize = 50,
            TotalSize = 1
        };

        var json = JsonSerializer.Serialize(response, s_options);
        var deserialized = JsonSerializer.Deserialize<ListTasksResponse>(json, s_options);

        Assert.NotNull(deserialized);
        Assert.Single(deserialized.Tasks);
        Assert.Equal("t1", deserialized.Tasks[0].Id);
        Assert.Equal("", deserialized.NextPageToken);
        Assert.Equal(50, deserialized.PageSize);
        Assert.Equal(1, deserialized.TotalSize);
    }

    [Fact]
    public void SubscribeToTaskRequest_Serializes()
    {
        var request = new SubscribeToTaskRequest { Id = "task-123", Tenant = "t1" };
        var json = JsonSerializer.Serialize(request, s_options);
        var deserialized = JsonSerializer.Deserialize<SubscribeToTaskRequest>(json, s_options);

        Assert.NotNull(deserialized);
        Assert.Equal("task-123", deserialized.Id);
        Assert.Equal("t1", deserialized.Tenant);
    }

    [Fact]
    public void CreateTaskPushNotificationConfigRequest_Serializes()
    {
        var request = new CreateTaskPushNotificationConfigRequest
        {
            TaskId = "task-1",
            ConfigId = "config-1",
            Config = new PushNotificationConfig { Url = "https://webhook.example.com" }
        };

        var json = JsonSerializer.Serialize(request, s_options);
        var deserialized = JsonSerializer.Deserialize<CreateTaskPushNotificationConfigRequest>(json, s_options);

        Assert.NotNull(deserialized);
        Assert.Equal("task-1", deserialized.TaskId);
        Assert.Equal("config-1", deserialized.ConfigId);
        Assert.Equal("https://webhook.example.com", deserialized.Config.Url);
    }

    [Fact]
    public void DeleteTaskPushNotificationConfigRequest_Serializes()
    {
        var request = new DeleteTaskPushNotificationConfigRequest { TaskId = "t1", Id = "c1" };
        var json = JsonSerializer.Serialize(request, s_options);
        var deserialized = JsonSerializer.Deserialize<DeleteTaskPushNotificationConfigRequest>(json, s_options);

        Assert.NotNull(deserialized);
        Assert.Equal("t1", deserialized.TaskId);
        Assert.Equal("c1", deserialized.Id);
    }

    [Fact]
    public void ListTaskPushNotificationConfigRequest_Serializes()
    {
        var request = new ListTaskPushNotificationConfigRequest
        {
            TaskId = "t1",
            PageSize = 10,
            PageToken = "abc"
        };

        var json = JsonSerializer.Serialize(request, s_options);
        var deserialized = JsonSerializer.Deserialize<ListTaskPushNotificationConfigRequest>(json, s_options);

        Assert.NotNull(deserialized);
        Assert.Equal("t1", deserialized.TaskId);
        Assert.Equal(10, deserialized.PageSize);
    }

    [Fact]
    public void GetExtendedAgentCardRequest_Serializes()
    {
        var request = new GetExtendedAgentCardRequest { Tenant = "my-tenant" };
        var json = JsonSerializer.Serialize(request, s_options);
        var deserialized = JsonSerializer.Deserialize<GetExtendedAgentCardRequest>(json, s_options);

        Assert.NotNull(deserialized);
        Assert.Equal("my-tenant", deserialized.Tenant);
    }

    [Fact]
    public void SendMessageResponse_Task_PayloadCase()
    {
        var response = new SendMessageResponse
        {
            Task = new AgentTask { Id = "t1", ContextId = "c1" }
        };

        Assert.Equal(SendMessageResponseCase.Task, response.PayloadCase);
        Assert.NotNull(response.Task);
        Assert.Null(response.Message);
    }

    [Fact]
    public void SendMessageResponse_Message_PayloadCase()
    {
        var response = new SendMessageResponse
        {
            Message = new AgentMessage { MessageId = "m1", Role = MessageRole.Agent, Parts = [Part.FromText("hi")] }
        };

        Assert.Equal(SendMessageResponseCase.Message, response.PayloadCase);
        Assert.Null(response.Task);
        Assert.NotNull(response.Message);
    }

    [Fact]
    public void SendMessageResponse_RoundTrip_Task()
    {
        var response = new SendMessageResponse
        {
            Task = new AgentTask { Id = "t1", ContextId = "c1", Status = new AgentTaskStatus { State = TaskState.Submitted } }
        };

        var json = JsonSerializer.Serialize(response, s_options);
        Assert.Contains("\"task\"", json);
        Assert.DoesNotContain("\"message\"", json);

        var deserialized = JsonSerializer.Deserialize<SendMessageResponse>(json, s_options);
        Assert.NotNull(deserialized?.Task);
        Assert.Equal("t1", deserialized.Task.Id);
    }

    [Fact]
    public void StreamResponse_AllPayloadCases()
    {
        Assert.Equal(StreamResponseCase.Task,
            new StreamResponse { Task = new AgentTask { Id = "t" } }.PayloadCase);

        Assert.Equal(StreamResponseCase.Message,
            new StreamResponse { Message = new AgentMessage { MessageId = "m" } }.PayloadCase);

        Assert.Equal(StreamResponseCase.StatusUpdate,
            new StreamResponse { StatusUpdate = new TaskStatusUpdateEvent { TaskId = "t" } }.PayloadCase);

        Assert.Equal(StreamResponseCase.ArtifactUpdate,
            new StreamResponse { ArtifactUpdate = new TaskArtifactUpdateEvent { TaskId = "t" } }.PayloadCase);

        Assert.Equal(StreamResponseCase.None,
            new StreamResponse().PayloadCase);
    }

    [Fact]
    public void StreamResponse_RoundTrip_StatusUpdate()
    {
        var response = new StreamResponse
        {
            StatusUpdate = new TaskStatusUpdateEvent
            {
                TaskId = "t1",
                ContextId = "c1",
                Status = new AgentTaskStatus { State = TaskState.Working }
            }
        };

        var json = JsonSerializer.Serialize(response, s_options);
        Assert.Contains("\"statusUpdate\"", json);

        var deserialized = JsonSerializer.Deserialize<StreamResponse>(json, s_options);
        Assert.NotNull(deserialized?.StatusUpdate);
        Assert.Equal("t1", deserialized.StatusUpdate.TaskId);
        Assert.Equal(TaskState.Working, deserialized.StatusUpdate.Status.State);
    }
}
