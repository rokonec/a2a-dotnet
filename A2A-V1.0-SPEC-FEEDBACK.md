# A2A v1.0 Specification Feedback

**Date**: February 13, 2026 (revised)  
**Spec**: https://a2a-protocol.org/latest/specification/  
**Proto**: https://github.com/a2aproject/A2A/blob/main/specification/a2a.proto  
**Perspective**: Cross-language SDK implementation (C#, Python, JavaScript, Java, Go)  
**Primary implementation context**: .NET SDK with System.Text.Json + ASP.NET Core

---

Ordered by severity (S1 = critical, S5 = minor). Each item includes cross-language impact assessment.

---

## S1 — Critical (spec ambiguity or internal inconsistency causes incompatible implementations)

### S1.1: Field-presence oneof detection is unspecified for ambiguous payloads

**Problem**: The spec correctly constrains all five oneof types with "MUST contain exactly one" language (§3.2.3 StreamResponse, §4.1.6 Part, §4.5.1 SecurityScheme, §4.5.7 OAuthFlows, §10.4.1 SendMessageResponse). However, this is a production constraint on senders. The spec does not define what **parsers** must do upon receiving a violation — e.g., a payload with **both** `task` and `message` fields present, or **neither** present. For example:

```json
{
  "task": {"id": "t1", "contextId": "c1", "status": {...}},
  "message": {"messageId": "m1", "role": "ROLE_AGENT", "parts": [...]}
}
```

ProtoJSON §3 says oneofs must have at most one field set, and parsers **must reject** messages with multiple oneof fields. However:

- While §3.2.3 et al. state messages "MUST contain exactly one" oneof field, this constrains the sender. The spec never explicitly mandates the parser-side corollary: "Parsers MUST reject JSON objects with multiple oneof fields set."
- No error code is defined for this class of validation failure (neither for multiple fields nor for zero fields).

Languages with proto-native parsers enforce this automatically. Languages using general-purpose JSON parsers do not — they silently populate both fields, leaving the object in an invalid state.

**Cross-language impact:**

| Language | Behavior with both `task` and `message` present |
|----------|------------------------------------------------|
| Python | `json.loads()` populates both dict keys. No rejection. |
| JavaScript | `JSON.parse()` populates both object properties. No rejection. |
| Go | `protojson.Unmarshal` rejects — returns error. ✓ |
| Java | `JsonFormat.parser()` rejects — throws exception. ✓ |
| C# | System.Text.Json populates both nullable properties silently. No rejection without custom converter. |

**Recommendation**: Add a normative statement: "Parsers MUST reject messages where more than one field of a oneof group is present in the JSON representation" and assign error code (likely `InvalidRequest` / `-32600`).

---

### S1.2: `google.protobuf.Value` serialization for `Part.data` is underspecified

**Proto:**
```protobuf
message Part {
  oneof content {
    ...
    google.protobuf.Value data = 4;
  }
}
```

`google.protobuf.Value` is a proto well-known type that wraps any JSON value. Per ProtoJSON, it serializes **directly as its value** — not wrapped. So:

```json
{"data": "hello"}        // Value containing a string
{"data": 42}             // Value containing a number  
{"data": {"key": "val"}} // Value containing a struct
{"data": null}           // Value containing NullValue
```

**Problem**: `{"data": null}` is ambiguous. Does it mean:
- (a) The `data` variant is set, containing a `NullValue`? → `ContentCase == Data`
- (b) The `data` variant is not set (field absent vs. explicitly null)? → `ContentCase == None`

Proto wire format distinguishes these (field present with NullValue vs. field absent). But JSON doesn't — `"data": null` and omitting `"data"` entirely are semantically different in proto but many JSON parsers/serializers treat `null` values as equivalent to absent.

**Cross-language impact:**

| Language | Impact |
|----------|--------|
| Python | `dict.get("data")` returns `None` for both absent and `null`. Ambiguous. |
| JavaScript | `obj.data === undefined` (absent) vs `obj.data === null` (null). **Distinguishable** but fragile. |
| Go | protojson correctly distinguishes. ✓ |
| Java | protojson correctly distinguishes. ✓ |
| C# | `JsonElement?` — absent = `null` property, explicit null = `JsonElement` with `ValueKind.Null`. Distinguishable but requires careful implementation. |

**Recommendation**: Adopt the "null equals absent" rule: Senders SHOULD omit oneof fields rather than sending `null`. Parsers MUST treat a `null`-valued oneof field identically to the field being absent.

Two options were considered:

| Approach | Rule | Pros | Cons |
|----------|------|------|------|
| **Option 1: Reject null** | Parsers MUST reject `"data": null` as invalid | Strict; no ambiguity | Requires validation code that most JSON parsers don't naturally enforce; creates compliance gap for Python/JS |
| **Option 2: null = absent** (recommended) | Parsers MUST treat `"data": null` as field-not-present | Lenient; matches Postel's Law; aligns with default behavior of Python, JS, and C# JSON parsers | Cannot represent `NullValue` in the `data` variant |

Option 2 is preferred because it matches what 3 of 5 target languages (Python, JavaScript, C#) do by default, requires no additional validation code, and follows Postel's Law ("be liberal in what you accept"). The practical loss — inability to represent JSON `null` as a valid `data` value — is acceptable since the `data` variant is intended for structured data, not null signaling.

**Suggested spec wording:**

> "For oneof fields serialized as ProtoJSON, a JSON `null` value MUST be treated as equivalent to the field being absent (i.e., the oneof is not set). Senders SHOULD omit oneof fields entirely rather than setting them to `null`. This applies to all oneof groups in the protocol, including `Part.content`, `SendMessageResponse.payload`, `StreamResponse.payload`, `SecurityScheme.scheme`, and `OAuthFlows.flow`."

---

### S1.3: Specification examples use v0.3.0 formats, contradicting v1.0 normative definitions

**Problem**: Several examples in the normative specification contain v0.3.0-era values and structures that contradict the v1.0 data model defined in §4. Since examples in a specification are routinely used as implementation reference by SDK developers, these inconsistencies will directly cause incorrect implementations.

**Affected sections:**

**§6.6 Push Notification Setup and Usage** — three inconsistencies in one example:

1. Uses `"schemes": ["Bearer"]` (v0.3 array format) but §4.3.2 `AuthenticationInfo` defines `"scheme": "Bearer"` (v1.0 singular string):

    ```json
    // §6.6 example (WRONG):
    "authentication": { "schemes": ["Bearer"] }
    
    // §4.3.2 definition (CORRECT):
    "authentication": { "scheme": "Bearer" }
    ```

2. Uses `"state": "submitted"` (v0.3 lowercase kebab-case) but §4.1.3 `TaskState` defines `"TASK_STATE_SUBMITTED"` (v1.0 SCREAMING_SNAKE_CASE):

    ```json
    // §6.6 response (WRONG):
    "status": { "state": "submitted", ... }
    
    // §4.1.3 definition (CORRECT):
    "status": { "state": "TASK_STATE_SUBMITTED", ... }
    ```

3. Uses `"state": "completed"` in the webhook notification but should be `"TASK_STATE_COMPLETED"`.

**§6.5 Task Listing — Validation Error Example** — uses unlisted enum values:

```json
// §6.5 validation error (WRONG):
"message": "Invalid status value 'running'. Must be one of: pending, working, completed, failed, canceled"
```
Neither `"running"` nor `"pending"` exist in any version of the spec. Valid values are `TASK_STATE_WORKING`, `TASK_STATE_SUBMITTED`, etc.

**§6.5 Task Listing — Request/Response examples** — uses `POST /tasks/list` but §11.3.2 defines `GET /tasks`:

```
// §6.5 example (WRONG):
POST /tasks/list HTTP/1.1

// §11.3.2 URL Patterns (CORRECT):
GET /tasks
```

**Cross-language impact**: All languages equally — incorrect examples lead to incorrect implementations regardless of language. SDK authors who build test suites from spec examples will encode wrong behavior.

**Recommendation**: Audit all examples in §6 to align with the normative definitions in §4 and §11. Consider adding a CI check that validates examples against the proto-generated JSON schema.

---

### S1.4: `securityRequirements` (§4.4.1) vs `security` (§8.5) — field name inconsistency

**Problem**: The `AgentCard` data model definition in §4.4.1 states:

> `securityRequirements` | array of SecurityRequirement | No | Security requirements for contacting the agent.

But the sample Agent Card in §8.5 uses the field name `security`:

```json
{
  "securitySchemes": { "google": { ... } },
  "security": [{ "google": ["openid", "profile", "email"] }]
}
```

One of these is wrong. If the proto field is `security_requirements`, the JSON name should be `securityRequirements`. If the proto field is `security`, the JSON name should be `security`. This is not merely a documentation issue — it causes implementations to use the wrong field name on the wire.

**Cross-language impact:**

| Language | Impact |
|----------|--------|
| Go/Java | Proto-generated code uses whichever the proto defines. No ambiguity if proto is read. |
| Python | SDK author reads spec prose → uses `securityRequirements`. Reads sample → uses `security`. Two incompatible SDKs. |
| JavaScript | Same as Python. |
| C# | Same. The `[JsonPropertyName]` attribute will encode whichever the developer picks. |

**Recommendation**: Verify against the normative `a2a.proto` file and correct whichever is wrong. Update both the data model table in §4.4.1 and the sample in §8.5 to match.

---

### S1.5: `tenant` is described as "provided as a path parameter" but URL patterns lack tenant segment

**Problem**: Every request message table in sections §3.1 and §10.4 describes `tenant` as:

> `tenant` | string | No | Optional tenant, provided as a path parameter.

But the URL patterns defined in §11.3 do not include a `{tenant}` path segment in any URL:

```
POST /message:send          ← no tenant
GET  /tasks/{id}            ← no tenant
GET  /tasks                 ← no tenant
POST /tasks/{id}:cancel     ← no tenant
```

How is tenant actually transmitted? The options are:
1. Path prefix: `/tenants/{tenant}/tasks/{id}` — not shown in URL patterns
2. Query parameter: `/tasks/{id}?tenant=xyz` — not documented
3. HTTP header: `A2A-Tenant: xyz` — not defined in §3.2.6 or §14.2
4. gRPC metadata only — makes it gRPC-specific, contradicting "path parameter" description

The gRPC service definition shows `tenant` as a field in proto request messages, which naturally maps to a path parameter via gRPC-Gateway annotations. But for HTTP+JSON/REST and JSON-RPC, the transmission mechanism is undefined.

**Cross-language impact**: All languages equally. Server implementations cannot interoperate if they disagree on where to find the tenant.

**Recommendation**: Either:
- (a) Define URL patterns with tenant prefix: `/{tenant}/tasks/{id}` or `/tenants/{tenant}/tasks/{id}`, OR
- (b) Remove "provided as a path parameter" from the description and specify the actual mechanism for each binding, OR
- (c) For JSON-RPC: include `tenant` in the `params` object (already there). For HTTP+JSON: specify as query parameter or URL prefix. For gRPC: request field (already there).

---

## S2 — Major (causes significant implementation complexity)

### S2.1: SCREAMING_SNAKE_CASE enum prefix creates SDK ergonomics burden

**Proto:**
```protobuf
enum TaskState {
  TASK_STATE_UNSPECIFIED = 0;
  TASK_STATE_SUBMITTED = 1;
  TASK_STATE_WORKING = 2;
  ...
}
enum Role {
  ROLE_UNSPECIFIED = 0;
  ROLE_USER = 1;
  ROLE_AGENT = 2;
}
```

ProtoJSON requires these exact strings on the wire: `"TASK_STATE_COMPLETED"`, `"ROLE_USER"`.

**Problem**: Every non-Go, non-Java language must choose between:
1. **Wire-faithful enums** — expose `TaskState.TASK_STATE_COMPLETED` to users (ugly, redundant prefix)
2. **Idiomatic enums** — expose `TaskState.Completed` with custom serialization to/from the wire format (extra code)
3. **String constants** — don't use enums at all, just strings (loses type safety)

The prefix convention (`TASK_STATE_` on `TaskState`) is a protobuf anti-collision measure that has no value in typed languages with namespaced enums. In C# specifically, the standard .NET `JsonStringEnumConverter` has no built-in support for prefix-stripping; implementing this requires a custom `JsonConverter<TEnum>` with a mapping dictionary, impacting every enum in the protocol across every model type.

**Cross-language impact:**

| Language | Idiomatic enum | Wire format | Gap |
|----------|---------------|-------------|-----|
| Go | `TaskState_TASK_STATE_COMPLETED` | `"TASK_STATE_COMPLETED"` | Go devs expect this; proto-native ✓ |
| Java | `TaskState.TASK_STATE_COMPLETED` | `"TASK_STATE_COMPLETED"` | Java devs expect this; proto-native ✓ |
| Python | `TaskState.TASK_STATE_COMPLETED` | `"TASK_STATE_COMPLETED"` | Acceptable with proto; unusual without ✓ |
| JavaScript/TS | `TaskState.Completed` | `"TASK_STATE_COMPLETED"` | TS devs expect PascalCase enums. **Mapping needed.** |
| C# | `TaskState.Completed` | `"TASK_STATE_COMPLETED"` | C# convention is PascalCase. **Custom converter needed.** |

**C# implementation detail**: The current v0.3 SDK uses `KebabCaseLowerJsonStringEnumConverter<TEnum>`. Migrating to SCREAMING_SNAKE requires per-enum `JsonConverter<TEnum>` implementations with explicit `switch`-based mappings (e.g., `TaskStateJsonConverter`). While .NET 9 added `[JsonStringEnumMemberName]` which could handle this declaratively, multi-TFM SDKs targeting net8.0/netstandard2.0 cannot use it. No built-in STJ naming policy can produce the type-prefixed format (`TASK_STATE_COMPLETED`). The implementation is straightforward (~25 lines per enum) and AOT-safe, but it is bespoke work with no off-the-shelf solution. See [docs/enum-screaming-snake-implementation.md](docs/enum-screaming-snake-implementation.md) for detailed analysis.

**Recommendation**: Consider defining canonical short names alongside proto names in the spec (like gRPC-Gateway does), or at minimum, add a non-normative note acknowledging that SDKs MAY map to idiomatic enum names as long as they serialize to the canonical proto strings. This single sentence would save every C#, JS, and Swift SDK from re-inventing the same mapping.

---

### S2.2: No guidance on which binding SDKs should prioritize

**Context**: The spec defines three protocol bindings: JSON-RPC (`JSONRPC`), gRPC (`GRPC`), and HTTP+JSON (`HTTP+JSON`). Agents declare their supported bindings via `AgentCard.supported_interfaces`, and each binding is opt-in — agents and SDKs are free to support any subset. This is a reasonable design.

**Problem**: While bindings are optional per-agent, the spec provides no guidance on which binding(s) an SDK should implement first, or which is the most broadly expected. An SDK client that only supports JSON-RPC can't talk to a gRPC-only agent. Each binding has different:
- Error format (JSON-RPC error vs `google.rpc.Status` vs RFC 9457)
- Streaming mechanism (SSE vs server-streaming RPC vs SSE)
- Content types (`application/json` vs proto vs `application/a2a+json`)

**Cross-language impact:**

| Language | Natural first binding | Secondary |
|----------|----------------------|-----------|
| Go | gRPC (proto-native) | HTTP+JSON via grpc-gateway |
| Java | gRPC (proto-native) | JSON-RPC or HTTP+JSON |
| Python | HTTP+JSON or JSON-RPC | gRPC (requires grpcio) |
| JavaScript | HTTP+JSON | JSON-RPC (gRPC complex in browsers) |
| C# | Depends — gRPC has excellent .NET support (Grpc.AspNetCore), but JSON-RPC was chosen for v0.3 | HTTP+JSON fits ASP.NET minimal APIs naturally |

**Recommendation**: Add a non-normative note suggesting a recommended implementation order for SDK authors, e.g.: "SDKs are encouraged to support at least one binding. HTTP+JSON offers the broadest reach across languages and environments." This helps SDK teams prioritize without mandating.

---

### S2.3: `google.protobuf.Struct` for metadata makes typed access painful

**Proto:**
```protobuf
google.protobuf.Struct metadata = 6;  // on Task, Message, Part, Artifact, etc.
```

In ProtoJSON, `Struct` serializes as a plain JSON object `{"key": value}`. But in non-proto languages, deserializing this into a useful type requires significant work.

**Cross-language impact:**

| Language | Natural mapping | Problem |
|----------|----------------|---------|
| Go | `structpb.Struct` → `map[string]interface{}` via `.AsMap()` | Extra conversion step |
| Java | `Struct` → iterate `getFieldsMap()` → `Value` → unwrap | Very verbose |
| Python | `MessageToDict(struct)` → dict | Easy ✓ |
| JavaScript | Plain object | Easy ✓ |
| C# | No native Struct type. Must use `Dictionary<string, JsonElement>` or `JsonElement` or `JsonObject` | Type chosen by SDK affects entire downstream API |

**C# elaboration**: In the current .NET SDK, metadata is `JsonElement?` — a readonly struct that forces callers to access nested values through `GetProperty()` / `TryGetProperty()` chains that return more `JsonElement` values. This is functional but ergonomically far from the dictionary access developers expect (`metadata["key"]`). Using `Dictionary<string, object?>` would be ergonomic but lossy. Using `JsonObject` (from System.Text.Json.Nodes) would be mutable and ergonomic but doesn't integrate with source-generation. There is no obviously correct choice without spec guidance.

**Recommendation**: Add a non-normative note recommending SDK mappings: `Struct` → language-native dictionary/map type (e.g., `dict` in Python, `Map<String, Object>` in Java, `Dictionary<string, JsonElement>` in C#, `map[string]interface{}` in Go). This guidance prevents each SDK from independently inventing an incompatible mapping.

---

### S2.4: `ListTasksResponse.totalSize` being REQUIRED is impractical at scale

**Proto / §3.1.4:**
```
| totalSize | integer | Yes | Total number of tasks available (before pagination). |
```

`totalSize` is marked REQUIRED (field behavior annotation), meaning servers MUST return an accurate total count on every `ListTasks` response.

**Problem**: Computing exact total counts is expensive or impossible at scale for many storage backends:
- SQL databases: `SELECT COUNT(*)` on large tables with complex filters requires a full scan or separate count query
- NoSQL / document stores (DynamoDB, Cosmos DB, Firestore): do not provide efficient count operations
- ElasticSearch: `total_hits` is approximate by default
- In-memory stores: trivial, but not representative of production

Google's own AIP-158 (pagination standard) treats `total_size` as optional for exactly this reason, noting that "servers that cannot efficiently compute total_size may omit it from responses."

**Cross-language impact**: This is not language-specific — it's architecture-specific. Any server implementation backed by a database that cannot efficiently compute filtered counts is forced to either:
1. Execute an expensive additional query on every list request
2. Return an inaccurate count (violating the REQUIRED constraint)
3. Return -1 or 0 as a sentinel (specification violation)

**C# specific**: ASP.NET + Entity Framework typical pattern is `IQueryable<T>.CountAsync()` + `IQueryable<T>.Skip().Take().ToListAsync()` — two queries per request. For filtered queries with indexes, this can double response time.

**Recommendation**: Change `totalSize` from REQUIRED to OPTIONAL, or change the semantics to "estimated total size" with a note that implementations MAY omit this field if an efficient count is not available. This aligns with Google AIP-158 and avoids forcing O(n) count operations on every paginated request.

---

### S2.5: `Message.messageId` required on all messages extends to status messages

**§4.1.4 Message and §4.1.2 TaskStatus:**

`Message` is used both for client↔agent communication AND as an optional field within `TaskStatus`:

```protobuf
message TaskStatus {
  TaskState state = 1;
  optional Message message = 2;  // status message
  ...
}
```

`Message.messageId` is REQUIRED (§4.1.4). This means every `TaskStatus.message` — even a simple "processing your request..." status update — must include a unique UUID.

**Problem**: Status messages are often **ephemeral, server-generated, and numerous**. Requiring a UUID for each one:
1. Adds overhead — UUID generation on every status transition
2. Implies persistence — UUIDs suggest the message is addressable/retrievable
3. Conflates two use cases — conversational messages (which benefit from IDs for deduplication/threading) and transient status annotations (which do not)

**Cross-language impact:**

| Language | UUID generation cost | Concern level |
|----------|---------------------|--------------|
| Go | `uuid.New()` — fast (V4, crypto/rand) | Low |
| Java | `UUID.randomUUID()` — fast | Low |
| Python | `uuid.uuid4()` — fast | Low |
| C# | `Guid.NewGuid()` — fast | Low overhead, but semantic concern remains |
| JavaScript | `crypto.randomUUID()` — fast | Low |

The performance cost is negligible, but the **semantic overhead** is real: agent implementers must now decide whether to persist these IDs, whether they're referenced in `referenceTaskIds`, and how to handle duplicate status messages during retries.

**Recommendation**: Consider either:
- (a) Making `messageId` optional on messages with `role = ROLE_AGENT` that appear in `TaskStatus.message`, or
- (b) Adding a non-normative note that `messageId` for status messages is primarily for deduplication and need not be persisted beyond the immediate delivery context.

---

## S3 — Moderate (causes friction but has clear workarounds)

### S3.1: `bytes` field (`Part.raw`) base64 variant is not specified

**Proto:**
```protobuf
bytes raw = 2;  // In JSON serialization, encoded as base64 string
```

ProtoJSON says `bytes` uses **standard base64** (RFC 4648 §4, with `+` and `/`, with `=` padding). But the proto spec also says parsers should accept **URL-safe base64** (RFC 4648 §5, with `-` and `_`). The A2A spec comment says "base64 string" without specifying which variant.

**Cross-language impact:**

| Language | Default base64 | Problem |
|----------|----------------|---------|
| Go | `base64.StdEncoding` (standard) | Must also accept URL-safe |
| Java | `Base64.getDecoder()` (standard) | Must also accept URL-safe |
| Python | `base64.b64decode` (standard, lenient) | Usually works either way ✓ |
| JavaScript | `atob()` (standard only) | Fails on URL-safe input |
| C# | `Convert.FromBase64String` (standard only) | Fails on URL-safe input |

**Recommendation**: Explicitly state: "The `raw` field MUST be encoded using standard base64 (RFC 4648 §4). Decoders SHOULD accept both standard and URL-safe base64."

---

### S3.2: Oneof constraint violations lack defined parser behavior and error codes

Every oneof type in the spec includes a normative "MUST contain exactly one" constraint (e.g., §4.1.6: "A `Part` MUST contain exactly one of the following: `text`, `raw`, `url`, `data`"). This correctly prohibits both zero and multiple fields.

**Problem**: While the constraint exists, the spec doesn't define parser behavior for violations. §5.7 says "Implementations SHOULD ignore unrecognized fields" (for forward compatibility), but doesn't address the separate case of oneof violations: receiving a `Part` with no content, a `SecurityScheme` with two schemes, or a `StreamResponse` with zero variants. No error code is assigned for this class of validation failure.

This partially overlaps with S1.1 (which focuses on the multi-field case). The zero-field case — e.g., a `Part` with metadata but no content — is arguably more likely in practice (accidental omission vs. intentional duplication).

**Cross-language impact**: All languages equally — no standard error response is defined for empty or overfull oneofs.

**Recommendation**: Add a normative statement: "Parsers MUST reject messages where a oneof group contains zero or more than one field. Such violations SHOULD be reported as validation errors" and map to appropriate error codes per binding (e.g., `-32602` for JSON-RPC, `INVALID_ARGUMENT` for gRPC, `400` for HTTP+JSON).

---

### S3.3: `optional` keyword used inconsistently for presence tracking

Some fields use proto3's `optional` keyword (which enables explicit presence tracking), while semantically-similar fields don't:

```protobuf
optional int32 history_length = 3;   // optional — can distinguish 0 from absent ✓
bool blocking = 4;                    // NOT optional — 0/false and absent are same
optional string documentation_url = 6; // optional — can distinguish "" from absent ✓
string version = 5;                   // NOT optional — "" and absent are same
```

`blocking = false` is indistinguishable from "blocking not set" on the proto wire. The spec says "Default is false" — so this works. But it means an SDK can never know if the client **explicitly chose non-blocking** vs. **didn't specify**.

**Cross-language impact:**

| Language | Impact |
|----------|--------|
| Go | `bool` is zero-valued; no presence. Needs `*bool` wrapper to distinguish. |
| Java | `hasBlocking()` returns false for both default and absent. |
| Python | Same as proto default behavior. |
| C# | `bool` can't distinguish; needs `bool?` to model presence. SDK author must choose between `bool` (simple, lossy) and `bool?` (correct, verbose). |
| JavaScript | `undefined` vs `false` — technically distinguishable if carefully handled. |

**Recommendation**: Audit all `bool` and `int32` fields and apply `optional` consistently where "not set" has different semantics from "default value". Specifically, `blocking` should probably be `optional bool blocking = 4;` so clients can express "I have no preference" vs. "I want non-blocking".

---

### S3.4: `Content-Type` inconsistency across bindings and examples

**Problem**: The spec defines different content types for each binding, but the examples don't consistently use them:

| Source | Content-Type |
|--------|-------------|
| §9.1 JSON-RPC Protocol Requirements | `application/json` |
| §11.1 HTTP+JSON/REST Protocol Requirements | `application/json` |
| §14.1.1 IANA Media Type Registration | `application/a2a+json` |
| §6.1-§6.8 Workflow Examples (request) | `application/a2a+json` |
| §6.1-§6.8 Workflow Examples (response) | `application/a2a+json` |
| §11.4 HTTP+JSON Request/Response Format | `application/json` |

So the examples in §6 use `application/a2a+json`, but both binding definitions (§9.1, §11.1) say `application/json`. The IANA registration (§14.1.1) registers `application/a2a+json` and says it's "intended for the HTTP+JSON/REST binding."

Questions that arise:
1. Should servers accept both? Which is normative for requests?
2. Should responses use `application/a2a+json` or `application/json`?
3. Should JSON-RPC use `application/json` exclusively (per §9.1) while HTTP+JSON/REST uses `application/a2a+json`?

**Cross-language impact**: All server implementers must make a content-type decision. C# ASP.NET defaults to `application/json` for JSON responses; explicitly setting `application/a2a+json` requires additional configuration.

**Recommendation**: Clarify in §9.1 and §11.1 which content types are accepted and produced. A reasonable approach: servers SHOULD accept both `application/json` and `application/a2a+json`, and SHOULD respond with `application/a2a+json` for HTTP+JSON/REST and `application/json` for JSON-RPC.

---

### S3.5: Missing `ImplicitOAuthFlow` and `PasswordOAuthFlow` type definitions

**§4.5.7 OAuthFlows** lists five oneof variants:

| Variant | Type | Defined in spec? |
|---------|------|-----------------|
| `authorizationCode` | `AuthorizationCodeOAuthFlow` | Yes (§4.5.8) ✓ |
| `clientCredentials` | `ClientCredentialsOAuthFlow` | Yes (§4.5.9) ✓ |
| `implicit` | `ImplicitOAuthFlow` | **No** — §4.5.7 shows empty description |
| `password` | `PasswordOAuthFlow` | **No** — §4.5.7 shows empty description |
| `deviceCode` | `DeviceCodeOAuthFlow` | Yes (§4.5.10) ✓ |

**Problem**: Two of the five OAuthFlows variants reference types that are never defined in the specification. Implementers must guess the field structure, presumably by analogy with `AuthorizationCodeOAuthFlow` (which has `authorizationUrl`, `tokenUrl`, `refreshUrl`, `scopes`). But without normative definitions, implementations may diverge.

The `implicit` flow is deprecated by OAuth 2.1 (draft-ietf-oauth-v2-1), and `password` (Resource Owner Password Credentials) is similarly discouraged. If these are intentionally omitted because they're deprecated, the spec should say so explicitly.

**Cross-language impact**: All implementers equally — missing type definitions affect proto codegen and hand-written models alike.

**Recommendation**: Either:
- (a) Add complete field definitions for `ImplicitOAuthFlow` and `PasswordOAuthFlow`, or
- (b) Mark them as deprecated in the spec with a note: "These flow types are included for OpenAPI compatibility but SHOULD NOT be used in new implementations. See OAuth 2.1 for guidance."

---

### S3.6: `stateTransitionHistory` appears in sample AgentCard but not in `AgentCapabilities` definition

**§8.5 Sample Agent Card** includes:
```json
"capabilities": {
  "streaming": true,
  "pushNotifications": true,
  "stateTransitionHistory": false,
  "extendedAgentCard": true
}
```

**§4.4.3 AgentCapabilities** defines these fields:
- `streaming` ✓
- `pushNotifications` ✓
- `extensions` ✓
- `extendedAgentCard` ✓
- `stateTransitionHistory` — **NOT DEFINED**

**Problem**: Either `stateTransitionHistory` is a real capability that was omitted from the schema definition, or it's a leftover from an earlier draft that should be removed from the sample.

**Impact**: SDK developers implementing AgentCapabilities from the data model definition will not include `stateTransitionHistory`. Developers copying from the sample will add it. Interoperability suffers.

**Recommendation**: Either add `stateTransitionHistory` to §4.4.3 with a proper definition, or remove it from the §8.5 sample.

---

## S4 — Minor (cosmetic or documentation issues)

### S4.1: Inconsistent field naming between proto and JSON (acronym handling)

Proto uses `snake_case`, ProtoJSON converts to `camelCase`. This is standard. But some field names create surprising JSON:

| Proto field | JSON (camelCase) | Surprise? |
|---|---|---|
| `context_id` | `contextId` | Normal ✓ |
| `open_id_connect_url` | `openIdConnectUrl` | Normal ✓ |
| `pkce_required` | `pkceRequired` | `PKCE` is an acronym — should it be `PKCERequired`? |
| `oauth2_metadata_url` | `oauth2MetadataUrl` | lowercase `oauth2` looks odd |
| `device_authorization_url` | `deviceAuthorizationUrl` | Normal ✓ |

**Impact**: Low — ProtoJSON rules are deterministic, so all implementations agree. But `pkceRequired` vs `PKCERequired` may trip up developers reading documentation. In C# specifically, the PascalCase convention would produce `PkceRequired` (matching the JSON camelCase `pkceRequired`), but many developers would instinctively write `PKCERequired` — requiring an explicit `[JsonPropertyName("pkceRequired")]`.

**Recommendation**: No change needed, but documentation examples should always show the exact camelCase JSON field names to avoid confusion.

---

### S4.2: `reserved` fields without comments explaining what was removed

```protobuf
message AgentCard {
  reserved 3, 9, 14, 15, 16;
  // ...
}
message TaskArtifactUpdateEvent {
  reserved 4;
  // ...
}
```

**Problem**: No comment explaining what fields 3, 9, 14, 15, 16 used to be. SDK developers implementing migration from v0.3.0 need to know what was removed to handle backward compatibility. The migration appendix (Appendix A) partially documents renames but doesn't map to specific field numbers.

**Recommendation**: Add comments: `reserved 3; // was 'url' in v0.3.0` etc. This is standard practice in Google's own protos.

---

### S4.3: `AgentInterface.protocol_binding` is `string` not enum

```protobuf
string protocol_binding = 2;  // "JSONRPC", "GRPC", "HTTP+JSON"
```

This is intentionally open-ended ("to be easily extended for other protocol bindings"). But the three canonical values aren't defined as known constants anywhere in the proto — only in spec prose.

**Impact**: Every SDK must define its own string constants. No compile-time validation. In C# this means creating a static class:
```csharp
public static class ProtocolBinding
{
    public const string JsonRpc = "JSONRPC";
    public const string Grpc = "GRPC";
    public const string HttpJson = "HTTP+JSON";
}
```
And hoping the strings match. In Go, Java, and Python, similar boilerplate is needed.

**Recommendation**: Either:
- (a) Add a `ProtocolBinding` enum with the canonical values and `PROTOCOL_BINDING_UNSPECIFIED`, or
- (b) At minimum, add `// Known values: "JSONRPC", "GRPC", "HTTP+JSON"` as a proto comment so codegen tools can produce constants.

---

### S4.4: HTTP+JSON response examples only show `task` variant, not `message`

**Observation**: All examples across the entire spec (§6.1-§6.8, §11.4) show only the `task` variant of `SendMessageResponse`. The `message` variant (direct message response without task creation) is never exemplified in a concrete example.

Since the `message`-only response is the newer, less intuitive pattern (agent responds directly without creating a task), an explicit example would help implementers understand the complete API surface.

**Recommendation**: Add a `message` variant example alongside the existing `task` examples in §6 or §11.4:

```json
HTTP/1.1 200 OK
Content-Type: application/a2a+json

{
  "message": {
    "messageId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "role": "ROLE_AGENT",
    "parts": [{"text": "The capital of France is Paris."}]
  }
}
```

---

### S4.5: Migration appendix examples use pre-v0.3 `kind` values, not v0.3 values

**§A.2.1 Breaking Change: Kind Discriminator Removed** — The "Legacy Pattern" examples use `"kind": "TextPart"` and `"kind": "FilePart"`, but the actual v0.3.0 format uses lowercase values like `"kind": "text"` and `"kind": "file"`:

```json
// A.2.1 legacy example (shows):
{"kind": "TextPart", "text": "Hello, world!"}

// Actual v0.3.0 format:
{"kind": "text", "text": "Hello, world!"}
```

**Impact**: Minor — the migration guide is informational. But SDK developers using it as a reference for backward compatibility parsing will implement the wrong legacy detection.

**Recommendation**: Update the legacy examples to match actual v0.3.0 wire format.

---

### S4.6: `Part.data` migration path example is confusing (`{"data": {"data": {...}}}`)

**§A.2.1** shows the migration for DataPart as:

```
Legacy:  { "kind": "DataPart", "data": {...} }
Current: { "data": { "data": {...} } }
```

The double-nesting `{"data": {"data": {...}}}` would occur if `Part.data` were a `Struct` (which wraps values in a `{"fields": {...}}` envelope). But `Part.data` is `google.protobuf.Value`, which per ProtoJSON serializes **directly as its JSON value**. So a struct value like `{"key": "val"}` serializes as:

```json
{"data": {"key": "val"}}
```

NOT as:

```json
{"data": {"data": {"key": "val"}}}
```

If the `{"data": {"data": {...}}}` example is intentional (e.g., the inner `"data"` is a user-defined key), it's misleading without explanation.

**Recommendation**: Clarify the DataPart migration example. If the intent is `Part.data = Value(struct({...}))`, the correct v1.0 serialization is `{"data": {...}}` not `{"data": {"data": {...}}}`.

---

## S5 — Informational (design observations, no change necessarily needed)

### S5.1: The spec is well-structured for multi-language implementation

The 3-layer architecture (Data Model → Operations → Bindings) with proto as the single normative source is a strong design. It means:
- Go and Java get proto-generated code for free
- Python, JS, and C# can choose between proto-generated code or hand-written models with ProtoJSON serialization rules
- The mapping between bindings is fully specified in §5.3

The separation of abstract operations from bindings is particularly helpful — a C# SDK implementing only JSON-RPC doesn't need to understand gRPC error handling, and vice versa.

### S5.2: Push notification CRUD is a solid design improvement over v0.3.0

The `Create/Get/List/Delete TaskPushNotificationConfig` operations with proper resource semantics are much cleaner than v0.3.0's single `set` operation. This maps naturally to REST patterns and is straightforward to implement in ASP.NET Core with standard controller/minimal API patterns.

### S5.3: `SendMessageResponse` as task|message union is a good simplification

The ability for agents to return a direct `Message` (for simple queries) or a `Task` (for async work) from the same endpoint is a practical design that avoids forcing all interactions through the task lifecycle. However, it does require SDK clients to handle both variants — in C#, this maps naturally to a discriminated union pattern (e.g., a class with `Task?` and `Message?` properties where exactly one is non-null, plus a helper property indicating which variant is active).

### S5.4: Extension mechanism via URI-keyed metadata is well-designed

The extension pattern — URI identifiers in the `extensions` array, extension data in `metadata` keyed by the same URI — is clean and doesn't require schema changes for new extensions. It works well across all languages and serialization frameworks.

### S5.5: Agent Card signing via RFC 8785 canonicalization + JWS is thorough

The canonicalization rules (§8.4.1) and their interaction with proto field presence semantics are well-thought-out. The requirement to exclude default-valued optional fields before canonicalization is important and correctly specified.

---

## Summary Matrix

| ID | Severity | Title | Languages Most Affected |
|----|----------|-------|------------------------|
| S1.1 | **Critical** | Multiple oneof fields rejection not mandated | Python, JS, C# |
| S1.2 | **Critical** | `data: null` vs absent ambiguity | Python, C#, JS |
| S1.3 | **Critical** | Examples use v0.3.0 formats (AuthenticationInfo, TaskState, URLs) | All |
| S1.4 | **Critical** | `securityRequirements` vs `security` field name mismatch | All |
| S1.5 | **Critical** | `tenant` path parameter mechanism undefined for HTTP bindings | All |
| S2.1 | **Major** | SCREAMING_SNAKE enum prefix ergonomics | C#, JS/TS |
| S2.2 | **Major** | No binding prioritization guidance for SDKs | All |
| S2.3 | **Major** | `Struct` mapping guidance missing | C#, Go, Java |
| S2.4 | **Major** | `ListTasksResponse.totalSize` REQUIRED is impractical at scale | All (server) |
| S2.5 | **Major** | `messageId` required on ephemeral status messages | All (server) |
| S3.1 | **Moderate** | base64 variant unspecified | JS, C# |
| S3.2 | **Moderate** | Oneof validation errors lack error codes | All |
| S3.3 | **Moderate** | `optional` keyword inconsistency | Go, C#, Java |
| S3.4 | **Moderate** | Content-Type inconsistency across bindings and examples | All |
| S3.5 | **Moderate** | Missing `ImplicitOAuthFlow` and `PasswordOAuthFlow` definitions | All |
| S3.6 | **Moderate** | `stateTransitionHistory` in sample but not in schema | All |
| S4.1 | **Minor** | Acronym casing in camelCase fields | All (docs) |
| S4.2 | **Minor** | Reserved fields undocumented in proto | All (migration) |
| S4.3 | **Minor** | `protocol_binding` as string not enum | All |
| S4.4 | **Minor** | Response examples only show `task` variant | All (docs) |
| S4.5 | **Minor** | Migration appendix uses wrong legacy `kind` values | All (migration) |
| S4.6 | **Minor** | DataPart migration example shows incorrect double-nesting | All (migration) |

---

## Positive Observations

This feedback focuses on issues, but it's worth noting that the v1.0 specification represents a **substantial maturation** of the protocol:

1. **Proto as normative source** eliminates the specification drift that plagued v0.3.0 (where the JSON schema and prose often disagreed).
2. **Three-layer architecture** makes the spec approachable — SDK authors can focus on one binding at a time.
3. **Removing `kind` discriminators** in favor of field presence is the right call — it aligns with proto `oneof` semantics and removes an entire class of custom serialization code.
4. **Comprehensive error codes** (§5.4) with cross-binding mappings are excellent. The URI-based error types for HTTP+JSON/REST (RFC 9457) are a best-practice adoption.
5. **Versioning strategy** (§3.6) with per-interface protocol version and `A2A-Version` header is pragmatic and well-specified.
6. **Agent Card signing** (§8.4) with canonicalization rules demonstrates attention to enterprise security requirements.

The issues identified above are largely editorial (inconsistent examples), definitional gaps (missing type definitions, undefined parser behavior), and ergonomic concerns (enum naming). The core architecture is sound.
