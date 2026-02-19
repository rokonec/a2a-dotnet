# A2A .NET SDK: v0.3 → v1.0 Migration Plan

> **Status: Migration Complete.** This document was the original planning artifact. See README.md for the current migration guide.

## What Was Done

- v1.0 data model: flat oneof types (`SendMessageResponse`, `StreamResponse`, `Part`), no abstract class hierarchies
- SCREAMING_SNAKE_CASE enums, PascalCase JSON-RPC methods
- AgentCard with `supportedInterfaces`, SecurityScheme as oneof
- New operations: ListTasks, GetExtendedAgentCard, push notification CRUD
- A2A-Version header support
- No custom JSON converters on model types — plain STJ with source gen
- v0.3 compat layer in `Compat/V03/` (isolated, deletable)
- TCK validation: 39/81 tests passing (28 failures are TCK bugs with ListTasks method name)

## Original Plan

## Problem Statement

The a2a-dotnet SDK currently implements the A2A protocol v0.3. The protocol has released a v1.0 Release Candidate with significant breaking changes to the data model, serialization format, operation names, and new operations. We need to migrate the SDK to v1.0 while maintaining backward compatibility with v0.3 — and **designing for easy removal of v0.3 support** when the time comes.

## Key Design Constraints

1. **Default to v1.0** — new users get v1.0 behavior out of the box
2. **v0.3 compat is isolated** — all v0.3-specific code lives in a dedicated `Compat/V03/` folder per project, so dropping v0.3 = delete those folders
3. **Dual-server support** — consumers may talk to both v0.3 and v1.0 servers simultaneously during the transition period
4. **Proto-first** — `specification/a2a.proto` is the normative source; v1.0 C# models must match it exactly
5. **v0.3 removal must be trivial** — no v0.3-specific logic in v1.0 code paths; compat layer adapts at the boundary

## Architecture: v0.3 Compat Strategy

### Approach: Isolated Compat Layer with Separate Namespace

Since v0.3 and v1.0 have fundamentally different data models (flat `Part` vs discriminated hierarchy, different enum serialization, different method names, different AgentCard structure), **the v0.3 compat layer preserves the current v0.3 models and converters in a separate folder with a dedicated namespace**.

```
src/A2A/
├── Models/              ← v1.0 models (new, proto-aligned)
├── Client/              ← v1.0 client
├── Server/              ← v1.0 server
├── JsonRpc/             ← v1.0 JSON-RPC methods
├── Compat/
│   └── V03/
│       ├── Models/      ← Preserved v0.3 model types (TextPart, FilePart, AgentTask, etc.)
│       ├── V03Methods.cs       ← v0.3 JSON-RPC method names
│       ├── V03JsonUtilities.cs ← v0.3 serialization config
│       ├── V03Converters.cs    ← v0.3 custom converters (kind discriminator, kebab-case enums)
│       └── V03Adapter.cs       ← Conversion helpers: v0.3 models ↔ v1.0 models

src/A2A.AspNetCore/
├── (v1.0 processor/endpoints)
├── Compat/
│   └── V03/
│       ├── V03JsonRpcProcessor.cs   ← Handles v0.3 method names, delegates to TaskManager
│       └── V03EndpointExtensions.cs ← v0.3 HTTP endpoint mappings (if needed)

tests/
├── A2A.UnitTests/
│   ├── (v1.0 tests)
│   ├── Compat/
│   │   └── V03/       ← v0.3 serialization & adapter tests
```

### How It Works

1. **v1.0 is the primary API** — all new code uses v1.0 models directly
2. **v0.3 models are preserved** in `Compat/V03/Models/` (current `TextPart`, `FilePart`, `DataPart`, `AgentTask`, `AgentMessage`, etc.)
3. **`V03Adapter`** converts between v0.3 and v1.0 models at the boundary:
   - `V03Adapter.ToV1(v03Message) → v1.Message`
   - `V03Adapter.FromV1(v1Task) → v03.AgentTask`
