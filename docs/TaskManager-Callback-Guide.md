# TaskManager Callback Configuration Guide

## Overview

The `TaskManager` class provides several callbacks to handle different stages of message and task processing. Understanding how these callbacks interact is crucial for implementing your agent correctly.

## Callback Types

The `TaskManager` provides four main callbacks for message and task processing:

1. **`OnMessageReceived`** - Handles incoming messages directly
2. **`OnTaskCreated`** - Called when a new task is created
3. **`OnTaskCancelled`** - Called when a task is cancelled
4. **`OnTaskUpdated`** - Called when an existing task is updated

## Important: Callback Precedence

### OnMessageReceived vs OnTaskCreated

When processing a **new message** (one without an existing `TaskId`), the `TaskManager` follows this priority:

1. **If `OnMessageReceived` is set** ? It is called, and `OnTaskCreated` is **NOT** called
2. **If `OnMessageReceived` is null** ? A new task is automatically created, and `OnTaskCreated` is called

This means:
- `OnMessageReceived` and `OnTaskCreated` are **mutually exclusive** for new messages
- Setting both callbacks will result in `OnTaskCreated` never being called for new messages
- You should choose **one approach** based on your agent's requirements

### When Each Callback Is Invoked

| Callback | Invoked When |
|----------|-------------|
| `OnMessageReceived` | New message arrives **without** existing TaskId (if set) |
| `OnTaskCreated` | New message arrives **without** existing TaskId (if `OnMessageReceived` is null) |
| `OnTaskUpdated` | Message arrives **with** existing TaskId (always called regardless of `OnMessageReceived`) |
| `OnTaskCancelled` | Task cancellation is requested (always called regardless of `OnMessageReceived`) |

## Usage Patterns

### Pattern 1: Message-Based Agent (Simple)

Use `OnMessageReceived` when your agent:
- Responds immediately to messages
- Doesn't need task tracking for every request
- May conditionally create tasks for complex operations

```csharp
var taskManager = new TaskManager();

taskManager.OnMessageReceived = async (messageSendParams, cancellationToken) =>
{
    var userMessage = messageSendParams.Message.Parts.OfType<TextPart>().First().Text;
    
    // Simple queries return messages directly
    if (userMessage.StartsWith("What is"))
    {
        return new AgentMessage
        {
            Parts = [new TextPart { Text = $"Answer to: {userMessage}" }]
        };
    }
    
    // Complex operations create tasks
    if (userMessage.StartsWith("Analyze"))
    {
        var task = await taskManager.CreateTaskAsync(cancellationToken: cancellationToken);
        // Process async in background...
        return task;
    }
    
    return new AgentMessage { Parts = [new TextPart { Text = "Unknown request" }] };
};
```

### Pattern 2: Task-Based Agent (Traditional)

Use `OnTaskCreated` when your agent:
- Always creates tasks for all requests
- Needs to track all operations
- Uses task lifecycle for state management

```csharp
var taskManager = new TaskManager();

// Leave OnMessageReceived as null (default)
// OnTaskCreated will be called for all new messages

taskManager.OnTaskCreated = async (task, cancellationToken) =>
{
    // Start processing the task
    await taskManager.UpdateStatusAsync(task.Id, TaskState.Working, cancellationToken: cancellationToken);
    
    // Do work...
    var result = await ProcessTaskAsync(task);
    
    // Complete the task
    await taskManager.UpdateStatusAsync(
        task.Id, 
        TaskState.Completed, 
        message: new AgentMessage { Parts = [new TextPart { Text = result }] },
        final: true,
        cancellationToken: cancellationToken);
};

taskManager.OnTaskUpdated = async (task, cancellationToken) =>
{
    // Handle updates to existing tasks
    await ContinueProcessingAsync(task, cancellationToken);
};

taskManager.OnTaskCancelled = async (task, cancellationToken) =>
{
    // Clean up resources for cancelled tasks
    await CleanupAsync(task, cancellationToken);
};
```

### Pattern 3: Hybrid Approach (Advanced)

Use `OnMessageReceived` that conditionally creates tasks:

```csharp
var taskManager = new TaskManager();

taskManager.OnMessageReceived = async (messageSendParams, cancellationToken) =>
{
    var input = messageSendParams.Message.Parts.OfType<TextPart>().First().Text;
    
    // Determine if this requires task tracking
    bool needsTask = IsComplexOperation(input);
    
    if (needsTask)
    {
        // Create task and let OnTaskCreated handle it
        var task = await taskManager.CreateTaskAsync(cancellationToken: cancellationToken);
        task.History = [messageSendParams.Message];
        
        // This will trigger OnTaskCreated to be called
        await taskManager.OnTaskCreated(task, cancellationToken);
        
        return task;
    }
    else
    {
        // Handle simple requests directly
        return new AgentMessage 
        { 
            Parts = [new TextPart { Text = await ProcessSimpleRequestAsync(input) }] 
        };
    }
};

taskManager.OnTaskCreated = async (task, cancellationToken) =>
{
    // Only called when OnMessageReceived explicitly creates a task
    await ProcessComplexOperationAsync(task, cancellationToken);
};
```

## Common Pitfalls

### ? Setting Both Callbacks Without Understanding

```csharp
// BAD: OnTaskCreated will never be called for new messages!
taskManager.OnMessageReceived = (msg, ct) => Task.FromResult<A2AResponse>(new AgentMessage());
taskManager.OnTaskCreated = (task, ct) => Task.CompletedTask; // Never called!
```

### ? Choose One Approach

```csharp
// GOOD: Choose one approach
taskManager.OnMessageReceived = (msg, ct) => /* handle directly */;
// OR
taskManager.OnTaskCreated = (task, ct) => /* handle tasks */;
```

## Decision Guide

**Use `OnMessageReceived` when:**
- ? You want direct control over response types
- ? Not all requests need task tracking
- ? You need to return immediate responses for simple queries
- ? Your agent supports both stateless and stateful operations

**Use `OnTaskCreated` when:**
- ? All operations should be tracked as tasks
- ? You need consistent task lifecycle management
- ? Your agent primarily handles long-running operations
- ? You want simpler callback logic without conditional task creation

**Always use `OnTaskUpdated` and `OnTaskCancelled`:**
- ? These are orthogonal to `OnMessageReceived` vs `OnTaskCreated`
- ? They handle operations on existing tasks
- ? They work the same way regardless of your initial message handling approach

## Summary

The key insight is that `OnMessageReceived` and `OnTaskCreated` serve **different architectural patterns**:

- **`OnMessageReceived`**: Flexible, message-first approach with optional task creation
- **`OnTaskCreated`**: Structured, task-first approach with automatic task creation

Choose the pattern that best fits your agent's requirements, but **don't set both expecting them to work together for new messages** - they won't. The TaskManager will always prioritize `OnMessageReceived` when it's set.
