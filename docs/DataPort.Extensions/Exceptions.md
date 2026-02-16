# Common Exceptions

**Namespace**: `ViciOne.Suite.DataPort.Extensions.Exceptions`

Standard exception types for DataPort operations. All inherit directly from `System.Exception` and follow standard .NET constructor patterns (default, message, message + inner exception).

## Exception Types

### ConnectionFailureException

**Location**: `src/DataPort.Extensions/Exceptions/ConnectionFailureException.cs`

Thrown when a connection to an external device cannot be established. Throw from `IClientLifecycleManager` and `AcquireConnectionAsync` implementations when the underlying protocol fails to connect.

---

### DataRetrievalException

**Location**: `src/DataPort.Extensions/Exceptions/DataRetrievalException.cs`

Thrown when reading data from an external device fails. Throw from `IReadClient` implementations to translate protocol-specific errors.

---

### DataConversionException

**Location**: `src/DataPort.Extensions/Exceptions/DataConversionException.cs`

Thrown when a value cannot be converted between the protocol's native type and the .NET type. Uses factory methods instead of public constructors:

| Factory method | Scenario |
|----------------|----------|
| `UnsupportedValueType(systemTypeName, valueType)` | Value type not supported for read conversion |
| `UnsupportedSystemType(systemType)` | System type not supported for read conversion |
| `UnsupportedWriteValueType(systemTypeName, valueType)` | Value type not supported for write conversion |
| `UnsupportedWriteSystemType(systemType)` | System type not supported for write conversion |
| `FailedToSetAccessBuffer(valueTypeName, errorMessage)` | Setting value in access buffer failed |
| `FailedToGetAccessBuffer(valueTypeName, errorMessage)` | Getting value from access buffer failed |

---

### InvalidConfigurationException

**Location**: `src/DataPort.Extensions/Exceptions/InvalidConfigurationException.cs`

Thrown when configuration is invalid. Throw from `TypedNodeMapper` and configuration validation code during startup.

---

### MaxRetriesExceededException

**Location**: `src/DataPort.Extensions/Exceptions/MaxRetriesExceededException.cs`

Thrown by `BoundedRetryPolicy` when all retry attempts are exhausted. Includes an `AttemptCount` property with the number of attempts made. Caught internally by `OutgoingDataPortBase` — implementations do not need to handle it.

---

### QueueBackpressureException

**Location**: `src/DataPort.Extensions/Exceptions/QueueBackpressureException.cs`

Thrown when a retrying write item is displaced from the queue head (DropOldest strategy + queue overflow during retry). Caught internally by `OutgoingDataPortBase` — implementations do not need to handle it.

---

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

---

**Last Updated**: 2026-02-23
