# Value Validation System

**Namespace**: `ViciOne.Suite.DataPort.Extensions.Outgoing.Value.Validation`

**Location**: `src/DataPort.Extensions/Outgoing/Value/Validation/`

Validates data point values before writing to devices using **all-or-nothing semantics**: if any value in a batch fails, the entire batch is rejected. This prevents partial writes that could leave the device in an inconsistent state.

The validation runs automatically inside `OutgoingDataPortBase` — implementations only need to implement `IDataPointValue<TDataPoint>` correctly.

## Components

### DataPointValueValidator

**Location**: `src/DataPort.Extensions/Outgoing/Value/Validation/DataPointValueValidator.cs`

```csharp
public class DataPointValueValidator<TDataPoint, TDataPointValue>
    where TDataPoint : IDataPoint
    where TDataPointValue : IDataPointValue<TDataPoint>
{
    public DataPointValuesValidationResult<TDataPoint, TDataPointValue> ValidateAll(
        IReadOnlyCollection<TDataPointValue> dataPointValues);
}
```

Calls `ValidateType()` then `IsInValueRange()` on each value. Returns a result with `IsValid`, `ValidValues`, and `Failures`.

---

### DataPointValuesValidationResult

```csharp
public record DataPointValuesValidationResult<TDataPoint, TDataPointValue>(
    bool IsValid,
    IReadOnlyList<TDataPointValue> ValidValues,
    IReadOnlyList<DataPointValueValidationFailure<TDataPoint>> Failures);
```

---

### DataPointValueValidationFailure

```csharp
public record DataPointValueValidationFailure<TDataPointType>(
    TDataPointType DataPoint,
    object? ActualValue,
    ValidationFailureReason Reason,
    string Details) where TDataPointType : IDataPoint;
```

---

### ValidationFailureReason

```csharp
public enum ValidationFailureReason
{
    TypeMismatch,      // runtime value type doesn't match the configured data point type
    ValueOutOfRange    // value is outside valid bounds for the data type
}
```

---

### TypeValidationResult

Returned by `IDataPointValue.ValidateType()`. Properties: `IsValid`, `ActualType`, `AllowedTypes`.

---

## Logging

The validator logs failures with structured messages:

- **Type mismatch**: `"Value has wrong type for channel {Channel} at {Address}: expected {AllowedTypes}, got {ActualType}"`
- **Out of range**: `"Value out of range for channel {Channel} at {Address}: '{Value}' is invalid for {DataType}"`

---

## Related Components

- [OutgoingDataPort](OutgoingDataPort.md) — uses validation during write processing
- [Model](../Model.md) — `IDataPointValue` interface

---

**Last Updated**: 2026-02-23
