# SCREAMING_SNAKE_CASE Enum Serialization for A2A v1.0

> **Date:** February 13, 2026  
> **Context:** A2A v1.0 requires ProtoJSON SCREAMING_SNAKE_CASE enum values with type-prefixed names  
> **Constraint:** Must work across `net9.0`, `net8.0`, and `netstandard2.0` with AOT + source-gen compatibility

---

## Problem

A2A v1.0 requires enum values in SCREAMING_SNAKE_CASE with type-prefixed names per the ProtoJSON specification:

| C# Enum Member | Wire Format (JSON) |
|----------------|-------------------|
| `TaskState.Submitted` | `"TASK_STATE_SUBMITTED"` |
| `TaskState.Working` | `"TASK_STATE_WORKING"` |
| `TaskState.InputRequired` | `"TASK_STATE_INPUT_REQUIRED"` |
| `TaskState.Completed` | `"TASK_STATE_COMPLETED"` |
| `TaskState.Canceled` | `"TASK_STATE_CANCELED"` |
| `TaskState.Failed` | `"TASK_STATE_FAILED"` |
| `TaskState.Rejected` | `"TASK_STATE_REJECTED"` |
| `TaskState.AuthRequired` | `"TASK_STATE_AUTH_REQUIRED"` |
| `TaskState.Unspecified` | `"TASK_STATE_UNSPECIFIED"` |
| `MessageRole.User` | `"ROLE_USER"` |
| `MessageRole.Agent` | `"ROLE_AGENT"` |
| `MessageRole.Unspecified` | `"ROLE_UNSPECIFIED"` |

The current v0.3 SDK uses `KebabCaseLowerJsonStringEnumConverter<TEnum>` which wraps `JsonStringEnumConverter<TEnum>(JsonNamingPolicy.KebabCaseLower)` to produce values like `"input-required"` and `"auth-required"`.

---

## Available STJ Mechanisms

| Mechanism | What it does | Availability | AOT safe |
|-----------|-------------|-------------|----------|
| `[JsonPropertyName]` | Renames class/struct **properties** | All STJ versions | Yes |
| `[JsonStringEnumMemberName]` | Renames individual **enum members** | **.NET 9+ only** | Yes |
| `JsonStringEnumConverter(JsonNamingPolicy)` | Applies a naming policy to all enum members | .NET 8+ (generic form) | Yes (generic form only) |
| `JsonStringEnumConverter` (non-generic) | Reflection-based enum string conversion | All STJ versions | **No — breaks AOT** |
| Custom `JsonConverter<TEnum>` | Full read/write control | All versions | Yes |

### Key Findings

1. **`[JsonPropertyName]` does NOT work on enum members** — it only applies to properties/fields of classes and structs. This is a common misconception.

2. **`[JsonStringEnumMemberName]` is .NET 9+ only** — perfect for net9.0 TFM, but unavailable on net8.0 and netstandard2.0.

3. **`JsonNamingPolicy.SnakeCaseUpper`** (net8.0+) converts `InputRequired` → `INPUT_REQUIRED` but **cannot add a prefix** like `TASK_STATE_`. No built-in naming policy produces the required prefixed format.

4. **`JsonStringEnumConverter<TEnum>`** (the generic, AOT-safe version) was introduced in .NET 9. On net8.0 only the non-generic version exists, which uses reflection and **breaks AOT**.

5. **The current `KebabCaseLowerJsonStringEnumConverter<TEnum>`** inherits from `JsonStringEnumConverter<TEnum>` which is net9+ for AOT — so the current code already has an AOT gap on net8.0/netstandard2.0.

---

## Recommended Implementation

### Approach: Explicit `switch`-based converters

For 2 enums (12 total values), a hardcoded `switch`-based converter is the simplest approach that satisfies all constraints:

- **AOT compatible** — no reflection, no `Type.GetMembers()`, just `switch` expressions
- **Source-gen compatible** — registered via `[JsonConverter]` attribute, source generator sees concrete type
- **All TFMs** — works identically on netstandard2.0, net8.0, and net9.0
- **Fast** — `switch` on strings compiles to hash-based jump tables
- **Idiomatic C# API** — users write `TaskState.Completed`, wire format produces `"TASK_STATE_COMPLETED"`

