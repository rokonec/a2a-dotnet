# Migrating to A2A .NET SDK v1.0

This guide helps you migrate from the v0.3 SDK to the v1.0 SDK. The v1.0 SDK aligns with the [A2A Protocol v1.0 RC](https://a2a-protocol.org/latest/specification/).

## Breaking Changes at a Glance

| Area | What Changed |
|------|-------------|
| **Part model** | Flat class with nullable properties instead of class hierarchy |
| **Response types** | `SendMessageResponse` / `StreamResponse` oneof wrappers replace `A2AResponse` / `A2AEvent` |
| **Enum serialization** | SCREAMING_SNAKE_CASE (`TASK_STATE_COMPLETED`, `ROLE_USER`) |
| **JSON-RPC methods** | PascalCase (`SendMessage`, `GetTask`) |
| **AgentCard** | `SupportedInterfaces` array replaces `Url` + `PreferredTransport` |
| **SecurityScheme** | Flat oneof class replaces abstract inheritance |
| **JSON wire format** | No `kind` discriminator, oneof wrapper in responses |

## Part Model

### Before (v0.3)
```csharp
// Creating parts
var textPart = new TextPart { Text = "Hello" };
var filePart = new FilePart { File = new FileContent(uri) { MimeType = "image/png" } };
var dataPart = new DataPart { Data = myDictionary };

// Reading parts
switch (part)
{
    case TextPart tp: Console.WriteLine(tp.Text); break;
    case FilePart fp: Console.WriteLine(fp.File.Uri); break;
    case DataPart dp: Console.WriteLine(dp.Data["key"]); break;
}

// Or using cast helpers
var text = part.AsTextPart().Text;
```

### After (v1.0)
```csharp
// Creating parts — use static factories or set properties directly
var textPart = Part.FromText("Hello");
var filePart = Part.FromUrl(uri.ToString(), mediaType: "image/png");
var dataPart = Part.FromData(jsonElement);

// Or with object initializers (TextPart/FilePart/DataPart still exist as thin wrappers)
var textPart = new Part { Text = "Hello" };
var filePart = new Part { Url = "https://...", MediaType = "image/png", Filename = "photo.png" };

// Reading parts — use ContentCase or null-check properties directly
switch (part.ContentCase)
{
    case PartContentCase.Text: Console.WriteLine(part.Text); break;
    case PartContentCase.Url:  Console.WriteLine(part.Url); break;
    case PartContentCase.Raw:  Console.WriteLine(part.Raw); break;
    case PartContentCase.Data: Console.WriteLine(part.Data); break;
}

// Or simply
var text = part.Text; // null if not a text part
```

### JSON Format Change
```json
// v0.3: discriminated by "kind"
{"kind": "text", "text": "Hello"}
{"kind": "file", "file": {"uri": "https://...", "mimeType": "image/png"}}

// v1.0: flat oneof (no "kind", file content merged into Part)
{"text": "Hello"}
{"url": "https://...", "mediaType": "image/png", "filename": "photo.png"}
```

## Response Types

### Before (v0.3)
```csharp
// Handler returns A2AResponse (abstract base of AgentTask and AgentMessage)
taskManager.OnMessageReceived = async (params, ct) =>
{
    return new AgentMessage { ... }; // implicit upcast to A2AResponse
};

// Client receives A2AResponse
A2AResponse response = await client.SendMessageAsync(sendParams);
if (response is AgentTask task) { /* handle task */ }
if (response is AgentMessage msg) { /* handle message */ }

// Streaming yields A2AEvent (abstract base)
await foreach (var evt in client.SendMessageStreamingAsync(sendParams))
{
    if (evt.Data is TaskStatusUpdateEvent statusEvt) { ... }
}
```

### After (v1.0)
```csharp
// Handler returns SendMessageResponse (oneof wrapper)
taskManager.OnMessageReceived = async (params, ct) =>
{
    return new SendMessageResponse
    {
        Message = new AgentMessage { ... }
    };
};

// Client receives SendMessageResponse
SendMessageResponse response = await client.SendMessageAsync(sendParams);
switch (response.PayloadCase)
{
    case SendMessageResponseCase.Task:    var task = response.Task; break;
    case SendMessageResponseCase.Message: var msg = response.Message; break;
}

// Streaming yields StreamResponse (oneof wrapper)
await foreach (var evt in client.SendMessageStreamingAsync(sendParams))
{
    switch (evt.Data.PayloadCase)
    {
        case StreamResponseCase.Task:           var task = evt.Data.Task; break;
        case StreamResponseCase.StatusUpdate:   var status = evt.Data.StatusUpdate; break;
        case StreamResponseCase.ArtifactUpdate: var artifact = evt.Data.ArtifactUpdate; break;
    }
}
```

### JSON Format Change
```json
// v0.3: "kind" discriminator at top level
{"jsonrpc": "2.0", "id": "1", "result": {"kind": "task", "id": "...", "status": {...}}}

// v1.0: oneof wrapper with property name
{"jsonrpc": "2.0", "id": "1", "result": {"task": {"id": "...", "status": {...}}}}
```

## AgentCard

### Before (v0.3)
```csharp
var card = new AgentCard
{
    Name = "My Agent",
    Url = "https://agent.example.com/a2a",
    PreferredTransport = AgentTransport.JsonRpc,
    ProtocolVersion = "0.3.0",
    Version = "1.0.0",
    Capabilities = new AgentCapabilities { Streaming = true },
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    Skills = [...]
};

// Reading
var url = card.Url;
```

### After (v1.0)
```csharp
var card = new AgentCard
{
    Name = "My Agent",
    SupportedInterfaces =
    [
        new AgentInterface
        {
            Url = "https://agent.example.com/a2a",
            ProtocolBinding = "JSONRPC",
            ProtocolVersion = "1.0"
        }
    ],
    Version = "1.0.0",
    Capabilities = new AgentCapabilities { Streaming = true },
    DefaultInputModes = ["text/plain"],
    DefaultOutputModes = ["text/plain"],
    Skills = [...]
};

// Reading
var url = card.SupportedInterfaces[0].Url;
```

## SecurityScheme

### Before (v0.3)
```csharp
// Abstract class with inheritance and "type" discriminator
SecurityScheme scheme = new HttpAuthSecurityScheme("bearer");
SecurityScheme apiKey = new ApiKeySecurityScheme("X-API-Key", "header");

// Type checking
if (scheme is HttpAuthSecurityScheme http) { ... }
```

### After (v1.0)
```csharp
// Sealed class with nullable oneof properties
var scheme = new SecurityScheme
{
    HttpAuthSecurityScheme = new HttpAuthSecurityScheme { Scheme = "bearer" }
};
var apiKey = new SecurityScheme
{
    ApiKeySecurityScheme = new ApiKeySecurityScheme { Name = "X-API-Key", Location = "header" }
};

// Property checking
if (scheme.HttpAuthSecurityScheme is { } http) { ... }
```

Note: `ApiKeySecurityScheme.KeyLocation` was renamed to `Location` (matching the proto field name).

## Enum Serialization

C# enum member names are unchanged (`TaskState.Completed`, `MessageRole.User`), but JSON serialization now uses SCREAMING_SNAKE_CASE:

```
TaskState.Completed     → "TASK_STATE_COMPLETED"
TaskState.InputRequired → "TASK_STATE_INPUT_REQUIRED"  
MessageRole.User        → "ROLE_USER"
MessageRole.Agent       → "ROLE_AGENT"
```

A new `TaskState.Unspecified` and `MessageRole.Unspecified` value was added for proto compatibility.

## JSON-RPC Method Names

| v0.3 | v1.0 |
|------|------|
| `message/send` | `SendMessage` |
| `message/stream` | `SendStreamingMessage` |
| `tasks/get` | `GetTask` |
| `tasks/cancel` | `CancelTask` |
| `tasks/resubscribe` | `SubscribeToTask` |
| `tasks/pushNotificationConfig/set` | `CreateTaskPushNotificationConfig` |
| `tasks/pushNotificationConfig/get` | `GetTaskPushNotificationConfig` |
| *(new)* | `ListTasks` |
| *(new)* | `DeleteTaskPushNotificationConfig` |
| *(new)* | `ListTaskPushNotificationConfig` |
| *(new)* | `GetExtendedAgentCard` |

## Other Changes

- **`TaskStatusUpdateEvent.Final`** — Removed. Terminal state is inferred from `TaskState` (Completed, Canceled, Failed, Rejected).
- **`PushNotificationAuthenticationInfo.Schemes`** (list) → `.Scheme` (singular string).
- **`FileContent`**, **`AgentTransport`**, **`PartKind`** — Removed from v1.0. Available in `A2A.Compat.V03` namespace only.
- **`A2A-Version` header** — Clients should send `A2A-Version: 1.0` with requests.

## v0.3 Compatibility Layer

Original v0.3 types are preserved in the `A2A.Compat.V03` namespace at `src/A2A/Compat/V03/`:

- All v0.3 model classes with original serialization behavior
- `V03Methods` — v0.3 JSON-RPC method name constants
- `V03Adapter` — static methods to convert between v0.3 and v1.0 models

**To remove v0.3 support entirely:** delete the `src/A2A/Compat/V03/` folder. No v1.0 code references it.