4. **Server v0.3 compat**: The `A2A-Version` header (empty = v0.3 per spec) determines whether v0.3 JSON-RPC method names are accepted and whether responses are serialized in v0.3 format
5. **Client v0.3 compat**: When `AgentCard` has `protocolVersion: "0.3"` (old format), the client auto-detects and uses v0.3 serialization and method names
6. **Dropping v0.3**: Delete all `Compat/V03/` folders across all projects, remove version-detection branches. No v1.0 code needs modification.

## Key Breaking Changes (v0.3 → v1.0)

| Area | v0.3 (Current) | v1.0 (Target) |
|------|----------------|---------------|
| **Part model** | Discriminated hierarchy: `TextPart`, `FilePart`, `DataPart` with `kind` | Flat `Part` with `oneof`: `text`, `raw`, `url`, `data` + shared `metadata`, `filename`, `mediaType` |
| **FileContent** | Separate class with `bytes`/`uri`, `name`, `mimeType` | Eliminated — fields merged into `Part` (`raw`, `url`, `filename`, `mediaType`) |
| **Enum serialization** | kebab-case: `completed`, `input-required` | SCREAMING_SNAKE_CASE: `TASK_STATE_COMPLETED`, `TASK_STATE_INPUT_REQUIRED` |
| **Role** | String `"user"` / `"agent"` | Enum `ROLE_USER` / `ROLE_AGENT` |
| **JSON-RPC methods** | `message/send`, `message/stream`, `tasks/get`, `tasks/cancel`, `tasks/resubscribe` | `SendMessage`, `SendStreamingMessage`, `GetTask`, `CancelTask`, `SubscribeToTask` |
| **AgentCard** | `url` + `preferredTransport` + `additionalInterfaces` | `supportedInterfaces` array of `AgentInterface` (url + protocolBinding + protocolVersion) |
| **AgentCard fields** | `protocolVersion`, `supportsAuthenticatedExtendedCard` | Removed from card root; moved into `AgentInterface.protocolVersion` and `capabilities.extendedAgentCard` |
| **AgentCapabilities** | `stateTransitionHistory` field | Removed (reserved in proto) |
| **Streaming events** | `A2AEvent` discriminated with `kind` | `StreamResponse` with `oneof`: `task`, `message`, `statusUpdate`, `artifactUpdate` |
| **SendMessage response** | Always returns `Task` (`A2AResponse`) | Returns `oneof`: `Task` or `Message` (`SendMessageResponse`) |
| **TaskStatusUpdateEvent** | Has `final` boolean field | Field removed (reserved in proto); terminal state inferred from `TaskState` |
| **PushNotificationConfig** | `authentication` has `schemes` (plural, list) + `credentials` | `authentication` has `scheme` (singular, string) + `credentials` — `AuthenticationInfo` |
| **Push notification ops** | `set`/`get` only | Full CRUD: `Create`, `Get`, `List`, `Delete` |
| **Security** | Flat `securityRequirements` list, `security` on AgentCard | `securitySchemes` map + `securityRequirements` array, `SecurityScheme` is `oneof` |
| **SecurityScheme** | JSON polymorphic with `type` discriminator | Proto `oneof`: `apiKeySecurityScheme`, `httpAuthSecurityScheme`, `oauth2SecurityScheme`, `openIdConnectSecurityScheme`, `mtlsSecurityScheme` |
| **OAuthFlows** | Multiple flows as separate properties | `oneof flow` — exactly one flow per scheme |
| **New operations** | N/A | `ListTasks`, `SubscribeToTask`, `CreateTaskPushNotificationConfig`, `ListTaskPushNotificationConfig`, `DeleteTaskPushNotificationConfig`, `GetExtendedAgentCard` |
| **Versioning** | None | `A2A-Version` header required; `VersionNotSupportedError` |
| **New errors** | Limited error codes | `InvalidAgentResponseError`, `ExtendedAgentCardNotConfiguredError`, `ExtensionSupportRequiredError`, `VersionNotSupportedError` |
| **Tenant support** | None | Optional `tenant` path parameter on all operations |
| **Artifact fields** | No `extensions` | Added `extensions` array |
| **DeviceCodeOAuthFlow** | Not present | New OAuth flow type |

