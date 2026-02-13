using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Defines the possible lifecycle states of a Task.
/// </summary>
[JsonConverter(typeof(ProtoJsonEnumConverter<TaskState>))]
public enum TaskState
{
    /// <summary>
    /// The task is in an unknown or indeterminate state.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_UNSPECIFIED")]
    Unspecified,

    /// <summary>
    /// Indicates that the task has been submitted.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_SUBMITTED")]
    Submitted,

    /// <summary>
    /// Indicates that the task is currently being worked on.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_WORKING")]
    Working,

    /// <summary>
    /// Indicates that the task requires input from the user.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_INPUT_REQUIRED")]
    InputRequired,

    /// <summary>
    /// Indicates that the task has been completed successfully.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_COMPLETED")]
    Completed,

    /// <summary>
    /// Indicates that the task has been canceled.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_CANCELED")]
    Canceled,

    /// <summary>
    /// Indicates that the task has failed.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_FAILED")]
    Failed,

    /// <summary>
    /// Indicates that the task has been rejected.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_REJECTED")]
    Rejected,

    /// <summary>
    /// Indicates that the task requires authentication.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_AUTH_REQUIRED")]
    AuthRequired,

    /// <summary>
    /// Indicates that the task state is unknown.
    /// </summary>
    [EnumMember(Value = "TASK_STATE_UNKNOWN")]
    Unknown
}