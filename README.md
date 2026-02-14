# A2A .NET SDK

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/A2A.svg)](https://www.nuget.org/packages/A2A/)

A .NET library that helps run agentic applications as A2AServers following the [Agent2Agent (A2A) Protocol](https://a2a-protocol.org).

The A2A .NET SDK provides a robust implementation of the Agent2Agent (A2A) protocol, enabling seamless communication between AI agents and applications. This library offers both high-level abstractions and fine-grained control, making it easy to build A2A-compatible agents while maintaining flexibility for advanced use cases.

Key features include:
- **Agent Capability Discovery**: Retrieve agent capabilities and metadata through agent cards
- **Message-based Communication**: Direct, stateless messaging with immediate responses
- **Task-based Communication**: Create and manage persistent, long-running agent tasks
- **Streaming Support**: Real-time communication using Server-Sent Events
- **ASP.NET Core Integration**: Built-in extensions for hosting A2A agents in web applications
- **Cross-platform Compatibility**: Supports .NET Standard 2.0 and .NET 8+

## Protocol Compatibility

This library implements the A2A Protocol **v1.0 Release Candidate**. Key v1.0 features:

- **Flat Part model** — `text`, `raw`, `url`, `data` as oneof properties (no `kind` discriminator)
- **SCREAMING_SNAKE_CASE enums** — `TASK_STATE_COMPLETED`, `ROLE_USER`, etc. per ProtoJSON spec
- **PascalCase JSON-RPC methods** — `SendMessage`, `GetTask`, `CancelTask`, etc.
- **SupportedInterfaces** — AgentCard declares protocol bindings via `supportedInterfaces` array
- **SecurityScheme oneof** — `apiKeySecurityScheme`, `httpAuthSecurityScheme`, etc.
- **New operations** — `ListTasks`, `SubscribeToTask`, `GetExtendedAgentCard`, push notification CRUD
- **A2A-Version header** — Protocol version negotiation
- **New error codes** — `VersionNotSupported`, `InvalidAgentResponse`, `ExtendedAgentCardNotConfigured`, `ExtensionSupportRequired`

For migration details from v0.3, see the **[Migration Guide](docs/migration-guide-v1.md)** with before/after code examples.

### Migrating from v0.3

If you are upgrading from the v0.3 SDK, here are the key breaking changes:

| v0.3 | v1.0 |
|------|------|
| `new TextPart { Text = "hi" }` | `Part.FromText("hi")` or `new Part { Text = "hi" }` |
| `part.AsTextPart().Text` | `part.Text` (check `part.ContentCase`) |
| `new FilePart { File = new FileContent(uri) }` | `Part.FromUrl(uri.ToString(), mediaType)` |
| `A2AResponse` (abstract: AgentTask or AgentMessage) | `SendMessageResponse` (oneof: `.Task` or `.Message`) |
| `A2AEvent` (abstract: all event types) | `StreamResponse` (oneof: `.Task`, `.Message`, `.StatusUpdate`, `.ArtifactUpdate`) |
| `Task<A2AResponse>` return type | `Task<SendMessageResponse>` return type |
| `IAsyncEnumerable<A2AEvent>` | `IAsyncEnumerable<StreamResponse>` |
| `AgentCard.Url` | `AgentCard.SupportedInterfaces[0].Url` |
| `AgentCard.PreferredTransport` | `AgentCard.SupportedInterfaces[0].ProtocolBinding` |
| `AgentCard.ProtocolVersion` | `AgentCard.SupportedInterfaces[0].ProtocolVersion` |
| `AgentCapabilities.Streaming` (bool) | `AgentCapabilities.Streaming` (bool?) |
| `SecurityScheme` (abstract, inheritance) | `SecurityScheme` (sealed, oneof properties) |
| `FileContent`, `AgentTransport`, `PartKind` | Removed from v1.0 (in `Compat/V03/` only) |
| `PushNotificationAuthenticationInfo.Schemes` (list) | `PushNotificationAuthenticationInfo.Scheme` (string) |
| `TaskStatusUpdateEvent.Final` | Removed — infer from `TaskState` |
| `A2AMethods.MessageSend` (`"message/send"`) | `A2AMethods.SendMessage` (`"SendMessage"`) |
| JSON: `"role": "user"` | JSON: `"role": "ROLE_USER"` |
| JSON: `"state": "completed"` | JSON: `"state": "TASK_STATE_COMPLETED"` |
| JSON: `{"kind":"text","text":"hi"}` | JSON: `{"text":"hi"}` |
| JSON: `{"kind":"task",...}` response | JSON: `{"task":{...}}` response (oneof wrapper) |

### v0.3 Compatibility Layer

Original v0.3 model types are preserved in `A2A.Compat.V03` namespace under `src/A2A/Compat/V03/`. This includes:
- All v0.3 model classes (`TextPart`, `FilePart`, `DataPart`, `FileContent`, `AgentTransport`, etc.)
- v0.3 JSON-RPC method name constants (`V03Methods`)
- v0.3 JSON converters (kebab-case enums, kind discriminators)
- `V03Adapter` for converting between v0.3 and v1.0 models

**To drop v0.3 support**: delete the `src/A2A/Compat/V03/` folder. No v1.0 code needs modification.

## Installation

### Core A2A Library

```bash
dotnet add package A2A
```

### ASP.NET Core Extensions

```bash
dotnet add package A2A.AspNetCore
```

## Overview
![alt text](https://github.com/a2aproject/a2a-dotnet/raw/main/overview.png)

## Library: A2A
This library contains the core A2A protocol implementation. It includes the following key classes:

### Client Classes
- **`A2AClient`**: Primary client for making A2A requests to agents. Supports both streaming and non-streaming communication, task management, and push notifications.
- **`A2ACardResolver`**: Resolves agent card information from A2A-compatible endpoints to discover agent capabilities and metadata.

### Server Classes  
- **`TaskManager`**: Manages the complete lifecycle of agent tasks including creation, updates, cancellation, and event streaming. Handles both message-based and task-based communication patterns.
- **`ITaskStore`**: An interface for abstracting the storage of tasks.
- **`InMemoryTaskStore`**: Simple in-memory implementation of `ITaskStore` suitable for development and testing scenarios.

### Core Models
- **`AgentTask`**: Represents a task with its status, history, artifacts, and metadata.
- **`AgentCard`**: Contains agent metadata, capabilities, and endpoint information.
- **`AgentMessage`**: Represents messages exchanged between agents and clients.
- **`Part`**: Content container with oneof semantics (`Text`, `Raw`, `Url`, or `Data`).
- **`SendMessageResponse`**: Response from SendMessage — contains either a `Task` or `Message`.
- **`StreamResponse`**: Streaming event wrapper — contains one of `Task`, `Message`, `StatusUpdate`, or `ArtifactUpdate`.

## Library: A2A.AspNetCore
This library provides ASP.NET Core integration for hosting A2A agents. It includes the following key classes:

### Extension Methods
- **`A2ARouteBuilderExtensions`**: Provides `MapA2A()` and `MapHttpA2A()` extension methods for configuring A2A endpoints in ASP.NET Core applications.

## Getting Started

### 1. Create an Agent Server

```csharp
using A2A;
using A2A.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Create and register your agent
var taskManager = new TaskManager();
var agent = new EchoAgent();
agent.Attach(taskManager);

app.MapA2A(taskManager, "/echo");
app.Run();

public class EchoAgent
{
    public void Attach(ITaskManager taskManager)
    {
        taskManager.OnMessageReceived = ProcessMessageAsync;
        taskManager.OnAgentCardQuery = GetAgentCardAsync;
    }

    private Task<SendMessageResponse> ProcessMessageAsync(MessageSendParams messageSendParams, CancellationToken cancellationToken)
    {
        var text = messageSendParams.Message.Parts.First().Text;
        return Task.FromResult(new SendMessageResponse
        {
            Message = new AgentMessage
            {
                Role = MessageRole.Agent,
                MessageId = Guid.NewGuid().ToString(),
                ContextId = messageSendParams.Message.ContextId,
                Parts = [new TextPart { Text = $"Echo: {text}" }]
            }
        });
    }

    private Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AgentCard
        {
            Name = "Echo Agent",
            Description = "Echoes messages back to the user",
            SupportedInterfaces = [new AgentInterface { Url = agentUrl }],
            Version = "1.0.0",
            DefaultInputModes = ["text/plain"],
            DefaultOutputModes = ["text/plain"],
            Capabilities = new AgentCapabilities { Streaming = true }
        });
    }
}
```

### 2. Connect with A2AClient

```csharp
using A2A;

// Discover agent and create client
var cardResolver = new A2ACardResolver(new Uri("http://localhost:5100/"));
var agentCard = await cardResolver.GetAgentCardAsync();
var client = new A2AClient(new Uri(agentCard.SupportedInterfaces[0].Url));

// Send message
var response = await client.SendMessageAsync(new MessageSendParams
{
    Message = new AgentMessage
    {
        Role = MessageRole.User,
        Parts = [new TextPart { Text = "Hello!" }]
    }
});
```

## Samples

The repository includes several sample projects demonstrating different aspects of the A2A protocol implementation. Each sample includes its own README with detailed setup and usage instructions.

### Agent Client Samples
**[`samples/AgentClient/`](samples/AgentClient/README.md)**

Comprehensive collection of client-side samples showing how to interact with A2A agents:
- **Agent Capability Discovery**: Retrieve agent capabilities and metadata using agent cards
- **Message-based Communication**: Direct, stateless messaging with immediate responses
- **Task-based Communication**: Create and manage persistent agent tasks
- **Streaming Communication**: Real-time communication using Server-Sent Events

### Agent Server Samples
**[`samples/AgentServer/`](samples/AgentServer/README.md)**

Server-side examples demonstrating how to build A2A-compatible agents:
- **Echo Agent**: Simple agent that echoes messages back to clients
- **Echo Agent with Tasks**: Task-based version of the echo agent
- **Researcher Agent**: More complex agent with research capabilities
- **HTTP Test Suite**: Complete set of HTTP tests for all agent endpoints

### Semantic Kernel Integration
**[`samples/SemanticKernelAgent/`](samples/SemanticKernelAgent/README.md)**

Advanced sample showing integration with Microsoft Semantic Kernel:
- **Travel Planner Agent**: AI-powered travel planning agent
- **Semantic Kernel Integration**: Demonstrates how to wrap Semantic Kernel functionality in A2A protocol

### Command Line Interface
**[`samples/A2ACli/`](samples/A2ACli/)**

Command-line tool for interacting with A2A agents:
- Direct command-line access to A2A agents
- Useful for testing and automation scenarios

### Quick Start with Client Samples

1. **Clone and build the repository**:
   ```bash
   git clone https://github.com/a2aproject/a2a-dotnet.git
   cd a2a-dotnet
   dotnet build
   ```

2. **Run the client samples**:
   ```bash
   cd samples/AgentClient
   dotnet run
   ```

For detailed instructions and advanced scenarios, see the individual README files linked above.

## Further Reading

To learn more about the A2A protocol, explore these additional resources:

- **[A2A Protocol Documentation](https://a2a-protocol.org/latest/)** - The official documentation for the A2A protocol.
- **[A2A Protocol Specification](https://a2a-protocol.org/latest/specification/)** - The detailed technical specification of the protocol.
- **[A2A Topics](https://a2a-protocol.org/latest/topics/what-is-a2a/)** - An overview of key concepts and features of the A2A protocol.
- **[A2A Roadmap](https://a2a-protocol.org/latest/roadmap/)** - A look at the future development plans and upcoming features.

## Acknowledgements

This library builds upon [Darrel Miller's](https://github.com/darrelmiller) [sharpa2a](https://github.com/darrelmiller/sharpa2a) project. Thanks to Darrel and all the other contributors for the foundational work that helped shape this SDK.

## License

This project is licensed under the [Apache 2.0 License](LICENSE).