---

## Phase 1: Core v1.0 Data Model (New, Clean)

Build v1.0 models from scratch, proto-aligned. These go in `src/A2A/Models/` replacing the current v0.3 models.

### 1.1 Part Model (Flat, proto-aligned)
- New `Part` class: flat with `oneof`-style nullable properties: `Text`, `Raw` (byte[]), `Url`, `Data` (JsonElement/object)
- Shared properties: `Metadata`, `Filename`, `MediaType`
- Custom `PartConverter` to handle oneof deserialization (exactly one of text/raw/url/data)
- Remove: `PartKind`, `TextPart`, `FilePart`, `DataPart`, `FileContent`, `PartConverterViaKindDiscriminator`, `BaseKindDiscriminatorConverter` (for Part)

### 1.2 Enums (SCREAMING_SNAKE_CASE)
- `TaskState` enum: `TASK_STATE_UNSPECIFIED`, `TASK_STATE_SUBMITTED`, `TASK_STATE_WORKING`, `TASK_STATE_COMPLETED`, `TASK_STATE_FAILED`, `TASK_STATE_CANCELED`, `TASK_STATE_INPUT_REQUIRED`, `TASK_STATE_REJECTED`, `TASK_STATE_AUTH_REQUIRED`
- `Role` enum: `ROLE_UNSPECIFIED`, `ROLE_USER`, `ROLE_AGENT`
- New `ProtoJsonEnumConverter<T>` — serializes enum names as-is (SCREAMING_SNAKE_CASE)
- Remove `KebabCaseLowerJsonStringEnumConverter`

### 1.3 Core Types (proto-aligned names)
- `Task` (replaces `AgentTask`): `Id`, `ContextId`, `Status` (TaskStatus), `Artifacts`, `History` (Message[]), `Metadata`
- `TaskStatus` (replaces `AgentTaskStatus`): `State` (TaskState), `Message`, `Timestamp`
- `Message` (replaces `AgentMessage`): `MessageId`, `ContextId`, `TaskId`, `Role`, `Parts`, `Metadata`, `Extensions`, `ReferenceTaskIds`
- `Artifact`: `ArtifactId`, `Name`, `Description`, `Parts`, `Metadata`, `Extensions`

### 1.4 Streaming & Response Types
- `StreamResponse` (replaces `A2AEvent` hierarchy): oneof `Task`, `Message`, `TaskStatusUpdateEvent`, `TaskArtifactUpdateEvent`
- `SendMessageResponse` (replaces `A2AResponse`): oneof `Task`, `Message`
- `TaskStatusUpdateEvent`: `TaskId`, `ContextId`, `Status`, `Metadata` (no `Final` field)
- `TaskArtifactUpdateEvent`: `TaskId`, `ContextId`, `Artifact`, `Append`, `LastChunk`, `Metadata`
- Remove: `A2AEvent`, `A2AEventKind`, `A2AResponse`, `TaskUpdateEvent` base class

### 1.5 AgentCard (v1.0 structure)
- `AgentCard`: `Name`, `Description`, `SupportedInterfaces`, `Provider`, `Version`, `DocumentationUrl`, `Capabilities`, `SecuritySchemes` (map), `SecurityRequirements`, `DefaultInputModes`, `DefaultOutputModes`, `Skills`, `Signatures`, `IconUrl`
- `AgentInterface`: `Url`, `ProtocolBinding` (string), `Tenant`, `ProtocolVersion`
- `AgentCapabilities`: `Streaming`, `PushNotifications`, `Extensions` (AgentExtension[]), `ExtendedAgentCard`
- Remove: `AgentTransport` struct, `PreferredTransport`, `Url` from card root, `ProtocolVersion` from card root, `SupportsAuthenticatedExtendedCard`, `StateTransitionHistory`

