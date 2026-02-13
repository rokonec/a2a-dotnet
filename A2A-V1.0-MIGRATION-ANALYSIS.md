# A2A Protocol Migration Analysis: v0.3.0 → RC v1.0

> **Analysis Date:** February 11, 2026 (revised)  
> **Current Codebase Version:** v0.3.0  
> **Target Specification:** [A2A Protocol RC v1.0](https://a2a-protocol.org/latest/specification/)  
> **Normative Source:** `specification/a2a.proto` (Protocol Buffers) — all JSON schemas and SDK bindings are derived artifacts

## Summary

The latest A2A Protocol specification (Release Candidate v1.0) introduces **major breaking changes** compared to v0.3.0. The protocol now defines a 3-layer architecture:

1. **Layer 1: Canonical Data Model** — Protocol Buffer messages (normative)
2. **Layer 2: Abstract Operations** — Binding-independent operation definitions
3. **Layer 3: Protocol Bindings** — JSON-RPC, gRPC, and HTTP+JSON/REST

The C# SDK currently only implements JSON-RPC (Layer 3). The v1.0 spec adds HTTP+JSON/REST and gRPC as first-class bindings with their own URL patterns, error formats, and streaming mechanisms.

This document outlines all identified changes, their impact on the .NET SDK, and estimated effort for migration.

---

## Key Breaking Changes

### 1. Kind Discriminator Removed (HIGH IMPACT)

The `kind` field discriminator pattern used throughout the current codebase is **completely removed** in v1.0. Objects now use **JSON member names as discriminators** (protobuf `oneof` semantics), where the field name itself identifies the type.

See [Appendix A.2.1](https://a2a-protocol.org/latest/specification/#a21-breaking-change-kind-discriminator-removed) for official migration guidance.

**Part (§4.1.6) — field presence distinguishes type:**

| v0.3.0 Pattern | v1.0 Pattern |
|----------------|--------------|
| `{ "kind": "text", "text": "..." }` | `{ "text": "Hello" }` |
| `{ "kind": "file", "file": { "bytes": "..." } }` | `{ "raw": "base64...", "mediaType": "image/png" }` |
| `{ "kind": "file", "file": { "uri": "..." } }` | `{ "url": "https://...", "mediaType": "image/png" }` |
| `{ "kind": "data", "data": {...} }` | `{ "data": {...} }` |

A `Part` MUST contain **exactly one** of: `text`, `raw`, `url`, `data`. Shared optional fields: `metadata` (object), `filename` (string), `mediaType` (string).

**SendMessageResponse (§10.4.1) — response union:**

| v0.3.0 Pattern | v1.0 Pattern |
|----------------|--------------|
| `{ "kind": "task", "id": "..." }` | `{ "task": { "id": "..." } }` |
| `{ "kind": "message", "role": "..." }` | `{ "message": { "role": "..." } }` |

**StreamResponse (§3.2.3) — streaming event wrapper:**

| v0.3.0 Pattern | v1.0 Pattern |
|----------------|--------------|
| `{ "kind": "task", ... }` | `{ "task": {...} }` |
| `{ "kind": "message", ... }` | `{ "message": {...} }` |
| `{ "kind": "status-update", ... }` | `{ "statusUpdate": {...} }` |
| `{ "kind": "artifact-update", ... }` | `{ "artifactUpdate": {...} }` |

A `StreamResponse` MUST contain **exactly one** of: `task`, `message`, `statusUpdate`, `artifactUpdate`.

**SecurityScheme (§4.5.1) — field-name discriminator:**

| v0.3.0 Pattern | v1.0 Pattern |
|----------------|--------------|
| `{ "type": "apiKey", ... }` | `{ "apiKeySecurityScheme": {...} }` |
| `{ "type": "http", ... }` | `{ "httpAuthSecurityScheme": {...} }` |
| `{ "type": "oauth2", ... }` | `{ "oauth2SecurityScheme": {...} }` |
| `{ "type": "openIdConnect", ... }` | `{ "openIdConnectSecurityScheme": {...} }` |
| N/A | `{ "mtlsSecurityScheme": {...} }` |

A `SecurityScheme` MUST contain **exactly one** of the above.

**OAuthFlows (§4.5.7) — also a oneof (was multi-field object):**

| v0.3.0 Pattern | v1.0 Pattern |
|----------------|--------------|
| Multiple flows in one object | `{ "authorizationCode": {...} }` (exactly one) |
| | `{ "clientCredentials": {...} }` |
| | `{ "implicit": {...} }` |
| | `{ "password": {...} }` |
| N/A | `{ "deviceCode": {...} }` |

**Push Notification Payload (§4.3.3):**

| v0.3.0 Pattern | v1.0 Pattern |
|----------------|--------------|
| Kind-based events | `{ "statusUpdate": {...} }` or `{ "artifactUpdate": {...} }` |

**Impact on codebase:**
- `BaseKindDiscriminatorConverter` needs to be replaced with **field-presence-based converters**
- `A2AEventKind` static class to be removed
- `PartKind` static class to be removed
- `FileContentKind` and `FileContent` hierarchy to be removed (no more FileWithBytes/FileWithUri)
- `A2AEvent` and `A2AResponse` base classes need refactoring or removal
- All Part types (TextPart, FilePart, DataPart) replaced by single flat `Part` type
- `SecurityScheme` converter replaced with field-presence-based converter
- `OAuthFlows` becomes a oneof (was multi-field object)
- New `StreamResponse` wrapper type required

### 2. Enum Naming Convention Changed (HIGH IMPACT)

Enums now use `SCREAMING_SNAKE_CASE` format per ProtoJSON specification.

| v0.3.0 | v1.0 |
|--------|------|
| `submitted` | `TASK_STATE_SUBMITTED` |
| `working` | `TASK_STATE_WORKING` |
| `input-required` | `TASK_STATE_INPUT_REQUIRED` |
| `completed` | `TASK_STATE_COMPLETED` |
| `canceled` | `TASK_STATE_CANCELED` |
| `failed` | `TASK_STATE_FAILED` |
| `rejected` | `TASK_STATE_REJECTED` |
| `auth-required` | `TASK_STATE_AUTH_REQUIRED` |
| `unknown` | `TASK_STATE_UNSPECIFIED` |
| `user` | `ROLE_USER` |
| `agent` | `ROLE_AGENT` |

**C# Implementation**: Use explicit `switch`-based `JsonConverter<TEnum>` converters (e.g., `TaskStateJsonConverter`, `MessageRoleJsonConverter`) with hardcoded string mappings. This is the only approach that satisfies all constraints: AOT compatibility, source-gen support, and cross-TFM (net9.0/net8.0/netstandard2.0). No built-in STJ mechanism can produce the type-prefixed `TASK_STATE_` / `ROLE_` format across all TFMs. See [docs/enum-screaming-snake-implementation.md](docs/enum-screaming-snake-implementation.md) for full analysis and implementation.

### 3. AgentCard Structure Changes (HIGH IMPACT)

The AgentCard (§4.4.1) is significantly restructured. Protocol version is now **per-interface**, not per-card.

| v0.3.0 Field | v1.0 Field | Notes |
|--------------|------------|-------|
| `url` | **Removed** | Use `supportedInterfaces[0].url` |
| `preferredTransport` | **Removed** | First entry in `supportedInterfaces` is preferred |
| `additionalInterfaces` | `supportedInterfaces` (required) | Renamed; ordered array of `AgentInterface` |
| `protocolVersion` (string) | **Removed** from card | Version is now per-interface: `AgentInterface.protocolVersion` |
| `supportsAuthenticatedExtendedCard` | `capabilities.extendedAgentCard` | Moved to capabilities (see [A.2.2](https://a2a-protocol.org/latest/specification/#a22-breaking-change-extended-agent-card-field-relocated)) |
| N/A | `securitySchemes` (map<string, SecurityScheme>) | Security scheme definitions |
| N/A | `securityRequirements` (SecurityRequirement[]) | Required security for contacting the agent |
| N/A | `signatures` (AgentCardSignature[]) | JWS signatures per RFC 7515 |
| N/A | `iconUrl` (string) | Optional agent icon |
| `inputModes` | `defaultInputModes` (required) | Renamed |
| `outputModes` | `defaultOutputModes` (required) | Renamed |

### 4. AgentInterface Changes (§4.4.6)

| v0.3.0 | v1.0 | Notes |
|--------|------|-------|
| `transport` | `protocolBinding` (required) | Open string: `"JSONRPC"`, `"GRPC"`, `"HTTP+JSON"` |
| `url` | `url` (required) | Absolute HTTPS URL |
| N/A | `protocolVersion` (required) | Per-interface A2A version e.g. `"1.0"` |
| N/A | `tenant` (optional) | Tenant to set in requests |

New type: **AgentCardSignature** (§4.4.7) — JWS signature with `protected` (required), `signature` (required), `header` (optional).

### 5. New and Changed Operations

**New operations:**

| Operation | JSON-RPC | HTTP+JSON/REST | Description |
|-----------|----------|----------------|-------------|
| ListTasks | `ListTasks` | `GET /tasks` | List tasks with filtering (contextId, status, statusTimestampAfter), pagination (pageToken/pageSize), cursor-based |
| SubscribeToTask | `SubscribeToTask` | `POST /tasks/{id}:subscribe` | Subscribe to updates on existing task (SSE stream) |
| GetExtendedAgentCard | `GetExtendedAgentCard` | `GET /extendedAgentCard` | Fetch authenticated extended agent card |

**Renamed operations (§5.3 method mapping):**

| v0.3.0 | v1.0 JSON-RPC | v1.0 REST |
|--------|---------------|-----------|
| `message/send` | `SendMessage` | `POST /message:send` |
| `message/stream` | `SendStreamingMessage` | `POST /message:stream` |
| `tasks/get` | `GetTask` | `GET /tasks/{id}` |
| `tasks/cancel` | `CancelTask` | `POST /tasks/{id}:cancel` |
| `tasks/resubscribe` | `SubscribeToTask` | `POST /tasks/{id}:subscribe` |
| `tasks/pushNotificationConfig/set` | `CreateTaskPushNotificationConfig` | `POST /tasks/{id}/pushNotificationConfigs` |
| `tasks/pushNotificationConfig/get` | `GetTaskPushNotificationConfig` | `GET /tasks/{id}/pushNotificationConfigs/{configId}` |
| N/A | `ListTaskPushNotificationConfig` | `GET /tasks/{id}/pushNotificationConfigs` |
| N/A | `DeleteTaskPushNotificationConfig` | `DELETE /tasks/{id}/pushNotificationConfigs/{configId}` |

**ListTasksRequest parameters:**
- `contextId` — filter by context
- `status` — filter by TaskState
- `pageSize` — 1-100, default 50
- `pageToken` — cursor for pagination
- `historyLength` — messages per task
- `statusTimestampAfter` — ISO 8601 timestamp filter
- `includeArtifacts` — boolean, default false
- `tenant` — optional path parameter

**ListTasksResponse fields:** `tasks`, `nextPageToken` (empty string = last page), `pageSize`, `totalSize`

### 6. Error Handling Overhaul

**New A2A-specific error codes (§5.4):**

| JSON-RPC Code | Name | gRPC Status | HTTP Status | URI |
|---------------|------|-------------|-------------|-----|
| -32001 | `TaskNotFoundError` | NOT_FOUND | 404 | `https://a2a-protocol.org/errors/task-not-found` |
| -32002 | `TaskNotCancelableError` | FAILED_PRECONDITION | 409 | `https://a2a-protocol.org/errors/task-not-cancelable` |
| -32003 | `PushNotificationNotSupportedError` | UNIMPLEMENTED | 400 | `https://a2a-protocol.org/errors/push-notification-not-supported` |
| -32004 | `UnsupportedOperationError` | UNIMPLEMENTED | 400 | `https://a2a-protocol.org/errors/unsupported-operation` |
| -32005 | `ContentTypeNotSupportedError` | INVALID_ARGUMENT | 415 | `https://a2a-protocol.org/errors/content-type-not-supported` |
| -32006 | `InvalidAgentResponseError` | INTERNAL | 502 | `https://a2a-protocol.org/errors/invalid-agent-response` |
| -32007 | `ExtendedAgentCardNotConfiguredError` | FAILED_PRECONDITION | 400 | `https://a2a-protocol.org/errors/extended-agent-card-not-configured` |
| -32008 | `ExtensionSupportRequiredError` | FAILED_PRECONDITION | 400 | `https://a2a-protocol.org/errors/extension-support-required` |
| -32009 | `VersionNotSupportedError` | UNIMPLEMENTED | 400 | `https://a2a-protocol.org/errors/version-not-supported` |

**Error format differs per binding:**
- **JSON-RPC:** Standard JSON-RPC 2.0 error object (`code`, `message`, `data`)
- **gRPC:** `google.rpc.Status` with `google.rpc.ErrorInfo` in details (reason = `TASK_NOT_FOUND`, domain = `a2a-protocol.org`)
- **HTTP+JSON/REST:** RFC 9457 Problem Details (`type` URI, `title`, `status`, `detail`, extension fields)

### 7. Message Object Changes (§4.1.4) (MEDIUM IMPACT)

| v0.3.0 Field | v1.0 Field | Notes |
|--------------|------------|-------|
| N/A | `messageId` (required) | UUID, created by message creator |
| N/A | `contextId` (optional) | Associates message with a context |
| N/A | `taskId` (optional) | Associates message with a task |
| `role` | `role` (required) | Now `ROLE_USER` / `ROLE_AGENT` (SCREAMING_SNAKE_CASE) |
| `parts` | `parts` (required) | Array of Part (new flat format) |
| `metadata` | `metadata` (optional) | Unchanged — `object` (open JSON) |
| N/A | `extensions` (optional) | Array of extension URIs present in this message |
| N/A | `referenceTaskIds` (optional) | Task IDs this message references for context |

### 8. Task Object Changes (§4.1.1)

| v0.3.0 Field | v1.0 Field | Notes |
|--------------|------------|-------|
| `id` | `id` (required) | Unchanged |
| N/A | `contextId` (required) | Now required on all tasks |
| `status` | `status` (required) | TaskStatus object |
| `artifacts` | `artifacts` (optional) | Array of Artifact |
| `history` | `history` (optional) | Array of Message |
| `metadata` | `metadata` (optional) | Open JSON object |
| `kind` | **Removed** | No discriminator |

### 9. Streaming Event Changes

**TaskStatusUpdateEvent (§4.2.1):**

| v0.3.0 | v1.0 |
|--------|------|
| `taskId` | `taskId` (required) |
| N/A | `contextId` (required) — **new** |
| `status` | `status` (required) |
| N/A | `metadata` (optional) |
| `kind` | **Removed** |

**TaskArtifactUpdateEvent (§4.2.2):**

| v0.3.0 | v1.0 |
|--------|------|
| `taskId` | `taskId` (required) |
| N/A | `contextId` (required) — **new** |
| `artifact` | `artifact` (required) |
| N/A | `append` (optional) — append to previous artifact with same ID |
| N/A | `lastChunk` (optional) — final chunk indicator |
| N/A | `metadata` (optional) |
| `kind` | **Removed** |

### 10. PushNotification Changes

**AuthenticationInfo (§4.3.2) simplified:**

| v0.3.0 | v1.0 |
|--------|------|
| `schemes` (string[]) | `scheme` (string) — single HTTP auth scheme |
| N/A | `credentials` (optional string) — format depends on scheme |

**PushNotificationConfig (§4.3.1):**

| v0.3.0 | v1.0 | Notes |
|--------|------|-------|
| N/A | `id` (optional) | UUID for this notification config |
| `url` | `url` (required) | Unchanged |
| `token` | `token` (optional) | Unchanged |
| `authentication` | `authentication` (optional) | Now uses simplified AuthenticationInfo |

**Push notifications now support full CRUD** — Create, Get, List, Delete per task.

### 11. Service Parameters (§3.2.6)

A new cross-cutting concern: key-value parameters transmitted via protocol-specific mechanisms (HTTP headers, gRPC metadata).

| Header | Description | Notes |
|--------|-------------|-------|
| `A2A-Version` | Protocol version (`Major.Minor` format, e.g. `"1.0"`) | Empty = `0.3` |
| `A2A-Extensions` | Comma-separated extension URIs | Client opt-in to extensions |

All A2A service parameters are prefixed with `a2a-`.

### 12. New Security Scheme Types

- `MutualTlsSecurityScheme` (§4.5.6) — mTLS authentication (just `description` field)
- `DeviceCodeOAuthFlow` (§4.5.10) — OAuth Device Code flow (RFC 8628): `deviceAuthorizationUrl`, `tokenUrl`, `refreshUrl`, `scopes`
- `oauth2MetadataUrl` field added to `OAuth2SecurityScheme` (RFC 8414)
- Note: `OAuthFlows` is now a **oneof** — exactly one flow type per object (see section 1)

### 13. Content Type & Protocol Bindings

**New IANA media type:** `application/a2a+json` registered for HTTP+JSON/REST binding.

**Three first-class protocol bindings:**

| Binding | Method Names | Streaming | Error Format |
|---------|-------------|-----------|---------------|
| JSON-RPC (§9) | PascalCase (`SendMessage`) | SSE (`text/event-stream`) | JSON-RPC error codes |
| gRPC (§10) | Proto `A2AService` | Server streaming | `google.rpc.Status` + `ErrorInfo` |
| HTTP+JSON/REST (§11) | RESTful URLs (`POST /message:send`) | SSE | RFC 9457 Problem Details |

**JSON-RPC method names changed to PascalCase:**

| v0.3.0 | v1.0 |
|--------|------|
| `message/send` | `SendMessage` |
| `message/stream` | `SendStreamingMessage` |
| `tasks/get` | `GetTask` |
| `tasks/cancel` | `CancelTask` |
| `tasks/resubscribe` | `SubscribeToTask` |

### 14. SendMessageConfiguration Changes (§3.2.2)

| Field | Type | Notes |
|-------|------|-------|
| `acceptedOutputModes` | string[] | Media types client accepts |
| `pushNotificationConfig` | PushNotificationConfig | For async updates |
| `historyLength` | integer | Max messages in response history |
| `blocking` | boolean | **New** — wait until terminal/interrupted state |

### 15. Versioning (§3.6)

- Version format: `Major.Minor` (patch versions not used in negotiation)
- Default version when `A2A-Version` is empty: `0.3`
- Agent MUST return `VersionNotSupportedError` for unsupported versions
- Agents CAN expose multiple interfaces for different versions

### 16. Field Naming & Optionality (§5.5, §5.7)

- **JSON fields:** camelCase (proto `snake_case` → JSON `camelCase`)
- **Enum values:** SCREAMING_SNAKE_CASE per ProtoJSON spec
- **Required fields:** Indicated by `google.api.field_behavior = REQUIRED` annotation
- **Unrecognized fields:** Implementations SHOULD ignore (forward compatibility)

---

## Action List & Time Estimates

### Phase 1: Core Model Changes (8-10 man-days)

| # | Task | Est. | Priority |
|---|------|------|----------|
| 1.1 | Remove `BaseKindDiscriminatorConverter`, `A2AEventKind`, `PartKind`, `FileContentKind` | 0.5 days | Critical |
| 1.2 | Replace `Part` type hierarchy (TextPart/FilePart/DataPart) with single flat `Part` using field-presence converter | 2 days | Critical |
| 1.3 | Remove `FileContent`/`FileWithBytes`/`FileWithUri` hierarchy — file content is now flat on Part (`raw`/`url`) | 0.5 days | Critical |
| 1.4 | Refactor `A2AEvent`/`A2AResponse` base classes — remove `kind` property, restructure hierarchy | 1 day | Critical |
| 1.5 | Implement `SendMessageResponse` as oneof wrapper (`task` \| `message`) | 0.5 days | Critical |
| 1.6 | Implement `StreamResponse` wrapper type (`task` \| `message` \| `statusUpdate` \| `artifactUpdate`) | 1 day | Critical |
| 1.7 | Update `TaskState` enum values to SCREAMING_SNAKE_CASE | 0.5 days | Critical |
| 1.8 | Update `MessageRole` enum to SCREAMING_SNAKE_CASE (`ROLE_USER`/`ROLE_AGENT`/`ROLE_UNSPECIFIED`) | 0.5 days | Critical |
| 1.9 | Add `messageId`, `contextId`, `taskId`, `extensions`, `referenceTaskIds` to Message | 0.5 days | Critical |
| 1.10 | Add `contextId` (required) to Task; remove `kind` | 0.5 days | Critical |
| 1.11 | Update `TaskStatusUpdateEvent`/`TaskArtifactUpdateEvent` — add `contextId`, `metadata`, `append`, `lastChunk`; remove `kind` | 0.5 days | Critical |
| 1.12 | Update JSON-RPC method names from slash-case to PascalCase | 0.5 days | Critical |

### Phase 2: AgentCard Restructure (3-4 man-days)

| # | Task | Est. | Priority |
|---|------|------|----------|
| 2.1 | Replace `Url`+`PreferredTransport`+`AdditionalInterfaces` → `SupportedInterfaces` (required array) | 1 day | Critical |
| 2.2 | Remove card-level `ProtocolVersion`; version is now per-interface on `AgentInterface.ProtocolVersion` | 0.5 days | Critical |
| 2.3 | Move `SupportsAuthenticatedExtendedCard` → `Capabilities.ExtendedAgentCard` | 0.5 days | Critical |
| 2.4 | Update `AgentInterface`: `transport` → `protocolBinding`, add `protocolVersion` (required), `tenant` (optional) | 0.5 days | High |
| 2.5 | Add `securitySchemes` (map<string, SecurityScheme>) and `securityRequirements` (SecurityRequirement[]) | 0.5 days | High |
| 2.6 | Rename `inputModes`/`outputModes` → `defaultInputModes`/`defaultOutputModes` | 0.25 days | High |
| 2.7 | Add `AgentCardSignature` type and `signatures` field | 0.5 days | Medium |
| 2.8 | Add `iconUrl` field | 0.1 days | Low |

### Phase 3: New Operations & Parameters (5-6 man-days)

| # | Task | Est. | Priority |
|---|------|------|----------|
| 3.1 | Implement `ListTasks` operation with full filtering & cursor-based pagination | 2 days | High |
| 3.2 | Implement `SubscribeToTask` (SSE stream for existing task) | 1 day | High |
| 3.3 | Implement push notification CRUD (Create, Get, List, Delete) | 1 day | High |
| 3.4 | Add `tenant` parameter support across all operations | 0.5 days | Medium |
| 3.5 | Add `A2A-Version` service parameter support (version negotiation) | 0.5 days | Medium |
| 3.6 | Add `A2A-Extensions` service parameter support | 0.5 days | Medium |
| 3.7 | Add `blocking` field to `SendMessageConfiguration` | 0.25 days | Medium |
| 3.8 | Implement `GetExtendedAgentCard` operation | 0.5 days | Medium |

### Phase 4: Error Handling Updates (1-2 man-days)

| # | Task | Est. | Priority |
|---|------|------|----------|
| 4.1 | Add new error codes: -32005 through -32009 | 0.5 days | High |
| 4.2 | Add error URI constants (e.g. `https://a2a-protocol.org/errors/task-not-found`) | 0.25 days | High |
| 4.3 | Implement RFC 9457 Problem Details error format for HTTP+JSON/REST binding | 0.5 days | Medium |
| 4.4 | Add gRPC `google.rpc.ErrorInfo` mapping (`reason`, `domain`, `metadata`) | 0.5 days | Medium |

### Phase 5: SecurityScheme & PushNotification Refactoring (2-3 man-days)

| # | Task | Est. | Priority |
|---|------|------|----------|
| 5.1 | Refactor `SecurityScheme` to field-name-based oneof converter (5 scheme types) | 1 day | High |
| 5.2 | Refactor `OAuthFlows` from multi-field to oneof (5 flow types) | 0.5 days | High |
| 5.3 | Simplify `AuthenticationInfo` — `schemes[]` → `scheme` + `credentials` | 0.25 days | High |
| 5.4 | Add `MutualTlsSecurityScheme` type | 0.25 days | Medium |
| 5.5 | Add `DeviceCodeOAuthFlow` type | 0.25 days | Medium |
| 5.6 | Add `oauth2MetadataUrl` to `OAuth2SecurityScheme` | 0.1 days | Medium |
| 5.7 | Add `PushNotificationConfig.id` field and CRUD support types | 0.25 days | Medium |

### Phase 6: Testing & Documentation (3-4 man-days)

| # | Task | Est. | Priority |
|---|------|------|----------|
| 6.1 | Update all unit tests for breaking changes (kind removal, enum renames) | 2 days | Critical |
| 6.2 | Add serialization round-trip tests for all new oneof types | 1 day | Critical |
| 6.3 | Update sample applications | 0.5 days | High |
| 6.4 | Update README and documentation | 0.5 days | High |

### Phase 7: Server/Client Implementation Updates (3-4 man-days)

| # | Task | Est. | Priority |
|---|------|------|----------|
| 7.1 | Update A2A.AspNetCore routing (PascalCase method names, new operations) | 1 day | High |
| 7.2 | Update client implementation for new patterns (SendMessageResponse union, StreamResponse wrapper) | 1 day | High |
| 7.3 | Add version negotiation support (A2A-Version header) | 0.5 days | Medium |
| 7.4 | Add HTTP+JSON/REST binding support (URL patterns, error format) | 1 day | Medium |

---

## Total Estimated Effort

| Phase | Estimated Days |
|-------|----------------|
| Phase 1: Core Model Changes | 8-10 |
| Phase 2: AgentCard Restructure | 3-4 |
| Phase 3: New Operations | 5-6 |
| Phase 4: Error Handling | 1-2 |
| Phase 5: Security/PushNotification | 2-3 |
| Phase 6: Testing & Docs | 3-4 |
| Phase 7: Server/Client Updates | 3-4 |
| **Total** | **25-33 man-days** |

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Breaking change to discriminator pattern affects all serialization | High | Certain | Phased migration with feature flags |
| Enum value changes break existing integrations | High | High | Provide backward compatibility aliases |
| AgentCard field removals break existing consumers | Medium | High | Document migration path clearly |
| New ListTasks operation requires significant new code | Medium | Certain | Plan for complete implementation |
| SecurityScheme/OAuthFlows oneof change breaks AgentCard parsing | High | Certain | Implement and test converters early |
| Proto as normative source may evolve before final v1.0 release | Medium | Medium | Track proto changes; regenerate from proto when stable |
| Three protocol bindings increase surface area significantly | High | Certain | Implement JSON-RPC first, add REST/gRPC incrementally |

---

## Files Requiring Changes

### Core Models (`src/A2A/Models/`)
- `A2AEventKind.cs` - **DELETE**
- `PartKind.cs` - **DELETE**
- `FileContentKind.cs` - **DELETE**
- `FileContent.cs` - **DELETE** (file content is now flat fields on Part)
- `FileWithBytes.cs` - **DELETE**
- `FileWithUri.cs` - **DELETE**
- `A2AResponse.cs` - Major refactor (remove kind, add SendMessageResponse union)
- `A2AEvent.cs` - Major refactor (remove kind hierarchy)
- `AgentTask.cs` - Remove kind property; add `contextId` (required)
- `AgentMessage.cs` - Remove kind property; add `messageId` (required), `contextId`, `taskId`, `extensions`, `referenceTaskIds`
- `TaskState.cs` - Update enum values to SCREAMING_SNAKE_CASE
- `MessageRole.cs` - Update to SCREAMING_SNAKE_CASE; add `ROLE_UNSPECIFIED`
- `Part.cs` - **Major refactor** — single flat type with `text`/`raw`/`url`/`data` + `metadata`/`filename`/`mediaType`
- `TextPart.cs` - **DELETE** (absorbed into Part)
- `FilePart.cs` - **DELETE** (absorbed into Part)
- `DataPart.cs` - **DELETE** (absorbed into Part)
- `AgentCard.cs` - Major refactor (supportedInterfaces, remove url, securitySchemes map, signatures)
- `AgentInterface.cs` - Update: `transport` → `protocolBinding`, add `protocolVersion`, `tenant`
- `AgentCapabilities.cs` - Add `ExtendedAgentCard`
- `TaskStatusUpdateEvent.cs` - Add `contextId`, `metadata`; remove `kind`
- `TaskArtifactUpdateEvent.cs` - Add `contextId`, `append`, `lastChunk`, `metadata`; remove `kind`
- `PushNotificationAuthenticationInfo.cs` - Simplify to `scheme` + `credentials`
- `PushNotificationConfig.cs` - Add `id` field
- `SecurityScheme.cs` - **Major refactor** — field-name-based oneof (5 scheme types)
- `OAuthFlows.cs` - **Major refactor** — oneof (5 flow types, was multi-field)
- `OAuth2SecurityScheme.cs` - Add `oauth2MetadataUrl`
- `Artifact.cs` - Add `extensions` field
- `AgentSkill.cs` - Add `securityRequirements` field
- `SendMessageConfiguration.cs` - Add `blocking` field

### New Models to Create
- `StreamResponse.cs` — oneof wrapper for streaming events
- `SendMessageResponse.cs` — oneof for task/message result
- `ListTasksRequest.cs` — with all filter/pagination params
- `ListTasksResponse.cs` — with tasks array, pagination fields
- `MutualTlsSecurityScheme.cs`
- `DeviceCodeOAuthFlow.cs`
- `AgentCardSignature.cs`
- `AgentProvider.cs` (if not existing)
- `SubscribeToTaskRequest.cs`

### Converters (`src/A2A/`)
- `BaseKindDiscriminatorConverter.cs` - **DELETE**
- `A2AJsonConverter.cs` - Major refactor (no kind delegation)
- `A2AJsonUtilities.cs` - Update source-gen context for all new/changed types
- Create `PartConverter.cs` — field-presence-based oneof
- Create `StreamResponseConverter.cs` — field-presence-based oneof
- Create `SendMessageResponseConverter.cs` — field-presence-based oneof
- Create `SecuritySchemeConverter.cs` — field-name-based oneof
- Create `OAuthFlowsConverter.cs` — field-name-based oneof
- `KebabCaseLowerJsonStringEnumConverter.cs` - **DELETE**; replace with `TaskStateJsonConverter.cs` and `MessageRoleJsonConverter.cs` (explicit `switch`-based converters for AOT/source-gen/multi-TFM compatibility — see [docs/enum-screaming-snake-implementation.md](docs/enum-screaming-snake-implementation.md))

### Error Codes
- `A2AErrorCode.cs` - Add codes -32005 through -32009; add error URI constants

### Server/Client
- `A2A.AspNetCore` — Update routing for PascalCase method names; add new operation endpoints
- `A2AClient` — Update for SendMessageResponse union; StreamResponse wrapper
- Add HTTP+JSON/REST endpoint definitions (future phase)

---

## Recommended Migration Approach

1. **Feature branch strategy**: Create a `v1.0-migration` branch
2. **Incremental migration**: Start with model changes (Phase 1), validate with tests
3. **Backward compatibility consideration**: 
   - Consider supporting both v0.3 and v1.0 during transition
   - Use version detection via `AgentInterface.protocolVersion` field
   - Default `A2A-Version` empty = `0.3` per spec
4. **Test-driven**: Update tests alongside model changes to catch regressions early
5. **Proto alignment**: The proto file is normative — consider code generation from proto as the long-term strategy
6. **Binding priority**: Implement JSON-RPC binding first (current), add HTTP+JSON/REST second, gRPC third
7. **Samples last**: Update sample applications only after core SDK is stable

---

---

## Multi-Version Support Strategy

### Overview

To maintain backward compatibility while supporting the new v1.0 protocol, the codebase will support **both versions simultaneously** using namespace-based versioning.

### Recommended Approach: Namespace + Interface Pattern

#### Project Structure

```
src/A2A/
├── A2A.csproj
├── Common/
│   ├── Interfaces/
│   │   ├── ITask.cs
│   │   ├── IMessage.cs
│   │   ├── IPart.cs
│   │   └── IArtifact.cs
│   ├── JsonRpc/
│   │   ├── JsonRpcRequest.cs
│   │   ├── JsonRpcResponse.cs
│   │   └── JsonRpcError.cs
│   ├── A2AErrorCode.cs
│   └── ProtocolVersion.cs
│
├── V03/
│   ├── Models/
│   │   ├── AgentTask.cs          # Move existing
│   │   ├── AgentMessage.cs
│   │   ├── Part.cs
│   │   ├── TextPart.cs
│   │   ├── FilePart.cs
│   │   ├── DataPart.cs
│   │   ├── Artifact.cs
│   │   ├── AgentCard.cs
│   │   ├── TaskState.cs
│   │   └── ...
│   ├── Serialization/
│   │   ├── A2AJsonUtilities.cs
│   │   ├── BaseKindDiscriminatorConverter.cs
│   │   └── ...
│   ├── Client/
│   │   └── A2AClient.cs
│   └── Server/
│       └── A2AServerHandlers.cs
│
├── V10/
│   ├── Models/
│   │   ├── Task.cs               # New v1.0 models
│   │   ├── Message.cs
│   │   ├── Part.cs               # Flat oneof (text/raw/url/data)
│   │   ├── Artifact.cs
│   │   ├── AgentCard.cs
│   │   ├── AgentInterface.cs     # protocolBinding + protocolVersion
│   │   ├── AgentCardSignature.cs # JWS signatures
│   │   ├── TaskState.cs          # SCREAMING_SNAKE_CASE
│   │   ├── StreamResponse.cs     # Oneof wrapper
│   │   ├── SendMessageResponse.cs # Task | Message union
│   │   ├── SecurityScheme.cs     # Field-name oneof (5 types)
│   │   ├── OAuthFlows.cs         # Oneof (5 flow types)
│   │   ├── ListTasksRequest.cs
│   │   ├── ListTasksResponse.cs
│   │   └── ...
│   ├── Serialization/
│   │   ├── A2AJsonUtilities.cs
│   │   └── PartConverter.cs      # Field-presence based
│   ├── Client/
│   │   └── A2AClient.cs
│   └── Server/
│       └── A2AServerHandlers.cs
│
└── Versioning/
    ├── VersionDetector.cs        # Detect version from payload/headers
    ├── VersionedClientFactory.cs
    └── VersionedServerRouter.cs
```

#### Namespaces

```csharp
namespace A2A.Common;          // Shared interfaces, utilities
namespace A2A.V03.Models;      // v0.3 models
namespace A2A.V03.Client;      // v0.3 client
namespace A2A.V10.Models;      // v1.0 models  
namespace A2A.V10.Client;      // v1.0 client
```

### Key Design Patterns

#### 1. Version Detection

```csharp
public static class VersionDetector
{
    public static ProtocolVersion DetectFromAgentCard(JsonDocument doc)
    {
        // v1.0 uses "supportedInterfaces" with per-interface "protocolVersion"
        if (doc.RootElement.TryGetProperty("supportedInterfaces", out var interfaces)
            && interfaces.GetArrayLength() > 0
            && interfaces[0].TryGetProperty("protocolVersion", out var pv)
            && pv.GetString()?.StartsWith("1.") == true)
            return ProtocolVersion.V10;
        
        // v0.3 uses top-level "protocolVersion" (string)
        if (doc.RootElement.TryGetProperty("protocolVersion", out _))
            return ProtocolVersion.V03;
            
        return ProtocolVersion.Unknown;
    }
    
    public static ProtocolVersion DetectFromHeaders(HttpRequestHeaders headers)
    {
        if (headers.TryGetValues("A2A-Version", out var values))
        {
            var version = values.First();
            return version.StartsWith("1.") ? ProtocolVersion.V10 : ProtocolVersion.V03;
        }
        return ProtocolVersion.V03; // Default per spec: empty = 0.3
    }
}
```

#### 2. Versioned Client Factory

```csharp
public class A2AClientFactory
{
    public static IA2AClient Create(AgentCardInfo agentCard)
    {
        return agentCard.Version switch
        {
            ProtocolVersion.V03 => new V03.Client.A2AClient(agentCard),
            ProtocolVersion.V10 => new V10.Client.A2AClient(agentCard),
            _ => throw new NotSupportedException($"Protocol version not supported")
        };
    }
}
```

#### 3. Unified Public API (Optional)

```csharp
// High-level API that abstracts version differences
public class UnifiedA2AClient : IA2AClient
{
    private readonly IA2AClient _inner;
    
    public UnifiedA2AClient(string agentUrl)
    {
        var card = FetchAgentCard(agentUrl);
        _inner = A2AClientFactory.Create(card);
    }
    
    public async Task<ITask> SendMessageAsync(string message)
    {
        return await _inner.SendMessageAsync(message);
    }
}
```

### Multi-Version Migration Phases

#### Phase 1: Restructure for v0.3 (1-2 days with AI)

1. Create folder structure: `Common/`, `V03/`, `V10/`
2. Move ALL current models to `V03/Models/`
3. Move converters to `V03/Serialization/`
4. Update namespaces from `A2A` → `A2A.V03.Models`
5. Create interfaces in `Common/Interfaces/`
6. Update all using statements
7. Run tests, fix breaks

#### Phase 2: Extract Common (0.5-1 day)

1. Identify truly shared code (JsonRpc, error codes)
2. Move to `Common/`
3. Update V03 to use Common
4. Run tests

#### Phase 3: Implement v1.0 (3-5 days with AI)

1. Create `V10/Models/` - fresh implementation per spec
2. Create `V10/Serialization/` - new converters
3. Create `V10/Client/` and `V10/Server/`
4. Add tests for v1.0

#### Phase 4: Version Negotiation (1 day)

1. Implement VersionDetector
2. Implement VersionedClientFactory
3. Update A2A.AspNetCore to route by version
4. Test cross-version scenarios

### Namespace Migration Commands

Quick way to update namespaces across all files:

```powershell
# In PowerShell, from src/A2A folder:

# 1. Create new folder structure
New-Item -ItemType Directory -Path "Common/Interfaces", "V03/Models", "V03/Serialization", "V03/Client", "V10/Models", "V10/Serialization", "V10/Client", "Versioning" -Force

# 2. Move current models to V03
Move-Item Models/*.cs V03/Models/

# 3. Move converters
Move-Item BaseKindDiscriminatorConverter.cs V03/Serialization/
Move-Item A2AJsonUtilities.cs V03/Serialization/
Move-Item KebabCaseLowerJsonStringEnumConverter.cs Common/
```

**Sample Prompt for Claude to update namespaces:**
```
Update all .cs files in src/A2A/V03/Models/ to use namespace A2A.V03.Models 
instead of namespace A2A. Also add using A2A.Common; where interfaces are used.
```

### Revised Total Effort Estimate (Multi-Version Support)

| Task | Manual | With AI |
|------|--------|---------|
| Restructure folders & move files | 2 hours | 1 hour |
| Update V03 namespaces | 4 hours | 1 hour |
| Extract Common interfaces | 4 hours | 2 hours |
| Implement V10 models | 3-4 days | 1-2 days |
| Version negotiation | 1 day | 0.5 days |
| Testing & fixes | 2 days | 1 day |
| **Total** | **7-9 days** | **3-5 days** |

---

## AI-Assisted Migration Strategy

### Realistic Time Estimate with LLM Assistance

| Approach | Estimated Time |
|----------|----------------|
| Manual (no AI) | 22-30 man-days |
| **With LLM assistance (Claude/Copilot)** | **5-8 man-days** |
| Speedup factor | ~3-4x faster |

### Why LLMs Excel at This Type of Work

1. **Repetitive pattern changes** - Enum renames, property removals across many files
2. **Boilerplate generation** - New model classes, converters, tests
3. **JSON/serialization logic** - LLMs understand serialization patterns well
4. **Test generation** - Creating test cases from specifications
5. **Documentation** - Generating migration guides and changelogs

### Optimal Workflow Strategy

#### 1. Batch Similar Changes Together
Instead of file-by-file, group by change type:

```
Session 1: "Update all enum values across TaskState, MessageRole to SCREAMING_SNAKE_CASE"
Session 2: "Remove 'kind' property from all model classes that inherit from A2AEvent"
Session 3: "Create new StreamResponse wrapper type with proper serialization"
```

#### 2. Provide Clear Context Each Session
```
"Here's the spec change: [paste relevant section]
Here's the current code: [paste file]
Apply the change following our existing patterns in copilot-instructions.md"
```

#### 3. Use Specification as Source of Truth
- Keep the v1.0 spec document open
- Copy/paste relevant sections when requesting changes
- Ask Claude to validate changes against spec

### Recommended Session Breakdown

| Session | Task | Est. with AI |
|---------|------|--------------|
| 1 | Enum changes (TaskState, MessageRole to SCREAMING_SNAKE_CASE) + tests | 2-3 hours |
| 2 | Remove kind discriminator; flatten Part (TextPart/FilePart/DataPart → single Part) | 3-4 hours |
| 3 | Remove kind from A2AEvent hierarchy; implement SendMessageResponse union | 2-3 hours |
| 4 | New StreamResponse wrapper type + field-presence converters | 2-3 hours |
| 5 | SecurityScheme/OAuthFlows refactor to field-name oneofs | 2-3 hours |
| 6 | AgentCard restructure (supportedInterfaces, signatures, security) | 3-4 hours |
| 7 | Message/Task object field additions (messageId, contextId, etc.) | 2 hours |
| 8 | New error codes + error format support (Problem Details, ErrorInfo) | 1-2 hours |
| 9 | PushNotification simplification + CRUD operations | 2 hours |
| 10 | ListTasks operation (filtering, pagination) | 3-4 hours |
| 11 | JSON-RPC method name PascalCase migration + version negotiation | 2-3 hours |
| 12 | Test updates and validation | 4-6 hours |
| 13 | Server/Client updates | 4-6 hours |
| 14 | Sample apps & docs | 2-3 hours |
| **Total** | | **~35-50 hours (5-8 days)** |

### Pro Tips for LLM-Assisted Refactoring

#### ✅ DO:
- **Start with tests** - Ask Claude to update tests first, then implementation
- **One logical change per request** - "Change enum naming" not "refactor everything"
- **Validate incrementally** - Build after each session, fix errors immediately
- **Use multi-file edits** - Claude can edit 5-10 files in one request efficiently
- **Leverage existing patterns** - Point to copilot-instructions.md for consistency

#### ❌ DON'T:
- Try to do entire migration in one conversation (context limits)
- Make changes without running tests between sessions  
- Skip the "why" - explain the spec requirement for better results
- Forget to commit working states frequently

### Sample Prompts for Each Phase

**Phase 1 - Enums:**
```
Update TaskState enum in src/A2A/Models/TaskState.cs to use SCREAMING_SNAKE_CASE 
per A2A v1.0 spec (ProtoJSON format):
- submitted → TASK_STATE_SUBMITTED
- working → TASK_STATE_WORKING
- unknown → TASK_STATE_UNSPECIFIED
[etc.]
Also add ROLE_UNSPECIFIED to MessageRole.
Update the JSON converter to handle both old and new formats for backward compatibility.
```

**Phase 2 - Part Flattening:**
```
The A2A v1.0 spec flattens Part into a single type with oneof semantics.
Remove TextPart, FilePart, DataPart, FileContent, FileWithBytes, FileWithUri.
Create single Part class with mutually exclusive fields: text, raw, url, data.
Add shared optional fields: metadata (JsonElement), filename (string), mediaType (string).
Create a PartConverter that uses field presence (not kind) to determine content type.
Here's the spec §4.1.6: A Part MUST contain exactly one of: text, raw, url, data.
```

**Phase 3 - AgentCard:**
```
Restructure AgentCard per v1.0 spec §4.4.1:
- Remove: url, preferredTransport 
- Rename: additionalInterfaces → supportedInterfaces (required array of AgentInterface)
- Remove card-level protocolVersion; version is now per-interface
- Move: supportsAuthenticatedExtendedCard → capabilities.extendedAgentCard
- Add: securitySchemes (map<string, SecurityScheme>), securityRequirements, signatures
- Rename: inputModes → defaultInputModes, outputModes → defaultOutputModes
Update AgentInterface: transport → protocolBinding, add protocolVersion (required), tenant (optional)
```

### Suggested Calendar Schedule

**Estimated calendar time: 1.5-2.5 weeks** (not full-time)

| Day | Focus Area |
|-----|------------|
| Day 1-2 | Core model changes (enums, kind removal, flatten Part) |
| Day 3 | SendMessageResponse union, StreamResponse wrapper, field-presence converters |
| Day 4 | SecurityScheme/OAuthFlows oneof refactors |
| Day 5 | AgentCard restructure (supportedInterfaces, signatures) |
| Day 6 | Message/Task field additions, event updates |
| Day 7 | New operations (ListTasks, SubscribeToTask) |
| Day 8 | Error handling, PushNotification CRUD |
| Day 9-10 | Testing, fixes, validation |
| Day 11-12 | Server/client updates, samples, docs |

The key is **iterative validation** - don't accumulate too many changes before building and testing. LLMs are excellent at this work but need human verification checkpoints.

---

## References

- [A2A Protocol v0.3.0 Specification](https://a2a-protocol.org/v0.3.0/specification/)
- [A2A Protocol RC v1.0 Specification](https://a2a-protocol.org/latest/specification/)
- [A2A Protocol Proto Definition (normative)](https://github.com/a2aproject/A2A/blob/main/specification/a2a.proto)
- [Migration Guidance (Appendix A)](https://a2a-protocol.org/latest/specification/#appendix-a-migration-legacy-compatibility)
- [ADR-001: ProtoJSON Serialization](https://a2a-protocol.org/latest/adrs/adr-001-protojson-serialization.md)
- [ProtoJSON Specification](https://protobuf.dev/programming-guides/json/)
- [RFC 9457: Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457.html)
- [RFC 7515: JSON Web Signature (JWS)](https://tools.ietf.org/html/rfc7515)
- [RFC 8628: OAuth 2.0 Device Authorization Grant](https://tools.ietf.org/html/rfc8628)
