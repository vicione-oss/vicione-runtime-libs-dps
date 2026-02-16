# DataPort Extensions Package Documentation

## Quick Navigation

| Component | Documentation | Purpose |
|-----------|---------------|---------|
| **Overview** | [Overview.md](Overview.md) | Core concepts: typed nodes, mappers, YAML tests |
| **IncomingDataPort** | [IncomingDataPort.md](Incoming/IncomingDataPort.md) | Template for incoming (read) data ports |
| **OutgoingDataPort** | [OutgoingDataPort.md](Outgoing/OutgoingDataPort.md) | Template for outgoing (write) data ports |
| **Interfaces** | [Interfaces.md](Interfaces.md) | Extension interfaces reference |
| **PeriodicJob** | [IncomingDataPort.md#polling-infrastructure](Incoming/IncomingDataPort.md#polling-infrastructure) | Periodic task execution with resilience |
| **TypedNodes** | [Model.md#typednodes](Model.md#typednodes) | Configuration mapping framework |
| **Queue** | [Queue.md](Outgoing/Queue.md) | Thread-safe bounded queue with overflow strategies |
| **ValueValidation** | [ValueValidation.md](Outgoing/ValueValidation.md) | Value validation system |
| **Resilience** | [Resilience.md](Outgoing/Resilience.md) | Retry and backoff calculation utilities |
| **Exceptions** | [Exceptions.md](Exceptions.md) | Standard exception types |
| **Model** | [Model.md](Model.md) | Core abstractions and value types |

## Use Cases

### I want to create a new incoming data port
→ Start with [IncomingDataPort.md](Incoming/IncomingDataPort.md)
→ Then see [Interfaces.md](Interfaces.md) for IClientLifecycleManager and IReadClient

### I want to create a new outgoing data port
→ Start with [OutgoingDataPort.md](Outgoing/OutgoingDataPort.md)
→ Then see [Interfaces.md](Interfaces.md) for IWriteClient and IRetryPolicy

### I want to implement periodic polling
→ Start with [IncomingDataPort.md#polling-infrastructure](Incoming/IncomingDataPort.md#polling-infrastructure)
→ Then see [Resilience.md](Outgoing/Resilience.md) for retry strategies

### I want to buffer writes with backpressure
→ Start with [Queue.md](Outgoing/Queue.md)
→ Then see [Exceptions.md#queuebackpressureexception](Exceptions.md#queuebackpressureexception)

### I want to validate values before writing
→ Start with [ValueValidation.md](Outgoing/ValueValidation.md)
→ Then see [Model.md](Model.md) for IDataPointValue interface

### I want to add a custom data point type
→ Start with [Model.md](Model.md)
→ Then see [Model.md#typednodes](Model.md#typednodes) for mapping

---

See [CHANGELOG.md](CHANGELOG.md) for breaking changes and migration steps.