### 1.6 Security Model (proto-aligned)
- `SecurityScheme`: oneof-style with `ApiKeySecurityScheme`, `HttpAuthSecurityScheme`, `OAuth2SecurityScheme`, `OpenIdConnectSecurityScheme`, `MtlsSecurityScheme`
- `SecurityRequirement`: `Schemes` (Dictionary<string, StringList>)
- `OAuthFlows`: oneof with `AuthorizationCode`, `ClientCredentials`, `Implicit` (deprecated), `Password` (deprecated), `DeviceCode`
- Add `DeviceCodeOAuthFlow`: `DeviceAuthorizationUrl`, `TokenUrl`, `RefreshUrl`, `Scopes`
- Add `PkceRequired` to `AuthorizationCodeOAuthFlow`
- `AuthenticationInfo` (replaces `PushNotificationAuthenticationInfo`): `Scheme` (singular string), `Credentials`
- `OAuth2SecurityScheme`: add `OAuth2MetadataUrl`

### 1.7 Request/Response Types (proto-aligned)
- `SendMessageRequest` (replaces `MessageSendParams`): `Tenant`, `Message`, `Configuration`, `Metadata`
- `SendMessageConfiguration`: `AcceptedOutputModes`, `PushNotificationConfig`, `HistoryLength`, `Blocking`
- `GetTaskRequest` (replaces `TaskQueryParams`): `Tenant`, `Id`, `HistoryLength`
- `CancelTaskRequest` (replaces `TaskIdParams`): `Tenant`, `Id`
- New: `ListTasksRequest`, `ListTasksResponse`
- New: `SubscribeToTaskRequest`
- New: `CreateTaskPushNotificationConfigRequest`, `GetTaskPushNotificationConfigRequest`, `ListTaskPushNotificationConfigRequest`, `DeleteTaskPushNotificationConfigRequest`
- New: `ListTaskPushNotificationConfigResponse`
- New: `GetExtendedAgentCardRequest`
- New: `TaskPushNotificationConfig` resource wrapper

---

## Phase 2: v0.3 Compat Layer (Isolated)

Move current v0.3 code into `Compat/V03/` folders. This phase preserves backward compat while keeping it completely separate from v1.0.

### 2.1 Preserve v0.3 Models (`src/A2A/Compat/V03/Models/`)
- Move (copy + adapt) current model classes: `TextPart`, `FilePart`, `DataPart`, `FileContent`, `AgentTask`, `AgentMessage`, `A2AEvent`, `A2AResponse`, etc.
- Keep in namespace `A2A.Compat.V03.Models` (or `A2A.Models.V03`)
- Keep existing JSON serialization behavior (kebab-case enums, kind discriminators)
- These classes do NOT reference v1.0 models — they are self-contained

### 2.2 v0.3 Serialization (`src/A2A/Compat/V03/`)
- `V03JsonUtilities.cs`: v0.3 `JsonSerializerOptions` with kebab-case converters, kind discriminators
- `V03Converters.cs`: Preserved `KebabCaseLowerJsonStringEnumConverter`, `PartConverterViaKindDiscriminator`, `BaseKindDiscriminatorConverter`, etc.
- `V03Methods.cs`: v0.3 JSON-RPC method name constants (`message/send`, `tasks/get`, etc.)

### 2.3 v0.3 ↔ v1.0 Adapter (`src/A2A/Compat/V03/V03Adapter.cs`)
- Static conversion methods between v0.3 and v1.0 models:
  - `ToV1Message(V03.AgentMessage) → Message`
  - `FromV1Task(Task) → V03.AgentTask`
  - `ToV1Part(V03.TextPart|FilePart|DataPart) → Part`
  - `FromV1Part(Part) → V03.Part` (TextPart/FilePart/DataPart)
  - `ToV1AgentCard(V03.AgentCard) → AgentCard`
  - `ToV1TaskState(V03.TaskState) → TaskState`
  - etc.
- Used by client when talking to v0.3 servers and by server when handling v0.3 clients

