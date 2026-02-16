# DataPort Extensions Package Documentation

This directory contains comprehensive documentation for the reusable components in the DataPort Extensions package.

## Quick Navigation

| Component | Documentation | Purpose |
|-----------|---------------|---------|
| **IncomingDataPort** | [IncomingDataPort.md](IncomingDataPort.md) | Template for incoming (read) data ports |
| **OutgoingDataPort** | [OutgoingDataPort.md](OutgoingDataPort.md) | Template for outgoing (write) data ports |
| **Interfaces** | [Interfaces.md](Interfaces.md) | Extension interfaces reference |
| **PeriodicJob** | [Overview.md#periodicjob](Overview.md#periodicjob) | Periodic task execution with resilience |
| **TypedNodes** | [Overview.md#typed-nodes-system](Overview.md#typed-nodes-system) | Configuration mapping framework |
| **Queue** | [Queue.md](Queue.md) | Thread-safe bounded queue with overflow strategies |
| **ValueValidation** | [ValueValidation.md](ValueValidation.md) | Value validation system |
| **Resilience** | [Resilience.md](Resilience.md) | Retry and backoff calculation utilities |
| **Exceptions** | [Exceptions.md](Exceptions.md) | Standard exception types |
| **Logging** | [Logging.md](Logging.md) | Efficient logging utilities |
| **Model** | [Model.md](Model.md) | Core abstractions and value types |

## Documentation Files

### [IncomingDataPort.md](IncomingDataPort.md)
Template for building incoming (read) data ports.

**Covers**: IncomingDataPortBase, abstract methods, lifecycle, polling job creation, implementation example

### [OutgoingDataPort.md](OutgoingDataPort.md)
Template for building outgoing (write) data ports.

**Covers**: OutgoingDataPortBase, abstract members, queue processing, retry policies, value validation, implementation example

### [Interfaces.md](Interfaces.md)
Comprehensive reference for all extension interfaces.

**Covers**: IConnectionManager, IReadClient, IWriteClient, IRetryPolicy, IDataPointValidator, IDataPointValueFactory

### [Overview.md](Overview.md)
High-level overview of common components with usage examples and architectural patterns.

**Covers**: PeriodicJob framework, TypedNodes mapping system

### [Queue.md](Queue.md)
Detailed documentation of the queue components used for write buffering.

**Covers**: ChannelQueue<T>, PrependableChannelQueue<T>, QueueStrategy, Peek/Dequeue pattern, backpressure handling

### [ValueValidation.md](ValueValidation.md)
Value validation system for outgoing data.

**Covers**: DataPointValueValidator, ValidationFailureReason, all-or-nothing validation semantics

### [Resilience.md](Resilience.md)
Retry and backoff strategies for resilient communication.

**Covers**: RetryBackoffCalculator, IDelayProvider, exponential/linear backoff, jitter

### [Exceptions.md](Exceptions.md)
Standard exception types for semantic error handling.

**Covers**: ConnectionFailureException, DataRetrievalException, DataConversionException, InvalidConfigurationException, MaxRetriesExceededException, QueueBackpressureException

### [Logging.md](Logging.md)
Efficient structured logging utilities.

**Covers**: FormattedExternalValues, lazy formatting, structured logging patterns

### [Model.md](Model.md)
Core abstractions and value types.

**Covers**: IDataPoint, IPollingDataPoint, IDataPointPollingGroup, IDataPointValue, Channels, PollFrequency, QueueConfiguration

## Use Cases

### I want to create a new incoming data port
→ Start with [IncomingDataPort.md](IncomingDataPort.md)
→ Then see [Interfaces.md](Interfaces.md) for IConnectionManager and IReadClient

### I want to create a new outgoing data port
→ Start with [OutgoingDataPort.md](OutgoingDataPort.md)
→ Then see [Interfaces.md](Interfaces.md) for IWriteClient and IRetryPolicy

### I want to implement periodic polling
→ Start with [Overview.md#periodicjob](Overview.md#periodicjob)
→ Then see [Resilience.md](Resilience.md) for retry strategies

### I want to buffer writes with backpressure
→ Start with [Queue.md](Queue.md)
→ Then see [Exceptions.md#queuebackpressureexception](Exceptions.md#queuebackpressureexception)

### I want to validate values before writing
→ Start with [ValueValidation.md](ValueValidation.md)
→ Then see [Model.md](Model.md) for IDataPointValue interface

### I want to add a custom data point type
→ Start with [Model.md](Model.md)
→ Then see [Overview.md#typed-nodes-system](Overview.md#typed-nodes-system) for mapping

### I want to optimize logging performance
→ Start with [Logging.md](Logging.md)
→ See examples of FormattedExternalValues usage

### I want to understand error handling
→ Start with [Exceptions.md](Exceptions.md)
→ See error handling patterns

---

**Last Updated**: 2026-01-30
