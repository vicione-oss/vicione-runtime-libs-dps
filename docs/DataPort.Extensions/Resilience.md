# Resilience Components

**Namespace**: `ViciOne.Suite.DataPort.Extensions.Resilience`

Provides retry and backoff calculation utilities for building resilient communication patterns.

## Overview

The Resilience components complement the PeriodicJob resilience pipeline with standalone retry calculation utilities.

## Components

### RetryBackoffCalculator

**Location**: `src/Extension/Resilience/RetryBackoffCalculator.cs`

**Purpose**: Calculates retry delays with exponential or linear backoff strategies

**Key Features**:
- **Exponential backoff**: 2^retryCount with 10% jitter
- **Linear backoff**: retryCount * initialDelay
- **Bounded delays**: Configurable maximum delay cap
- **Jitter**: Random variation to prevent thundering herd
- **Stateless**: Pure calculation function

**Usage**:

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

**Exponential Backoff**:
```
delay = min(2^retryCount * initialDelay + jitter, maxDelay)
jitter = random(0, delay * 0.1)
```

**Linear Backoff**:
```
delay = min(retryCount * initialDelay, maxDelay)
```

**Parameters**:
- `retryCount`: Current retry attempt (0-based)
- `initialDelayMs`: Base delay in milliseconds
- `maxDelayMs`: Maximum delay cap in milliseconds
- `exponential`: Use exponential (true) or linear (false) backoff

**Returns**: `TimeSpan` representing the calculated delay

### IDelayProvider

**Location**: `src/Extension/Resilience/IDelayProvider.cs`

**Purpose**: Abstraction for delay operations (testability)

**Usage**: Enables testing of retry logic without real time delays:

```csharp
public interface IDelayProvider
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

// Production implementation
public class RealDelayProvider : IDelayProvider
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        => Task.Delay(delay, cancellationToken);
}

// Test implementation
public class FakeDelayProvider : IDelayProvider
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        => Task.CompletedTask; // No actual delay
}
```

## Retry Pattern Example

Combining RetryBackoffCalculator with retry logic:

```csharp
public async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    int maxRetries,
    IDelayProvider delayProvider,
    CancellationToken cancellationToken)
{
    for (int retry = 0; retry <= maxRetries; retry++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (retry < maxRetries)
        {
            var delay = RetryBackoffCalculator.CalculateDelay(
                retry,
                initialDelayMs: 100,
                maxDelayMs: 5000,
                exponential: true
            );

            logger.LogWarning(ex, "Operation failed, retrying in {Delay}ms", delay.TotalMilliseconds);
            await delayProvider.DelayAsync(delay, cancellationToken);
        }
    }

    throw new Exception("Max retries exceeded");
}
```

## Design Considerations

### Why Jitter?

Random jitter (10% variation) prevents "thundering herd" scenarios where multiple retrying clients all retry at exactly the same time after a failure, potentially overwhelming the recovering service.

**Scenario**: Imagine 50 PLC polling connections that all fail simultaneously due to a network outage. Without jitter:
- All 50 connections calculate the same retry delay (e.g., 2^1 * 100ms = 200ms)
- All 50 connections retry at exactly the same moment
- The PLC receives 50 simultaneous connection attempts, creating a load spike
- The load spike may cause failures, triggering another synchronized retry wave

**With 10% jitter**:
- Delays vary between 200ms and 220ms (200ms ± 10%)
- Connection attempts are distributed across a 20ms window
- PLC sees gradual ramp-up instead of instant spike
- Recovery is smoother and more reliable

### Why Random.Shared for Jitter?

The `RetryBackoffCalculator` uses `Random.Shared` (not cryptographic random) for jitter calculation. This is a deliberate design choice:

**Security Consideration**: The CA5394 code analyzer warning "Do not use insecure randomness" is suppressed because:

1. **Not a cryptographic use case**: Jitter is used for timing variance in distributed systems, not for security-sensitive operations like:
   - Token generation
   - Password generation
   - Encryption keys
   - Session IDs
   - Authentication challenges