### 2.4 ASP.NET Core v0.3 Compat (`src/A2A.AspNetCore/Compat/V03/`)
- `V03JsonRpcProcessor.cs`: Accepts v0.3 method names, deserializes v0.3 params, converts to v1.0, delegates to TaskManager, converts response back to v0.3
- Version detection: check `A2A-Version` header (empty = v0.3 per spec)
- Main processor delegates to V03 processor when version is 0.3

---

## Phase 3: JSON-RPC & Error Updates (v1.0)

### 3.1 Method Name Updates
- Update `A2AMethods` class with v1.0 PascalCase names:
  - `SendMessage`, `SendStreamingMessage`, `GetTask`, `ListTasks`, `CancelTask`, `SubscribeToTask`
  - `CreateTaskPushNotificationConfig`, `GetTaskPushNotificationConfig`, `ListTaskPushNotificationConfig`, `DeleteTaskPushNotificationConfig`
  - `GetExtendedAgentCard`
- Update `IsStreamingMethod()` for new names
- Old v0.3 names live in `V03Methods.cs` only

### 3.2 Error Code Updates
- Add new error codes to `A2AErrorCode`:
  - `InvalidAgentResponse` (-32006)
  - `ExtendedAgentCardNotConfigured` (-32007)
  - `ExtensionSupportRequired` (-32008)
  - `VersionNotSupported` (-32009)
- Update `A2AException` / `A2AExceptionExtensions` for new error types
- Add HTTP status mappings (RFC 9457 `application/problem+json` with `type` URIs)

---

## Phase 4: Server-Side Updates (v1.0)

### 4.1 ITaskManager Interface Updates
- Add: `ListTasksAsync()`, `SubscribeToTaskAsync()`, `CreatePushNotificationConfigAsync()`, `ListPushNotificationConfigsAsync()`, `DeletePushNotificationConfigAsync()`, `GetExtendedAgentCardAsync()`
- Update `SendMessageAsync()` to return `SendMessageResponse` (Task or Message)
- Update all method signatures to use v1.0 request/response types

### 4.2 TaskManager & TaskStore Implementation
- Implement new operations in `TaskManager`
- Update `ITaskStore` / `InMemoryTaskStore` for ListTasks (filtering by contextId, status, pagination)
- Update `DistributedCacheTaskStore` for new push notification CRUD
- Handle `SendMessageResponse` returning either `Task` or `Message`

### 4.3 Versioning Support
- Read `A2A-Version` header on every request
- Empty → treat as v0.3, route to V03 compat processor
- `1.0` → v1.0 processor
- Unsupported values → `VersionNotSupportedError`

---

## Phase 5: ASP.NET Core Integration Updates (v1.0)

### 5.1 JSON-RPC Processor Updates
- Update `A2AJsonRpcProcessor` for v1.0 method names and new operations
- Route to `V03JsonRpcProcessor` when `A2A-Version` is empty/0.3
- Update streaming result handling for `StreamResponse` format

### 5.2 HTTP Endpoint Updates
- Update `MapHttpA2A()` for v1.0 URL patterns:
  - `POST /message:send`, `POST /message:stream`
  - `GET /tasks/{id}`, `GET /tasks`, `POST /tasks/{id}:cancel`, `POST /tasks/{id}:subscribe`
  - Full push notification CRUD endpoints
  - `GET /extendedAgentCard`
- Add tenant path parameter support: `/{tenant}/...`
- Update response serialization for `SendMessageResponse` and `StreamResponse`

### 5.3 Well-Known Agent Card
- Return v1.0-format AgentCard (with `supportedInterfaces`)
- Optionally declare both v0.3 and v1.0 interfaces in `supportedInterfaces`

### 5.4 Error Response Updates
- HTTP: RFC 9457 `application/problem+json` with `type` URIs
- JSON-RPC: Updated error codes

---

## Phase 6: Client-Side Updates (v1.0)

### 6.1 IA2AClient Interface
- All methods use v1.0 types
- Add: `ListTasksAsync()`, `SubscribeToTaskAsync()`, push notification CRUD, `GetExtendedAgentCardAsync()`
- `SendMessageAsync()` returns `SendMessageResponse`