### TaskState

```csharp
/// <summary>
/// Represents the possible states of a Task.
/// </summary>
[JsonConverter(typeof(TaskStateJsonConverter))]
public enum TaskState
{
    /// <summary>The task state is unspecified.</summary>
    Unspecified,

    /// <summary>The task has been submitted.</summary>
    Submitted,

    /// <summary>The task is currently being worked on.</summary>
    Working,

    /// <summary>The task requires input from the user.</summary>
    InputRequired,

    /// <summary>The task has been completed successfully.</summary>
    Completed,

    /// <summary>The task has been canceled.</summary>
    Canceled,

    /// <summary>The task has failed.</summary>
    Failed,

    /// <summary>The task has been rejected.</summary>
    Rejected,

    /// <summary>The task requires authentication.</summary>
    AuthRequired,
}

internal sealed class TaskStateJsonConverter : JsonConverter<TaskState>
{
    public override TaskState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "TASK_STATE_UNSPECIFIED"    => TaskState.Unspecified,
            "TASK_STATE_SUBMITTED"     => TaskState.Submitted,
            "TASK_STATE_WORKING"       => TaskState.Working,
            "TASK_STATE_INPUT_REQUIRED" => TaskState.InputRequired,
            "TASK_STATE_COMPLETED"     => TaskState.Completed,
            "TASK_STATE_CANCELED"      => TaskState.Canceled,
            "TASK_STATE_FAILED"        => TaskState.Failed,
            "TASK_STATE_REJECTED"      => TaskState.Rejected,
            "TASK_STATE_AUTH_REQUIRED"  => TaskState.AuthRequired,
            _ => throw new JsonException($"Unknown TaskState value: '{value}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, TaskState value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            TaskState.Unspecified  => "TASK_STATE_UNSPECIFIED",
            TaskState.Submitted    => "TASK_STATE_SUBMITTED",
            TaskState.Working      => "TASK_STATE_WORKING",
            TaskState.InputRequired => "TASK_STATE_INPUT_REQUIRED",
            TaskState.Completed    => "TASK_STATE_COMPLETED",
            TaskState.Canceled     => "TASK_STATE_CANCELED",
            TaskState.Failed       => "TASK_STATE_FAILED",
            TaskState.Rejected     => "TASK_STATE_REJECTED",
            TaskState.AuthRequired => "TASK_STATE_AUTH_REQUIRED",
            _ => throw new JsonException($"Unknown TaskState value: '{value}'")
        });
    }
}
```

### MessageRole

```csharp
/// <summary>
/// Message sender's role.
/// </summary>
[JsonConverter(typeof(MessageRoleJsonConverter))]
public enum MessageRole
{
    /// <summary>Unspecified role.</summary>
    Unspecified,

    /// <summary>User role — communication from client to server.</summary>
    User,

    /// <summary>Agent role — communication from server to client.</summary>
    Agent,
}

internal sealed class MessageRoleJsonConverter : JsonConverter<MessageRole>
{
    public override MessageRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "ROLE_UNSPECIFIED" => MessageRole.Unspecified,
            "ROLE_USER"        => MessageRole.User,
            "ROLE_AGENT"       => MessageRole.Agent,
            _ => throw new JsonException($"Unknown MessageRole value: '{value}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, MessageRole value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            MessageRole.Unspecified => "ROLE_UNSPECIFIED",
            MessageRole.User        => "ROLE_USER",
            MessageRole.Agent       => "ROLE_AGENT",
            _ => throw new JsonException($"Unknown MessageRole value: '{value}'")
        });
    }
}
```

---

## Alternatives Considered

### Alternative 1: `[JsonStringEnumMemberName]` (net9+ only)

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<TaskState>))]
public enum TaskState
{
    [JsonStringEnumMemberName("TASK_STATE_UNSPECIFIED")]
    Unspecified,

    [JsonStringEnumMemberName("TASK_STATE_SUBMITTED")]
    Submitted,
    // ...
}
```

**Pros:** Zero custom code, fully declarative, AOT-safe on net9.  
**Cons:** `JsonStringEnumMemberName` and generic `JsonStringEnumConverter<T>` are .NET 9+ only. Would require `#if NET9_0_OR_GREATER` conditional compilation and a fallback converter for net8.0/netstandard2.0 — adding complexity for no benefit.

