namespace A2A;

/// <summary>
/// Constants for A2A JSON-RPC method names (v1.0 PascalCase).
/// </summary>
public static class A2AMethods
{
    /// <summary>
    /// Send a message to the agent.
    /// </summary>
    public const string SendMessage = "SendMessage";

    /// <summary>
    /// Send a message with streaming response.
    /// </summary>
    public const string SendStreamingMessage = "SendStreamingMessage";

    /// <summary>
    /// Get the current state of a task.
    /// </summary>
    public const string GetTask = "GetTask";

    /// <summary>
    /// List tasks with optional filtering and pagination.
    /// </summary>
    public const string ListTasks = "ListTasks";

    /// <summary>
    /// Cancel a task.
    /// </summary>
    public const string CancelTask = "CancelTask";

    /// <summary>
    /// Subscribe to task updates via streaming.
    /// </summary>
    public const string SubscribeToTask = "SubscribeToTask";

    /// <summary>
    /// Create a push notification configuration for a task.
    /// </summary>
    public const string CreateTaskPushNotificationConfig = "CreateTaskPushNotificationConfig";

    /// <summary>
    /// Get a push notification configuration for a task.
    /// </summary>
    public const string GetTaskPushNotificationConfig = "GetTaskPushNotificationConfig";

    /// <summary>
    /// List push notification configurations for a task.
    /// </summary>
    public const string ListTaskPushNotificationConfig = "ListTaskPushNotificationConfig";

    /// <summary>
    /// Delete a push notification configuration for a task.
    /// </summary>
    public const string DeleteTaskPushNotificationConfig = "DeleteTaskPushNotificationConfig";

    /// <summary>
    /// Get the extended agent card for authenticated agents.
    /// </summary>
    public const string GetExtendedAgentCard = "GetExtendedAgentCard";

    /// <summary>
    /// Determines if a method requires streaming response handling.
    /// </summary>
    /// <param name="method">The method name to check.</param>
    /// <returns>True if the method requires streaming, false otherwise.</returns>
    public static bool IsStreamingMethod(string method) => method is SendStreamingMessage or SubscribeToTask;

    /// <summary>
    /// Determines if a method name is valid for A2A JSON-RPC.
    /// </summary>
    /// <param name="method">The method name to validate.</param>
    /// <returns>True if the method is valid, false otherwise.</returns>
    public static bool IsValidMethod(string method) => method is
        SendMessage or SendStreamingMessage or
        GetTask or ListTasks or CancelTask or SubscribeToTask or
        CreateTaskPushNotificationConfig or GetTaskPushNotificationConfig or
        ListTaskPushNotificationConfig or DeleteTaskPushNotificationConfig or
        GetExtendedAgentCard;
}