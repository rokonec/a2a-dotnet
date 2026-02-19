using System.Text.Json;

namespace A2A.UnitTests.Models;

public class ProtoJsonEnumConverterTests
{
    [Theory]
    [InlineData(TaskState.Submitted, "TASK_STATE_SUBMITTED")]
    [InlineData(TaskState.Working, "TASK_STATE_WORKING")]
    [InlineData(TaskState.InputRequired, "TASK_STATE_INPUT_REQUIRED")]
    [InlineData(TaskState.Completed, "TASK_STATE_COMPLETED")]
    [InlineData(TaskState.Canceled, "TASK_STATE_CANCELED")]
    [InlineData(TaskState.Failed, "TASK_STATE_FAILED")]
    [InlineData(TaskState.Rejected, "TASK_STATE_REJECTED")]
    [InlineData(TaskState.AuthRequired, "TASK_STATE_AUTH_REQUIRED")]
    [InlineData(TaskState.Unspecified, "TASK_STATE_UNSPECIFIED")]
    [InlineData(TaskState.Unknown, "TASK_STATE_UNKNOWN")]
    public void TaskState_Serializes_ToScreamingSnakeCase(TaskState state, string expected)
    {
        var json = JsonSerializer.Serialize(state, A2AJsonUtilities.DefaultOptions);
        Assert.Equal($"\"{expected}\"", json);
    }

    [Theory]
    [InlineData("TASK_STATE_SUBMITTED", TaskState.Submitted)]
    [InlineData("TASK_STATE_COMPLETED", TaskState.Completed)]
    [InlineData("TASK_STATE_INPUT_REQUIRED", TaskState.InputRequired)]
    public void TaskState_Deserializes_FromScreamingSnakeCase(string json, TaskState expected)
    {
        var result = JsonSerializer.Deserialize<TaskState>($"\"{json}\"", A2AJsonUtilities.DefaultOptions);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(MessageRole.User, "ROLE_USER")]
    [InlineData(MessageRole.Agent, "ROLE_AGENT")]
    [InlineData(MessageRole.Unspecified, "ROLE_UNSPECIFIED")]
    public void MessageRole_Serializes_ToScreamingSnakeCase(MessageRole role, string expected)
    {
        var json = JsonSerializer.Serialize(role, A2AJsonUtilities.DefaultOptions);
        Assert.Equal($"\"{expected}\"", json);
    }

    [Theory]
    [InlineData("ROLE_USER", MessageRole.User)]
    [InlineData("ROLE_AGENT", MessageRole.Agent)]
    public void MessageRole_Deserializes_FromScreamingSnakeCase(string json, MessageRole expected)
    {
        var result = JsonSerializer.Deserialize<MessageRole>($"\"{json}\"", A2AJsonUtilities.DefaultOptions);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TaskState_RoundTrips()
    {
        foreach (var state in Enum.GetValues<TaskState>())
        {
            var json = JsonSerializer.Serialize(state, A2AJsonUtilities.DefaultOptions);
            var deserialized = JsonSerializer.Deserialize<TaskState>(json, A2AJsonUtilities.DefaultOptions);
            Assert.Equal(state, deserialized);
        }
    }

    [Fact]
    public void TaskState_InvalidValue_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TaskState>("\"INVALID_STATE\"", A2AJsonUtilities.DefaultOptions));
    }

    [Fact]
    public void TaskState_CaseInsensitive_Deserializes()
    {
        var result = JsonSerializer.Deserialize<TaskState>("\"task_state_completed\"", A2AJsonUtilities.DefaultOptions);
        Assert.Equal(TaskState.Completed, result);
    }
}