### Alternative 2: Generic converter with reflection

```csharp
internal sealed class ScreamingSnakeEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Dictionary<string, TEnum> s_read;
    private static readonly Dictionary<TEnum, string> s_write;

    static ScreamingSnakeEnumConverter()
    {
        // Build mappings via reflection: Enum.GetNames(), string manipulation
    }
}
```

**Pros:** Single reusable converter for any enum, automatic prefix derivation.  
**Cons:** Uses reflection in the static constructor (`Enum.GetNames()`, string manipulation) — **breaks AOT**. Could be made AOT-safe with a source generator, but that's overkill for 2 enums.

### Alternative 3: `JsonNamingPolicy.SnakeCaseUpper` (net8+)

```csharp
// Would produce: "INPUT_REQUIRED" — no prefix
new JsonStringEnumConverter<TaskState>(JsonNamingPolicy.SnakeCaseUpper)
```

**Pros:** Built-in, no custom code.  
**Cons:** Produces `"INPUT_REQUIRED"` not `"TASK_STATE_INPUT_REQUIRED"`. Cannot add the required type prefix. Also `JsonStringEnumConverter<T>` generic form is net9+ only.

### Alternative 4: Conditional compilation

```csharp
#if NET9_0_OR_GREATER
[JsonStringEnumMemberName("TASK_STATE_SUBMITTED")]
#endif
Submitted,
```

Plus a custom converter for downlevel TFMs that reads the same attributes via reflection at startup.

**Pros:** Attribute is the single source of truth.  
**Cons:** Complex build setup; reflection fallback still breaks AOT on net8.0; the `[JsonStringEnumMemberName]` attribute type doesn't exist on net8.0 so you'd also need to define a polyfill attribute.

---

## Comparison: Current vs. Proposed

| Aspect | Current (v0.3) | Proposed (v1.0) |
|--------|---------------|-----------------|
| Converter | `KebabCaseLowerJsonStringEnumConverter<TEnum>` | `TaskStateJsonConverter` / `MessageRoleJsonConverter` |
| Base class | `JsonStringEnumConverter<TEnum>` (net9+ for generic) | `JsonConverter<TEnum>` (all TFMs) |
| Mapping | Naming policy: `InputRequired` → `input-required` | Explicit switch: `InputRequired` → `TASK_STATE_INPUT_REQUIRED` |
| AOT safe | Only on net9.0 (generic `JsonStringEnumConverter<T>`) | All TFMs ✓ |
| Source-gen | Works (attribute-based registration) | Works (attribute-based registration) ✓ |
| Lines of code | 3 (converter class) | ~25 per enum converter |
| Extensibility | Any enum via generic | Per-enum (but only 2 enums exist) |
| Prefix support | N/A (not needed for v0.3) | Explicit per-member ✓ |

---

## Migration Steps

1. **Delete** `KebabCaseLowerJsonStringEnumConverter.cs`
2. **Create** `TaskStateJsonConverter.cs` with the `switch`-based implementation above
3. **Create** `MessageRoleJsonConverter.cs` with the `switch`-based implementation above
4. **Update** `TaskState.cs` — change `[JsonConverter]` attribute, add `Unspecified` member, reorder
5. **Update** `AgentMessage.cs` — change `MessageRole` enum: add `Unspecified`, update `[JsonConverter]` attribute
6. **Update** `A2AJsonUtilities.cs` — no changes needed (source-gen picks up `[JsonConverter]` attribute)
7. **Update tests** — all existing enum serialization tests need updated expected values
8. **Register in source-gen context** — verify `JsonContext` includes the enum types (already does via model types)

---

## Decision

**Use explicit `switch`-based converters.** The rationale:

- Only 2 enums with 12 total values — generic abstractions add complexity without proportional benefit
- AOT + source-gen compatibility on all 3 TFMs without conditional compilation
- The type-prefix pattern (`TASK_STATE_`, `ROLE_`) is not derivable from a naming policy — explicit mapping is required regardless
- `switch` expressions compile to efficient hash-based lookups, matching or exceeding dictionary performance
- Follows the existing project pattern of per-type converters (see `AgentTransportConverter`, `FileContent.Converter`)
