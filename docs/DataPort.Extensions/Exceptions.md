# Common Exceptions

**Namespace**: `ViciOne.Suite.DataPort.Extensions.Exceptions`

Defines standard exception types for DataPort operations.

## Overview

These exceptions provide semantic error types for common failure scenarios in PLC communication and data processing.

## Exception Types

### ConnectionFailureException

**Location**: `src/Extension/Exceptions/ConnectionFailureException.cs`

**Purpose**: Thrown when PLC connection establishment fails

**Usage**:
```csharp
try
{
    await s7Client.ConnectAsync(cancellationToken);
}
catch (SocketException ex)
{
    throw new ConnectionFailureException(
        $"Failed to connect to PLC at {ipAddress}:{port}",
        ex
    );
}
```

**Common Causes**:
- Network unreachable
- PLC not responding (offline, wrong IP address)
- Firewall blocking connection
- Port not open
- Invalid rack/slot configuration (absolute addressing only - symbolic addressing does not require rack/slot parameters)

### DataRetrievalException

**Location**: `src/Extension/Exceptions/DataRetrievalException.cs`

**Purpose**: Thrown when reading data from PLC fails

**Usage**:
```csharp
try
{
    var values = await s7Client.ReadAsync(dataPointGroup, cancellationToken);
}
catch (Exception ex)
{
    throw new DataRetrievalException(
        $"Failed to read {dataPointGroup.DataPoints.Count} data points",
        ex
    );
}
```

**Common Causes**:
- Invalid address (DB doesn't exist, offset out of bounds)
- PLC not in RUN mode
- Access rights insufficient
- Connection lost during read
- Read timeout

### InvalidConfigurationException

**Location**: `src/Extension/Exceptions/InvalidConfigurationException.cs`

**Purpose**: Thrown when YAML configuration is invalid

**Usage**:
```csharp
if (pollFrequency.Value <= TimeSpan.Zero)
{
    throw new InvalidConfigurationException(
        $"Poll frequency must be positive, got {pollFrequency.Value}"
    );
}
```

**Common Causes**:
- Missing required fields
- Invalid value ranges (negative intervals, invalid addresses)
- Type mismatches
- Conflicting settings
- Malformed YAML syntax

### QueueBackpressureException

**Location**: `src/Extension/Exceptions/QueueBackpressureException.cs`

**Purpose**: Thrown when write queue is critically full or when a retrying item is superseded

**Usage**:
```csharp
if (writeQueue.Count >= writeQueue.Capacity * 0.9)
{
    throw new QueueBackpressureException(
        $"Write queue at {writeQueue.Count}/{writeQueue.Capacity} - " +
        "backpressure detected, refusing new writes"
    );
}
```

**Common Causes**:
- PLC writes slower than incoming data rate
- Network congestion
- PLC in STOP mode (writes failing)
- Large batch writes
- Insufficient queue capacity configuration
- Item superseded during retry (DropOldest strategy)

---

### DataConversionException

**Location**: `src/Extension/Exceptions/DataConversionException.cs`

**Purpose**: Thrown when data type conversion fails during read or write operations

**Key Feature**: Uses factory methods instead of public constructors for semantic clarity.

**Factory Methods**:

| Method | Scenario |
|--------|----------|
| `UnsupportedValueType(systemTypeName, valueType)` | Value type not supported for read conversion |
| `UnsupportedSystemType(systemType)` | System type not supported for read conversion |
| `UnsupportedWriteValueType(systemTypeName, valueType)` | Value type not supported for write conversion |
| `UnsupportedWriteSystemType(systemType)` | System type not supported for write conversion |
| `FailedToSetAccessBuffer(valueTypeName, errorMessage)` | Setting value in access buffer failed |
| `FailedToGetAccessBuffer(valueTypeName, errorMessage)` | Getting value from access buffer failed |

**Usage**:
```csharp
// Read conversion failure
throw DataConversionException.UnsupportedValueType("S7_Bool", value.GetType());

// Write conversion failure
throw DataConversionException.UnsupportedWriteSystemType(systemType);

// Buffer operation failure
throw DataConversionException.FailedToSetAccessBuffer("STRING16", error.Message);
```

**Common Causes**:
- Incompatible .NET type for PLC data type
- Unsupported PLC data type
- String length exceeds PLC string capacity
- Buffer API error

---

### MaxRetriesExceededException

**Location**: `src/Extension/Exceptions/MaxRetriesExceededException.cs`

**Purpose**: Thrown when a retryable operation fails after exhausting all configured retry attempts

**Key Feature**: Includes `AttemptCount` property with the number of attempts made.

**Constructors**:

| Constructor | Description |
|-------------|-------------|
| `(long attemptCount)` | Basic with attempt count |
| `(long attemptCount, string message)` | With custom message |
| `(long attemptCount, string message, Exception inner)` | With inner exception |
| `()`, `(string)`, `(string, Exception)` | Compatibility constructors (AttemptCount = 0) |

**Usage**:
```csharp
try
{
    await WriteWithRetryAsync(data);
}
catch (MaxRetriesExceededException ex)
{
    logger.LogError(
        "Write failed after {Attempts} attempts: {Message}",
        ex.AttemptCount,
        ex.Message);

    // Handle permanent failure
}
```

**Common Causes**:
- Persistent network failure
- PLC in STOP mode
- Invalid address configuration
- Hardware failure

**Integration**: Thrown by `OutgoingDataPortBase` when `BoundedRetryPolicy` is exhausted.

## Exception Hierarchy

```
Exception (System)
  ├─ ConnectionFailureException
  ├─ DataRetrievalException
  ├─ DataConversionException
  ├─ InvalidConfigurationException
  ├─ MaxRetriesExceededException
  └─ QueueBackpressureException
```

All exceptions inherit directly from `System.Exception` and follow standard .NET exception patterns:
- Default constructor
- Message constructor
- Message + inner exception constructor

## Error Handling Patterns

### Connection Failures

```csharp
try
{
    await ConnectAsync(cancellationToken);
}
catch (ConnectionFailureException ex)
{
    logger.LogError(ex, "PLC connection failed - will retry");
    // Trigger reconnection logic
}
```

### Data Retrieval Failures

```csharp
try
{
    return await ReadDataAsync(cancellationToken);
}
catch (DataRetrievalException ex)
{
    logger.LogWarning(ex, "Read failed - marking values as invalid");
    return CreateInvalidValues(); // Return invalid markers instead of throwing
}
```

### Configuration Failures

```csharp
try
{
    var config = ValidateConfiguration(yamlConfig);
}
catch (InvalidConfigurationException ex)
{
    logger.LogError(ex, "Configuration validation failed");
    throw; // Fail fast during startup
}
```

### Backpressure

```csharp
try
{
    await EnqueueWriteAsync(values, cancellationToken);
}
catch (QueueBackpressureException ex)
{
    logger.LogWarning(ex, "Queue full - dropping oldest values");
    // Let DropOldest strategy handle it, or implement flow control
}
```

## Design Considerations

**Why Custom Exceptions?**
- Enables semantic error handling (catch specific failure types)
- Improves logging clarity (exception type indicates root cause)
- Documents expected failure modes
- Supports telemetry and monitoring (count exception types)

**When to Use?**
- Use custom exceptions for domain-specific failures
- Include context (addresses, counts, states) in messages
- Always include inner exception for technical details
- Don't use for flow control (use return values instead)

---

**Last Updated**: 2026-01-30