### 6.2 A2AClient Implementation
- Send `A2A-Version: 1.0` header with all requests
- Implement all new operations
- Handle `SendMessageResponse` union type
- Update streaming for `StreamResponse` format

### 6.3 Version-Aware Client (v0.3 server support)
- `A2ACardResolver` auto-detects version from AgentCard:
  - v1.0 card: has `supportedInterfaces` → use v1.0 serialization + method names
  - v0.3 card: has `url` + `protocolVersion: "0.3"` at root → use v0.3 serialization + method names
- When talking to v0.3 server:
  - Use v0.3 JSON-RPC method names from `V03Methods`
  - Serialize requests with v0.3 `JsonSerializerOptions` from `V03JsonUtilities`
  - Deserialize responses into v0.3 models, then convert to v1.0 via `V03Adapter`
- This logic lives in `Compat/V03/` — when v0.3 is dropped, client always uses v1.0

---

## Phase 7: Serialization Infrastructure (v1.0)

### 7.1 v1.0 JSON Converters
- `ProtoJsonEnumConverter<T>` — SCREAMING_SNAKE_CASE enum serialization
- `PartConverter` — flat oneof deserialization for `Part`
- `SecuritySchemeConverter` — oneof deserialization
- `OAuthFlowsConverter` — oneof deserialization
- `StreamResponseConverter` — oneof deserialization
- `SendMessageResponseConverter` — oneof deserialization
- Update `A2AJsonUtilities` / `JsonSerializerContext` for all v1.0 types

### 7.2 Source Generator / AOT
- Update `JsonSerializerContext` for v1.0 types
- Separate context for v0.3 types in compat folder

---

## Phase 8: Tests

### 8.1 v1.0 Unit Tests
- All v1.0 model serialization/deserialization tests
- New operation tests
- New error code tests
- `Part` flat model tests
- Enum SCREAMING_SNAKE_CASE tests
- `StreamResponse` / `SendMessageResponse` oneof tests

### 8.2 v0.3 Compat Tests (`tests/.../Compat/V03/`)
- v0.3 model serialization still works
- `V03Adapter` conversion correctness (v0.3 → v1.0 → v0.3 round-trip)
- v0.3 JSON-RPC method name routing
- v0.3 client → v1.0 server interop
- v1.0 client → v0.3 server interop (via adapter)

### 8.3 Integration / ASP.NET Core Tests
- New HTTP endpoint tests
- Version header routing tests
- v0.3 client → v1.0 server via JSON-RPC
- Streaming tests with `StreamResponse`

### 8.4 A2A TCK (Technology Compatibility Kit) Integration
The official A2A TCK at `E:\dev\a2a-tck` (`github.com/a2aproject/a2a-tck`) provides a comprehensive compliance test suite that validates A2A server implementations. We should use it as the final validation gate.

#### TCK Overview
- **Python-based pytest suite** that tests a running SUT (System Under Test) over HTTP
- **4 test tiers**: Mandatory (must pass), Capability (feature-dependent), Quality (production readiness), Feature (informational)
- **Multi-transport**: Tests JSON-RPC, gRPC, and HTTP+JSON/REST transports
- **CLI**: `./run_tck.py --sut-url http://localhost:PORT --category mandatory`

#### TCK Test Categories (what they validate)
| Category | Path | Tests |
|----------|------|-------|
| **Mandatory JSON-RPC** | `tests/mandatory/jsonrpc/` | JSON-RPC 2.0 compliance: malformed JSON, invalid requests, error codes |
| **Mandatory Protocol** | `tests/mandatory/protocol/` | Core A2A methods: SendMessage, GetTask, CancelTask, ListTasks |
| **Mandatory Security** | `tests/mandatory/security/` | TLS, certificates, authentication |
| **Mandatory Quality** | `tests/mandatory/quality/` | Error validation, state transitions |
| **Optional Capabilities** | `tests/optional/capabilities/` | Feature detection for streaming, push notifications |
| **Optional Features** | `tests/optional/features/` | Advanced behaviors |

