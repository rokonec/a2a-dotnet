namespace A2A.Compat.V03;

/// <summary>
/// Constants for A2A v0.3 JSON-RPC method names.
/// </summary>
internal static class V03Methods
{
    public const string MessageSend = "message/send";
    public const string MessageStream = "message/stream";
    public const string TaskGet = "tasks/get";
    public const string TaskCancel = "tasks/cancel";
    public const string TaskSubscribe = "tasks/resubscribe";
    public const string TaskPushNotificationConfigSet = "tasks/pushNotificationConfig/set";
    public const string TaskPushNotificationConfigGet = "tasks/pushNotificationConfig/get";

    public static bool IsStreamingMethod(string method) => method is MessageStream or TaskSubscribe;

    public static bool IsValidMethod(string method) => method is
        MessageSend or MessageStream or TaskGet or TaskCancel or TaskSubscribe or
        TaskPushNotificationConfigSet or TaskPushNotificationConfigGet;
}
