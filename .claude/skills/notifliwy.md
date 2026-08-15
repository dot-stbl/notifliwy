---
description: Complete developer guide for Notifliwy — .NET event notification library with fluent API for pipeline-based event processing. Covers all components, patterns, and use cases.
---

# notifliwy

Complete developer guide for Notifliwy library.

## When to Use This Skill

Use when working with Notifliwy for:
- Adding new notification pipelines
- Implementing conditions, mappers, steps, or exporters
- Configuring the notification server
- Debugging pipeline flow
- Understanding event-to-notification mapping
- Integrating providers (Kafka, In-Memory)

## Core Concept: Pipeline Flow

```
Event → InputPipe → Connector → Sector → Condition → Mapper → Steps → Exporter
```

| Stage | Interface | Purpose | Required |
|-------|-----------|---------|----------|
| Input | `IInputPipe<TEvent>` | Provides events via `AcceptAsync()` | Yes |
| Condition | `INotificationCondition<TN,TE>` | Filter events (return `false` to stop) | No |
| Mapper | `INotificationMapper<TN,TE>` | Convert event → notification | **Yes** |
| Steps | `INotificationStep<TN>` | Transform notification (chain multiple) | No |
| Exporter | `INotificationExporter<TN>` | Final output handler | No |

---

## Quick Start

### Minimal Setup

```csharp
builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddNotification<MyNotification, MyEvent>(sector =>
    {
        sector.AddMapper<MyMapper>(); // Required!
    });
    serverBuilder.AddInMemoryInput();
});
```

### Full Pipeline

```csharp
serverBuilder.AddNotification<MyNotification, MyEvent>(sector =>
{
    sector.AddCondition<MyCondition>();      // Optional: filter
    sector.AddMapper<MyMapper>();             // Required: convert
    sector.WithPipeline(pipeline =>          // Optional: transform
    {
        pipeline.AddStep<Step1>();
        pipeline.AddStep<Step2>();
    });
    sector.AddExporter<MyExporter>();        // Optional: output
});
```

---

## Interface Signatures

### INotificationCondition

Filter events. Return `false` to stop pipeline.

```csharp
public interface INotificationCondition<TNotification, TEvent>
{
    ValueTask<bool> AllowItAsync(TEvent inputEvent, CancellationToken cancellationToken = default);
}
```

**Example:** Only allow events with even ID:

```csharp
public sealed class EvenIdCondition : INotificationCondition<Notification, Event>
{
    public ValueTask<bool> AllowItAsync(Event input, CancellationToken ct = default)
    {
        return ValueTask.FromResult(input.Id % 2 == 0);
    }
}
```

---

### INotificationMapper

Convert event to notification. **Required** — without it pipeline cannot work.

```csharp
public interface INotificationMapper<TNotification, TEvent>
{
    ValueTask<TNotification> ConvertAsync(TEvent inputEvent, CancellationToken cancellationToken = default);
}
```

**Example:** Map OrderEvent to OrderNotification:

```csharp
public sealed class OrderEventMapper : INotificationMapper<OrderNotification, OrderEvent>
{
    public ValueTask<OrderNotification> ConvertAsync(OrderEvent input, CancellationToken ct = default)
    {
        return ValueTask.FromResult(new OrderNotification
        {
            OrderId = input.Id,
            Amount = input.Total,
            CustomerEmail = input.Customer.Email,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
```

---

### INotificationStep

Transform notification. Multiple steps execute sequentially in order added.

```csharp
public interface INotificationStep<TNotification>
{
    ValueTask<TNotification> AggregateAsync(TNotification notification, CancellationToken cancellationToken = default);
}
```

**Example:** Enrich notification with timestamp:

```csharp
public sealed class TimestampEnrichmentStep : INotificationStep<OrderNotification>
{
    public ValueTask<OrderNotification> AggregateAsync(OrderNotification notification, CancellationToken ct = default)
    {
        notification.ProcessedAt = DateTimeOffset.UtcNow;
        return ValueTask.FromResult(notification);
    }
}
```

**Example:** Validate notification:

```csharp
public sealed class ValidationStep : INotificationStep<OrderNotification>
{
    public ValueTask<OrderNotification> AggregateAsync(OrderNotification notification, CancellationToken ct = default)
    {
        if (notification.Amount <= 0)
        {
            throw new InvalidOperationException("Invalid amount");
        }
        return ValueTask.FromResult(notification);
    }
}
```

---

### INotificationExporter

Final output handler. Send notification to external system.

```csharp
public interface INotificationExporter<TNotification>
{
    ValueTask ThrowAsync(TNotification notification, CancellationToken cancellationToken = default);
}
```

**Example:** Send to email:

```csharp
public sealed class EmailExporter : INotificationExporter<OrderNotification>
{
    private readonly IEmailService _emailService;

    public EmailExporter(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public ValueTask ThrowAsync(OrderNotification notification, CancellationToken ct = default)
    {
        _emailService.Send(notification.CustomerEmail, "Order Confirmation", notification.ToString());
        return ValueTask.CompletedTask;
    }
}
```

**Example:** Log notification:

```csharp
public sealed class LoggingExporter : INotificationExporter<Notification>
{
    private readonly ILogger<LoggingExporter> _logger;

    public LoggingExporter(ILogger<LoggingExporter> logger)
    {
        _logger = logger;
    }

    public ValueTask ThrowAsync(Notification notification, CancellationToken ct = default)
    {
        _logger.LogInformation("Notification {Type}: {Data}", typeof(Notification).Name, notification);
        return ValueTask.CompletedTask;
    }
}
```

---

## Use Cases

### Use Case 1: Simple Event → Notification

```csharp
// Event from external system
public record OrderCreatedEvent(int Id, decimal Total, string CustomerEmail);

// Notification to send
public record OrderNotification
{
    public int OrderId { get; init; }
    public decimal Amount { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

// Mapper
public class OrderCreatedMapper : INotificationMapper<OrderNotification, OrderCreatedEvent>
{
    public ValueTask<OrderNotification> ConvertAsync(OrderCreatedEvent input, CancellationToken ct = default)
    {
        return ValueTask.FromResult(new OrderNotification
        {
            OrderId = input.Id,
            Amount = input.Total,
            CustomerEmail = input.CustomerEmail,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}

// Registration
builder.Services.AddNotifliwyServer(sb =>
{
    sb.AddNotification<OrderNotification, OrderCreatedEvent>(sector =>
    {
        sector.AddMapper<OrderCreatedMapper>();
    });
    sb.AddInMemoryInput();
});
```

### Use Case 2: Event → Notification with Filtering

```csharp
// High-value orders only
public class HighValueFilter : INotificationCondition<OrderNotification, OrderCreatedEvent>
{
    public ValueTask<bool> AllowItAsync(OrderCreatedEvent input, CancellationToken ct = default)
    {
        return ValueTask.FromResult(input.Total >= 1000);
    }
}

builder.Services.AddNotifliwyServer(sb =>
{
    sb.AddNotification<OrderNotification, OrderCreatedEvent>(sector =>
    {
        sector.AddCondition<HighValueFilter>(); // Only orders >= 1000
        sector.AddMapper<OrderCreatedMapper>();
    });
    sb.AddInMemoryInput();
});
```

### Use Case 3: Event → Notification with Multiple Steps

```csharp
// Step 1: Add metadata
public class MetadataEnrichmentStep : INotificationStep<OrderNotification>
{
    public ValueTask<OrderNotification> AggregateAsync(OrderNotification notification, CancellationToken ct = default)
    {
        notification.Metadata["source"] = "notifliwy";
        notification.Metadata["processed_at"] = DateTimeOffset.UtcNow.ToString("O");
        return ValueTask.FromResult(notification);
    }
}

// Step 2: Calculate fees
public class FeeCalculationStep : INotificationStep<OrderNotification>
{
    public ValueTask<OrderNotification> AggregateAsync(OrderNotification notification, CancellationToken ct = default)
    {
        notification.Fee = notification.Amount * 0.05m; // 5% fee
        return ValueTask.FromResult(notification);
    }
}

// Registration
builder.Services.AddNotifliwyServer(sb =>
{
    sb.AddNotification<OrderNotification, OrderCreatedEvent>(sector =>
    {
        sector.AddMapper<OrderCreatedMapper>();
        sector.WithPipeline(pipeline =>
        {
            pipeline.AddStep<MetadataEnrichmentStep>();
            pipeline.AddStep<FeeCalculationStep>();
        });
    });
    sb.AddInMemoryInput();
});
```

### Use Case 4: Multiple Exporters (Fan-Out)

```csharp
public class EmailExporter : INotificationExporter<OrderNotification> { /* ... */ }
public class SmsExporter : INotificationExporter<OrderNotification> { /* ... */ }
public class WebhookExporter : INotificationExporter<OrderNotification> { /* ... */ }

builder.Services.AddNotifliwyServer(sb =>
{
    sb.AddNotification<OrderNotification, OrderCreatedEvent>(sector =>
    {
        sector.AddMapper<OrderCreatedMapper>();
        sector.AddExporter<EmailExporter>();   // All three receive
        sector.AddExporter<SmsExporter>();     // the same
        sector.AddExporter<WebhookExporter>();  // notification
    });
    sb.AddInMemoryInput();
});
```

### Use Case 5: Multiple Sectors (Same Event → Different Notifications)

```csharp
// Email notification
builder.Services.AddNotifliwyServer(sb =>
{
    sb.AddNotification<EmailNotification, OrderEvent>(sector =>
    {
        sector.AddMapper<OrderToEmailMapper>();
        sector.AddExporter<EmailExporter>();
    });

    sb.AddNotification<SmsNotification, OrderEvent>(sector =>
    {
        sector.AddMapper<OrderToSmsMapper>();
        sector.AddExporter<SmsExporter>();
    });
    sb.AddInMemoryInput();
});
```

### Use Case 6: No Exporter (Logging Only)

If no exporter is added, notification is logged as JSON:

```csharp
sectorBuilder.AddMapper<MyMapper>();
// No AddExporter call

// Result: notification.ToString() logged via ILogger
logger.LogInformation(JsonSerializer.Serialize(aggregatedNotification));
```

### Use Case 7: Integration with Dependency Injection

All components support DI:

```csharp
public class EmailService { /* ... */ }
public class EmailExporter : INotificationExporter<Notification>
{
    private readonly EmailService _emailService;
    public EmailExporter(EmailService emailService) => _emailService = emailService;
    public ValueTask ThrowAsync(Notification n, CancellationToken ct) => /* use emailService */;
}

// Registration — EmailExporter resolved from DI
builder.Services.AddScoped<EmailService>();
builder.Services.AddNotifliwyServer(sb =>
{
    sb.AddNotification<Notification, Event>(sector =>
    {
        sector.AddMapper<MyMapper>();
        sector.AddExporter<EmailExporter>(); // DI resolves EmailService automatically
    });
    sb.AddInMemoryInput();
});
```

---

## Provider Pattern

### Built-in: In-Memory

Uses `System.Threading.Channels`:

```csharp
sb.AddInMemoryInput(); // Adds InMemoryEventExchange, InMemoryInputPipe, InMemoryExportPipe
```

### External: Kafka via MassTransit

```csharp
// In MassTransit configuration
registrationConfigurator.AddNotifliwyPipe<MyEvent>();
endpoint.ConfigureNotifliwyPipe<MyEvent>(pipe =>
{
    pipe.AddNotification<MyNotification, MyEvent>(sector =>
    {
        sector.AddMapper<MyMapper>();
        sector.AddExporter<MyExporter>();
    });
});
```

---

## Processing Model

- **Sectors process events in parallel** — `Parallel.ForEachAsync` across sectors
- **Pipelines within sector run sequentially** — steps execute in order
- **Each event gets its own DI scope** — fresh instances per event
- **Fire-and-forget** — sectors run via `Task.Run`, errors logged in SectorBlock

---

## Anti-Patterns

### Missing Mapper

```csharp
// ❌ WRONG — Mapper is required!
sector.AddCondition<MyCondition>();
// sector.AddMapper<MyMapper>(); // FORGOT!
// sector.AddExporter<MyExporter>();

// ✅ CORRECT
sector.AddMapper<MyMapper>(); // Required!
```

### Wrong CancellationToken Position

```csharp
// ❌ WRONG
public ValueTask<bool> AllowItAsync(CancellationToken ct, Event input)

// ✅ CORRECT
public ValueTask<bool> AllowItAsync(Event input, CancellationToken ct = default)
```

### String Interpolation in Logging

```csharp
// ❌ WRONG
logger.LogInformation($"Processing event {event.Id}");

// ✅ CORRECT
logger.LogInformation("Processing event {EventId}", event.Id);
```

### Single-Letter Lambda Parameters

```csharp
// ❌ WRONG
events.Where(e => e.IsActive).Select(m => m.Value).ToList();

// ✅ CORRECT
events.Where(notif => notif.IsActive).Select(mapper => mapper.OutputType).ToList();
```

---

## Debugging

### Check Pipeline Flow

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Notifliwy": "Debug"
    }
  }
}
```

Expected output:
- `"Event {hash} / {type} allowed, continue processing"` — condition passed
- `"Notification {Type} exported"` — exporter called
- `"Notification sector failed with exception"` — error in sector

### Verify Sector Registration

```csharp
// In NotificationServerBuilder, the AssignedNotifications dictionary tracks registrations
// Check logs: "Assigned sectors: {count}"
```

### Check Activity Tracing

```csharp
// Enable Activity Source tracing
using var activity = DiagnosticActivity.NotifliwySource.StartConnectorActivity<TEvent>();
// Activities available: Connector, Sector
```

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `Builders/NotificationServerBuilder.cs` | Root builder — entry point for DI registration |
| `Builders/NotificationSectorBuilder.cs` | Per-notification configuration |
| `Connectors/NotificationConnector.cs` | Background service — processes events |
| `Contexts/NotificationSector.cs` | Executes pipeline for single event |
| `Contexts/SectorBlock.cs` | Holds resolved components, orchestrates flow |
| `Pipes/InMemory/InMemoryInputPipe.cs` | Provides events from channel |
| `Pipes/InMemory/InMemoryExportPipe.cs` | Writes events to channel |
| `Pipes/InMemory/InMemoryEventExchange.cs` | Channel-based queue |
| `Steps/Interfaces/INotificationPipeline.cs` | Chains multiple steps |
| `Related/MultiplyServiceInstance.cs` | Holds single or multiple instances |

---

## Coding Conventions

- **Primary constructors** — Use C# 12 syntax
- **Async method naming** — Must end with `Async`
- **CancellationToken** — Always last parameter with `= default`
- **Structured logging** — Use named parameters: `LogInformation("Value {Value}", value)`
- **No private methods** — Use file static classes or local functions
- **Pattern matching** — Prefer `is {}` for null checks

See [`.claude/rules/`](.claude/rules/) for full coding standards.

---

## Known Issues

See [docs/BUGS.md](docs/BUGS.md) for bug reports and fix status.