#### TCK Expected JSON Formats (v1.0)
The TCK expects these exact formats (verified against test fixtures):

**JSON-RPC Method Names** (PascalCase):
```
SendMessage, SendStreamingMessage, GetTask, ListTasks, CancelTask,
SubscribeToTask, GetExtendedAgentCard,
CreateTaskPushNotificationConfig, GetTaskPushNotificationConfig,
ListTaskPushNotificationConfig, DeleteTaskPushNotificationConfig
```

**Part format** (flat oneof, no `kind` discriminator):
```json
{"text": "Hello from TCK!"}
{"data": {"key": "value", "number": 123}}
```

**Enum values** (SCREAMING_SNAKE_CASE):
```
ROLE_USER, ROLE_AGENT
TASK_STATE_SUBMITTED, TASK_STATE_WORKING, TASK_STATE_COMPLETED, etc.
```

**SendMessage request**:
```json
{
  "jsonrpc": "2.0",
  "method": "SendMessage",
  "params": {
    "message": {
      "messageId": "uuid",
      "role": "ROLE_USER",
      "parts": [{"text": "Hello"}]
    }
  },
  "id": "request-id"
}
```

**Response** (oneof task/message):
```json
{
  "jsonrpc": "2.0",
  "result": {
    "task": {
      "id": "task-id",
      "contextId": "context-id",
      "status": {"state": "TASK_STATE_COMPLETED"}
    }
  },
  "id": "request-id"
}
```

**SSE Streaming**:
```
data: {"jsonrpc": "2.0", "result": {...}, "id": "..."}
data: {"jsonrpc": "2.0", "result": {...}, "id": "..."}
data: [DONE]
```

#### TCK Known Issues (Spec Prevails)
⚠️ The TCK was recently updated for v1.0 and has some inconsistencies:
- **gRPC Part conversion** (`tck/transport/grpc_client.py`): Internally uses v0.3 `kind` discriminator format (`{"kind": "text", "text": "..."}`) when converting protobuf responses to JSON — this is a TCK bug, not spec behavior
- **`message_utils.py`**: Uses v0.3 `part.get("kind")` in message conversion layer — also a TCK bug
- **File parts**: TCK uses `{"file": {"name": "...", "mimeType": "...", "fileWithUri": "..."}}` in some places — v1.0 spec uses flat `{"url": "...", "filename": "...", "mediaType": "..."}`
- **`raw` and `url` Part types**: Not tested by TCK yet (only `text`, `file/data` covered)

**Rule**: When TCK behavior conflicts with `specification/a2a.proto` or the spec docs, **always follow the spec**. The proto is the single authoritative normative definition.

#### SUT Configuration for .NET SDK
Create `sut_configs/sut_dotnet_sdk.yaml`:
```yaml
sut_name: dotnet-a2a-sdk
github_repo: https://github.com/a2aproject/a2a-dotnet.git
prerequisites_script: scripts/build_dotnet.ps1
prerequisites_interpreter: powershell.exe
run_script: scripts/run_dotnet.ps1
run_interpreter: powershell.exe
```

#### Integration Plan
1. **Phase 8.1**: Build a minimal TCK-compatible SUT using our v1.0 server (a simple echo agent)
2. **Phase 8.3**: Run `./run_tck.py --sut-url http://localhost:PORT --category mandatory` against it
3. **Phase 9**: Create SUT config + scripts for CI/CD TCK validation
4. **Ongoing**: Track TCK updates and report bugs upstream when TCK conflicts with spec

---

## Phase 9: Samples & Documentation

### 9.1 Sample Updates
- Update all samples (`AgentServer`, `AgentClient`, `A2ACli`, `SemanticKernelAgent`) to v1.0 API
- Show v0.3 compat usage in one sample (or a README note)

