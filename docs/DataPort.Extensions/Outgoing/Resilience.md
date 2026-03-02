# Resilience Components

`RetryBackoffCalculator` and `IRetryPolicy` implementations live in `ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.Retry`. `IDelayProvider` lives in `ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing`.

## Components

### RetryBackoffCalculator

**Location**: `src/DataPort.Extensions/Outgoing/QueueProcessing/Retry/RetryBackoffCalculator.cs`

Stateless utility that calculates retry delays with exponential or linear backoff.

```csharp
// Exponential backoff with jitter (default)
var delay = RetryBackoffCalculator.CalculateDelay(
    retryCount: 3,
    initialDelayMs: 100,
    maxDelayMs: 5000,
    exponential: true
);
// Result: ~800ms (2^3 * 100ms) + 10% jitter, capped at 5000ms

// Linear backoff
var delay = RetryBackoffCalculator.CalculateDelay(
    retryCount: 3,
    initialDelayMs: 100,
    maxDelayMs: 5000,
    exponential: false
);
// Result: 300ms (3 * 100ms)
```

**Algorithm**:

| Mode | Formula |
|------|---------|
| Exponential | `min(2^retryCount * initialDelay + jitter, maxDelay)` — jitter is `random(0, delay * 0.1)` |
| Linear | `min(retryCount * initialDelay, maxDelay)` |

Use exponential for transient failures (network blips), linear when delays are predictable (rate limiting, maintenance windows).

---

### IDelayProvider

**Location**: `src/DataPort.Extensions/Outgoing/QueueProcessing/IDelayProvider.cs`

Abstracts `Task.Delay` to allow tests to skip actual waits. Inject `IDelayProvider` (optional, defaults to `Task.Delay`) via the `OutgoingDataPortBase` constructor.

```csharp
// Test implementation — no actual delay
public class FakeDelayProvider : IDelayProvider
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

---

## Related Components

- **DefaultResiliencePipeline** (`src/DataPort.Extensions/Incoming/Polling/Job/DefaultResiliencePipeline.cs`): Polly-based circuit breaker + timeout for periodic jobs
- **IRetryPolicy** — see [Interfaces.md](../Interfaces.md#iretrypolicy)

---

**Last Updated**: 2026-02-23