2. **Predictability is not a security risk**: Even if an attacker could predict the jitter values, there is no security exploit:
   - The attacker cannot gain unauthorized access
   - The attacker cannot bypass authentication
   - The attacker cannot decrypt data
   - The worst case is synchronized retries (the problem we're trying to avoid)

3. **Performance**: `Random.Shared` is:
   - Thread-safe without allocation
   - Faster than `System.Security.Cryptography.RandomNumberGenerator`
   - Suitable for hot paths where retry calculations may happen frequently

**When to use cryptographic random**: Use `RandomNumberGenerator` for:
- Generating tokens or passwords
- Creating encryption keys or IVs
- Session management
- Any operation where predictability could be exploited

**Reference**: See [OWASP Secure Coding Practices](https://owasp.org/www-project-secure-coding-practices-quick-reference-guide/) for guidance on when cryptographic randomness is required.

### Why Capped Delays?

Maximum delay caps prevent unbounded wait times. For PLC communication, waiting more than a few seconds is typically pointless - better to fail fast and alert operators.

**Typical PLC scenarios**:
- **Transient network blip**: Resolves in <1 second, exponential backoff recovers quickly
- **PLC reboot**: Takes 30-60 seconds, but connection should timeout and circuit breaker should open
- **Network failure**: May take minutes/hours, indefinite retries waste resources

A `maxDelayMs` of 5000ms (5 seconds) balances between:
- Giving transient failures time to resolve
- Not wasting resources on persistent failures
- Allowing circuit breaker patterns to take over

### Exponential vs Linear

- **Exponential**: Better for transient failures (network blips, temporary overload)
  - Backs off quickly to reduce load on stressed system
  - Example: PLC temporarily overloaded with requests

- **Linear**: Better for predictable delays (rate limiting, scheduled maintenance)
  - Consistent retry cadence for expected delays
  - Example: PLC in maintenance mode with known duration

## Testing Considerations

### Non-Deterministic Jitter

The random jitter makes exact delay values non-deterministic, which can complicate testing. Strategies for testing retry logic:

**Approach 1: Test delay ranges**
```csharp
[Fact]
public void CalculateDelay_WithRetryCount2_ReturnsDelayInExpectedRange()
{
    var delay = RetryBackoffCalculator.CalculateDelay(
        retryCount: 2,
        initialDelayMs: 100,
        maxDelayMs: 10000);

    // Base delay: 2^2 * 100 = 400ms
    // With 10% jitter: 400-440ms
    Assert.InRange(delay.TotalMilliseconds, 400, 440);
}
```

**Approach 2: Test retry behavior, not exact delays**
```csharp
[Fact]
public async Task RetryLogic_RetriesOnFailure_UntilSuccess()
{
    var attemptCount = 0;
    var delayProvider = new InstantDelayProvider(); // No actual delays

    await ExecuteWithRetryAsync(
        operation: async () => {
            attemptCount++;
            if (attemptCount < 3) throw new TransientException();
            return "Success";
        },
        maxRetries: 5,
        delayProvider: delayProvider,
        CancellationToken.None);

    Assert.Equal(3, attemptCount);
    // Test completed in <1ms instead of waiting for exponential backoff
}
```

**Approach 3: Focus on boundary conditions**
```csharp
[Fact]
public void CalculateDelay_RespectsMaxDelay()
{
    // Very high retry count
    var delay = RetryBackoffCalculator.CalculateDelay(
        retryCount: 20, // 2^20 = 1,048,576ms
        initialDelayMs: 100,
        maxDelayMs: 5000);

    // Even with jitter, should never exceed max
    Assert.InRange(delay.TotalMilliseconds, 0, 5000);
}
```

## Related Components

- **DefaultResiliencePipeline** (`src/Extension/PeriodicJob/DefaultResiliencePipeline.cs`): Uses Polly for circuit breaker + timeout
- **PeriodicJob**: Integrates resilience pipeline for periodic operations
- **S7DataPortOutgoing**: Uses retry logic for write operations

## Comparison with Polly

This project uses Polly (via DefaultResiliencePipeline) for circuit breaker and timeout policies. The RetryBackoffCalculator provides:
- Simpler, standalone backoff calculations
- Explicit control over delay calculation
- No external dependencies for basic retry logic
- Easy testability with IDelayProvider

**When to use RetryBackoffCalculator vs Polly**:

| Scenario | Recommendation |
|----------|---------------|
| Need circuit breaker pattern | Use Polly (DefaultResiliencePipeline) |
| Need timeout policies | Use Polly (DefaultResiliencePipeline) |
| Simple retry with custom backoff | Use RetryBackoffCalculator + manual retry loop |
| Testing retry logic without delays | Use RetryBackoffCalculator + IDelayProvider |
| Hot path performance critical | Use RetryBackoffCalculator (lower overhead) |

---

**Last Updated**: 2026-01-30
