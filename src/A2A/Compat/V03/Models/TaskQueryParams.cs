using System.Text.Json.Serialization;

namespace A2A.Compat.V03;

/// <summary>
/// Parameters for querying a task, including optional history length.
/// </summary>
public sealed class TaskQueryParams : TaskIdParams
{
    /// <summary>
    /// Number of recent messages to be retrieved.
    /// </summary>
    [JsonPropertyName("historyLength")]
    public int? HistoryLength { get; set; }
}