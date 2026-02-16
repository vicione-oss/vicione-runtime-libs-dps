# Model Components

**Namespace**: `ViciOne.Suite.DataPort.Extensions.Model`

Defines core abstractions and value types for DataPort operations.

## Overview

The Model components provide interfaces and records that both Absolute and Symbolic DataPort implementations must implement.

## Data Point Abstractions

### IDataPoint

**Location**: `src/Extension/Model/DataPoints/IDataPoint.cs`

**Purpose**: Base interface for all data point types

**Properties**:
- `Channels`: Channel configuration for this data point

**Methods**:
- `ContainsTransferredChannel(string channel)`: Checks if a channel is configured for transfer

**Usage**:
```csharp
public interface IDataPoint
{
    Channels Channels { get; init; }
    bool ContainsTransferredChannel(string channel);
}

// Implementations
public record IntegerDataPoint : IDataPoint { ... }
public record SymbolicDataPoint : IDataPoint { ... }
```

### IPollingDataPoint

**Location**: `src/Extension/Model/DataPoints/IPollingDataPoint.cs`

**Purpose**: Interface for data points that support periodic polling

**Properties**:
- `PollFrequency`: How often to read this data point from the PLC

**Usage**:
```csharp
public interface IPollingDataPoint : IDataPoint
{
    PollFrequency PollFrequency { get; init; }
}
```

### IDataPointPollingGroup

**Location**: `src/Extension/Model/DataPoints/IDataPointPollingGroup.cs`

**Purpose**: Interface for groups of data points polled together at the same frequency

**Properties**:
- `DataPoints`: Collection of data points in this group
- `PollFrequency`: Shared polling frequency for all data points in group

**Usage**:
```csharp
public interface IDataPointPollingGroup
{
    IReadOnlyList<IPollingDataPoint> DataPoints { get; init; }
    PollFrequency PollFrequency { get; init; }
}

// Example: Group data points by poll frequency for efficient batching
var groups = dataPoints
    .GroupBy(dp => dp.PollFrequency)
    .Select(g => new DataPointGroup
    {
        DataPoints = g.ToList(),
        PollFrequency = g.Key
    });
```

### IDataPointValue

**Location**: `src/Extension/Model/DataPoints/IDataPointValue.cs`

**Purpose**: Interface for data point values read from or written to PLC

**Properties**:
- `Channels`: Channel configuration
- `Value`: The actual value (object type for flexibility)

**Usage**:
```csharp
public interface IDataPointValue
{
    Channels Channels { get; init; }
    object? Value { get; init; }
}

// Implementations
public record DataPointValue : IDataPointValue { ... }
public record SymbolicDataPointValue : IDataPointValue { ... }
```

## Value Types

### Channels

**Location**: `src/Extension/Model/DataPoints/Channels.cs`

**Purpose**: Defines channel routing for data point values

**Properties**:
- `TransferredChannels`: List of channel names where values should be published

**Usage**:
```csharp
public record Channels(List<TransferredChannel> TransferredChannels);

public record TransferredChannel(string Value);

// Example
var channels = new Channels(
    TransferredChannels: new List<TransferredChannel>
    {
        new TransferredChannel("Temperature_Sensor_1"),
        new TransferredChannel("Temperature_Alarm")
    }
);
```

### PollFrequency

**Location**: `src/Extension/Model/DataPoints/PollFrequency.cs`

**Purpose**: Strongly-typed polling interval

**Properties**:
- `Value`: TimeSpan representing the polling interval

**Usage**:
```csharp
public record PollFrequency(TimeSpan Value);

// Example
var pollFrequency = new PollFrequency(TimeSpan.FromMilliseconds(100));

// Common intervals
var fast = new PollFrequency(TimeSpan.FromMilliseconds(100));    // 10 Hz
var medium = new PollFrequency(TimeSpan.FromMilliseconds(500));  // 2 Hz
var slow = new PollFrequency(TimeSpan.FromSeconds(1));           // 1 Hz
```

## Configuration Records

### QueueConfiguration

**Location**: `src/Extension/Model/QueueConfiguration.cs`

**Purpose**: Configuration for write queue behavior

**Properties**:
- `MaxPendingMessages`: Queue capacity
- `Strategy`: Overflow strategy (DropOldest or DropNewest)

**Usage**:
```csharp
public sealed record QueueConfiguration(int MaxPendingMessages, QueueStrategy Strategy);

// Example
var queueConfig = new QueueConfiguration(
    MaxPendingMessages: 1000,
    Strategy: QueueStrategy.DropNewest
);
```

## Design Patterns

### Interface Segregation

The model uses interface segregation to provide flexibility:
- `IDataPoint`: Minimal contract (just channels)
- `IPollingDataPoint`: Extends with polling capability
- `IDataPointPollingGroup`: Groups for batch operations
- `IDataPointValue`: Read/write values

This allows:
- Different data point types (Absolute vs Symbolic)
- Optional polling support
- Flexible grouping strategies

### Record Types

All model types are records for:
- **Immutability**: Values can't be modified after creation
- **Value equality**: Two records with same data are equal
- **Copy semantics**: `with` expressions for modifications
- **Deconstruction**: Pattern matching support

```csharp
// Immutability
var channels = new Channels(...);
// channels.TransferredChannels = ...; // Compiler error

// Value equality
var freq1 = new PollFrequency(TimeSpan.FromMilliseconds(100));
var freq2 = new PollFrequency(TimeSpan.FromMilliseconds(100));
Assert.Equal(freq1, freq2); // True

// Copy with modification
var newFreq = freq1 with { Value = TimeSpan.FromMilliseconds(200) };

// Pattern matching
var result = pollFrequency switch
{
    { Value: var v } when v < TimeSpan.FromMilliseconds(100) => "Very fast",
    { Value: var v } when v < TimeSpan.FromSeconds(1) => "Fast",
    _ => "Slow"
};
```

## Extension Points

Implement these interfaces to add new data point types:

```csharp
// Custom data point type
public record CustomDataPoint : IPollingDataPoint
{
    public Channels Channels { get; init; }
    public PollFrequency PollFrequency { get; init; }
    public string CustomProperty { get; init; }

    public bool ContainsTransferredChannel(string channel)
        => Channels.TransferredChannels.Exists(c => c.Value == channel);
}

// Custom polling group
public record CustomPollingGroup : IDataPointPollingGroup
{
    public IReadOnlyList<IPollingDataPoint> DataPoints { get; init; }
    public PollFrequency PollFrequency { get; init; }
    public string GroupName { get; init; }
}
```

## Related Components

- **[TypedNodes](Overview.md#typed-nodes-system)**: Configuration-to-model mapping system
- **[IncomingDataPort](IncomingDataPort.md)**: Uses IDataPoint for polling configuration
- **[OutgoingDataPort](OutgoingDataPort.md)**: Uses IDataPointValue for write operations

---

**Last Updated**: 2026-01-30