### 9.2 TCK SUT Sample
- Create a minimal TCK-compatible A2A server sample that can be used as SUT
- Include build/run scripts for TCK integration
- This doubles as a reference implementation for v1.0

### 9.3 Documentation
- Update README.md
- Add migration guide for users upgrading from v0.3
- Document dual-version support pattern
- Document how to drop v0.3: "Delete all `Compat/V03/` folders"
- Document TCK integration: how to run TCK against your A2A server

---

## Implementation Order

1. **Phase 1** — v1.0 Core Models (foundation)
2. **Phase 7.1** — v1.0 Serialization (converters for new models)
3. **Phase 2** — v0.3 Compat Layer (preserve current code, move to isolation)
4. **Phase 3** — JSON-RPC & Errors
5. **Phase 8.1** — v1.0 Unit Tests (validate models + serialization)
6. **Phase 4** — Server Updates
7. **Phase 5** — ASP.NET Core Updates
8. **Phase 6** — Client Updates
9. **Phase 8.2-8.3** — Compat + Integration Tests
10. **Phase 8.4** — TCK Validation (run TCK mandatory suite against our server)
11. **Phase 9** — Samples, TCK SUT & Docs

## Dropping v0.3 (Future)

When v0.3 is no longer needed:
1. Delete `src/A2A/Compat/V03/` folder
2. Delete `src/A2A.AspNetCore/Compat/V03/` folder
3. Delete `tests/*/Compat/V03/` folders
4. Remove version-detection branches in `A2AJsonRpcProcessor` and `A2AClient` (simple `if` guards)
5. Remove `V03` references from sample code
6. **No v1.0 code needs modification** — it is fully self-contained

## Reference Repositories (Local)

| Repo | Local Path | Remote | Purpose |
|------|-----------|--------|---------|
| **A2A Spec** | `E:\dev\A2A` | `github.com/a2aproject/A2A` | Normative spec & proto (bleeding edge `main`) |
| **A2A .NET SDK** | `E:\dev\a2a-dotnet` | `github.com/a2aproject/a2a-dotnet` | This SDK (our codebase) |
| **A2A TCK** | `E:\dev\a2a-tck` | `github.com/a2aproject/a2a-tck` | Compliance test suite |

### Authoritative Sources (Priority Order)
1. **`E:\dev\A2A\specification\a2a.proto`** — The single normative definition of all protocol data objects and messages. Always prevails.
2. **`https://a2a-protocol.org/latest/specification/`** — Human-readable spec docs (derived from proto + markdown)
3. **`E:\dev\a2a-tck`** — Compliance tests. Useful for validation but may have bugs (recently updated to v1.0). When TCK conflicts with proto, **follow the proto**.

### Proto File Reference
- Local path: `E:\dev\A2A\specification\a2a.proto`
- Remote: `github.com/a2aproject/A2A/blob/main/specification/a2a.proto`
- C# namespace declared in proto: `A2a.V1` (`option csharp_namespace = "A2a.V1"`)
- JSON schema: build artifact only (not committed), generated from proto via `protoc-gen-jsonschema`

## Notes

- **`E:\dev\A2A\specification\a2a.proto`** is the normative source — always prevails over TCK and spec docs
- All JSON field names use **camelCase** (not snake_case from proto)
- Enum values use **SCREAMING_SNAKE_CASE** per ProtoJSON spec
- `Part` no longer has a `kind` discriminator — it's a flat message with `oneof content`
- `StreamResponse` replaces `A2AEvent` hierarchy
- `SendMessageResponse` replaces `A2AResponse` hierarchy
- `AgentTransport` struct is removed — `protocolBinding` is just a string
- `DeviceCodeOAuthFlow` is new in v1.0
- v0.3 compat layer is 100% isolated — no `#if`, no interleaving, just folders to delete
- TCK at `E:\dev\a2a-tck` is the official compliance test suite
- TCK has known v0.3 artifacts in gRPC/message_utils layers — these are bugs, not spec requirements
- Proto declares `option csharp_namespace = "A2a.V1"` — consider whether to use this namespace or keep `A2A` namespace
