using System.Text.Json;

namespace A2A.Compat.V03;

/// <summary>
/// Provides conversion methods between v0.3 and v1.0 A2A models.
/// </summary>
internal static class V03Adapter
{
    /// <summary>
    /// Converts a v0.3 Part to a v1.0 Part.
    /// </summary>
    /// <param name="v03Part">The v0.3 part to convert.</param>
    public static A2A.Part ToV1(Part v03Part)
    {
        return v03Part switch
        {
            TextPart tp => new A2A.Part { Text = tp.Text, Metadata = tp.Metadata },
            FilePart fp when fp.File is { Uri: { } uri } => new A2A.Part
            {
                Url = uri.ToString(),
                Filename = fp.File.Name,
                MediaType = fp.File.MimeType,
                Metadata = fp.Metadata
            },
            FilePart fp when fp.File is { Bytes: { } bytes } => new A2A.Part
            {
                Raw = bytes,
                Filename = fp.File.Name,
                MediaType = fp.File.MimeType,
                Metadata = fp.Metadata
            },
            DataPart dp => new A2A.Part
            {
                Data = JsonSerializer.SerializeToElement(dp.Data, A2AJsonUtilities.DefaultOptions.GetTypeInfo(typeof(Dictionary<string, JsonElement>))),
                Metadata = dp.Metadata
            },
            _ => new A2A.Part { Metadata = v03Part.Metadata }
        };
    }

    /// <summary>
    /// Converts a v1.0 Part to a v0.3 Part.
    /// </summary>
    /// <param name="v1Part">The v1.0 part to convert.</param>
    public static Part FromV1(A2A.Part v1Part)
    {
        if (v1Part.Text is not null)
        {
            return new TextPart { Text = v1Part.Text, Metadata = v1Part.Metadata };
        }

        if (v1Part.Url is not null)
        {
            return new FilePart
            {
                File = new FileContent(new Uri(v1Part.Url)) { Name = v1Part.Filename, MimeType = v1Part.MediaType },
                Metadata = v1Part.Metadata
            };
        }

        if (v1Part.Raw is not null)
        {
            return new FilePart
            {
                File = new FileContent(v1Part.Raw) { Name = v1Part.Filename, MimeType = v1Part.MediaType },
                Metadata = v1Part.Metadata
            };
        }

        if (v1Part.Data is { } data)
        {
            var dict = new Dictionary<string, JsonElement>();
            if (data.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in data.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.Clone();
                }
            }
            return new DataPart { Data = dict, Metadata = v1Part.Metadata };
        }

        return new TextPart { Text = string.Empty, Metadata = v1Part.Metadata };
    }

    /// <summary>
    /// Converts a v0.3 TaskState to a v1.0 TaskState.
    /// </summary>
    /// <param name="v03State">The v0.3 task state.</param>
    public static A2A.TaskState ToV1(TaskState v03State)
    {
        return v03State switch
        {
            TaskState.Submitted => A2A.TaskState.Submitted,
            TaskState.Working => A2A.TaskState.Working,
            TaskState.InputRequired => A2A.TaskState.InputRequired,
            TaskState.Completed => A2A.TaskState.Completed,
            TaskState.Canceled => A2A.TaskState.Canceled,
            TaskState.Failed => A2A.TaskState.Failed,
            TaskState.Rejected => A2A.TaskState.Rejected,
            TaskState.AuthRequired => A2A.TaskState.AuthRequired,
            _ => A2A.TaskState.Unknown,
        };
    }

    /// <summary>
    /// Converts a v1.0 TaskState to a v0.3 TaskState.
    /// </summary>
    /// <param name="v1State">The v1.0 task state.</param>
    public static TaskState FromV1(A2A.TaskState v1State)
    {
        return v1State switch
        {
            A2A.TaskState.Submitted => TaskState.Submitted,
            A2A.TaskState.Working => TaskState.Working,
            A2A.TaskState.InputRequired => TaskState.InputRequired,
            A2A.TaskState.Completed => TaskState.Completed,
            A2A.TaskState.Canceled => TaskState.Canceled,
            A2A.TaskState.Failed => TaskState.Failed,
            A2A.TaskState.Rejected => TaskState.Rejected,
            A2A.TaskState.AuthRequired => TaskState.AuthRequired,
            _ => TaskState.Unknown,
        };
    }

    /// <summary>
    /// Converts a v0.3 MessageRole to a v1.0 MessageRole.
    /// </summary>
    /// <param name="v03Role">The v0.3 role.</param>
    public static A2A.MessageRole ToV1(MessageRole v03Role)
    {
        return v03Role switch
        {
            MessageRole.User => A2A.MessageRole.User,
            MessageRole.Agent => A2A.MessageRole.Agent,
            _ => A2A.MessageRole.Unspecified,
        };
    }

    /// <summary>
    /// Converts a v0.3 method name to v1.0 method name.
    /// </summary>
    /// <param name="v03Method">The v0.3 method name.</param>
    public static string? ToV1Method(string v03Method)
    {
        return v03Method switch
        {
            V03Methods.MessageSend => A2AMethods.SendMessage,
            V03Methods.MessageStream => A2AMethods.SendStreamingMessage,
            V03Methods.TaskGet => A2AMethods.GetTask,
            V03Methods.TaskCancel => A2AMethods.CancelTask,
            V03Methods.TaskSubscribe => A2AMethods.SubscribeToTask,
            V03Methods.TaskPushNotificationConfigSet => A2AMethods.CreateTaskPushNotificationConfig,
            V03Methods.TaskPushNotificationConfigGet => A2AMethods.GetTaskPushNotificationConfig,
            _ => null,
        };
    }

    /// <summary>
    /// Converts a v1.0 method name to v0.3 method name.
    /// </summary>
    /// <param name="v1Method">The v1.0 method name.</param>
    public static string? FromV1Method(string v1Method)
    {
        return v1Method switch
        {
            A2AMethods.SendMessage => V03Methods.MessageSend,
            A2AMethods.SendStreamingMessage => V03Methods.MessageStream,
            A2AMethods.GetTask => V03Methods.TaskGet,
            A2AMethods.CancelTask => V03Methods.TaskCancel,
            A2AMethods.SubscribeToTask => V03Methods.TaskSubscribe,
            A2AMethods.CreateTaskPushNotificationConfig => V03Methods.TaskPushNotificationConfigSet,
            A2AMethods.GetTaskPushNotificationConfig => V03Methods.TaskPushNotificationConfigGet,
            _ => null,
        };
    }
}
