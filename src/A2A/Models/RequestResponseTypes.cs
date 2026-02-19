using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Parameters for listing tasks with optional filtering criteria.
/// </summary>
public sealed class ListTasksRequest
{
    /// <summary>
    /// Optional tenant, provided as a path parameter.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// Filter tasks by context ID to get tasks from a specific conversation or session.
    /// </summary>
    [JsonPropertyName("contextId")]
    public string? ContextId { get; set; }

    /// <summary>
    /// Filter tasks by their current status state.
    /// </summary>
    [JsonPropertyName("status")]
    public TaskState? Status { get; set; }

    /// <summary>
    /// Maximum number of tasks to return. Must be between 1 and 100. Defaults to 50.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }

    /// <summary>
    /// Token for pagination. Use the nextPageToken from a previous ListTasksResponse.
    /// </summary>
    [JsonPropertyName("pageToken")]
    public string? PageToken { get; set; }

    /// <summary>
    /// The maximum number of messages to include in each task's history.
    /// </summary>
    [JsonPropertyName("historyLength")]
    public int? HistoryLength { get; set; }

    /// <summary>
    /// Filter tasks which have a status updated after the provided timestamp.
    /// </summary>
    [JsonPropertyName("statusTimestampAfter")]
    public DateTimeOffset? StatusTimestampAfter { get; set; }

    /// <summary>
    /// Whether to include artifacts in the returned tasks. Defaults to false.
    /// </summary>
    [JsonPropertyName("includeArtifacts")]
    public bool? IncludeArtifacts { get; set; }
}

/// <summary>
/// Result object for ListTasks method.
/// </summary>
public sealed class ListTasksResponse
{
    /// <summary>
    /// Array of tasks matching the specified criteria.
    /// </summary>
    [JsonPropertyName("tasks")]
    [JsonRequired]
    public List<AgentTask> Tasks { get; set; } = [];

    /// <summary>
    /// Token for retrieving the next page. Empty string if no more results.
    /// </summary>
    [JsonPropertyName("nextPageToken")]
    [JsonRequired]
    public string NextPageToken { get; set; } = string.Empty;

    /// <summary>
    /// The size of page requested.
    /// </summary>
    [JsonPropertyName("pageSize")]
    [JsonRequired]
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of tasks available (before pagination).
    /// </summary>
    [JsonPropertyName("totalSize")]
    [JsonRequired]
    public int TotalSize { get; set; }
}

/// <summary>
/// Request for subscribing to task updates.
/// </summary>
public sealed class SubscribeToTaskRequest
{
    /// <summary>
    /// Optional tenant.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// The resource id of the task to subscribe to.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Request for creating a push notification configuration.
/// </summary>
public sealed class CreateTaskPushNotificationConfigRequest
{
    /// <summary>
    /// Optional tenant.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// The parent task resource id.
    /// </summary>
    [JsonPropertyName("taskId")]
    [JsonRequired]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// The ID for the new config.
    /// </summary>
    [JsonPropertyName("configId")]
    [JsonRequired]
    public string ConfigId { get; set; } = string.Empty;

    /// <summary>
    /// The configuration to create.
    /// </summary>
    [JsonPropertyName("config")]
    [JsonRequired]
    public PushNotificationConfig Config { get; set; } = new();
}

/// <summary>
/// Request for deleting a push notification configuration.
/// </summary>
public sealed class DeleteTaskPushNotificationConfigRequest
{
    /// <summary>
    /// Optional tenant.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// The parent task resource id.
    /// </summary>
    [JsonPropertyName("taskId")]
    [JsonRequired]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// The resource id of the config to delete.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Request for listing push notification configurations for a task.
/// </summary>
public sealed class ListTaskPushNotificationConfigRequest
{
    /// <summary>
    /// Optional tenant.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// The parent task resource id.
    /// </summary>
    [JsonPropertyName("taskId")]
    [JsonRequired]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// The maximum number of configurations to return.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }

    /// <summary>
    /// A page token received from a previous call.
    /// </summary>
    [JsonPropertyName("pageToken")]
    public string? PageToken { get; set; }
}

/// <summary>
/// Response for listing push notification configurations.
/// </summary>
public sealed class ListTaskPushNotificationConfigResponse
{
    /// <summary>
    /// The list of push notification configurations.
    /// </summary>
    [JsonPropertyName("configs")]
    public List<TaskPushNotificationConfig>? Configs { get; set; }

    /// <summary>
    /// A token to retrieve the next page. Omitted if no more pages.
    /// </summary>
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
}

/// <summary>
/// Request for getting the extended agent card.
/// </summary>
public sealed class GetExtendedAgentCardRequest
{
    /// <summary>
    /// Optional tenant.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }
}
