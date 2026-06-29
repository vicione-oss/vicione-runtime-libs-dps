# Value Conversion and Validation

> **Beyond the API summaries.** The generated [API reference](api/index.md) carries each type's members.
> This page adds the two-stage shape of the outgoing write path and the log events it emits, and points
> to the *why* in
> [Outgoing data port](../explanation/outgoing/outgoing-data-port.md#conversion-and-range-validation-are-two-stages-not-one-type-check) —
> including [why range validation runs only on writes](../explanation/outgoing/outgoing-data-port.md#range-validation-guards-the-device-so-it-runs-only-on-writes).

The outgoing write path turns each engine-supplied `ExternalValue` into a strongly-typed data point
value in two all-or-nothing stages — **conversion** then **range validation**. The types involved are
listed below.

## Stage 1 — Conversion

- **`ConvertToDataPointValue`** — the abstract hook each protocol implements; the base class calls it
  once per resolved channel. See the [outgoing how-to](../how-to/outgoing-dataport/outgoing-data-port.md#3-subclass-outgoingdataportbase)
  for an implementation.
- [`DataPointValueConversion<TDataPointValue>`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.DataPointValueConversion%601)
  — the per-value result, either
  [`ConvertedDataPointValue<TDataPointValue>`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.ConvertedDataPointValue%601)
  or
  [`NotConvertedDataPointValue<TDataPointValue>`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.NotConvertedDataPointValue%601).
- [`DataPointValuesConversionResult<TDataPointValue>`](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation.DataPointValuesConversionResult%601)
  — the per-batch outcome.

## Stage 2 — Range validation

- [`DataPointValueValidator<TDataPoint, TDataPointValue>`](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation.DataPointValueValidator%602)
  — calls `IsInValueRange()` on each converted value; constructed and invoked by the base class.
- [`DataPointValuesValidationResult<TDataPointValue>`](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.ValueValidation.DataPointValuesValidationResult%601)
  — the per-batch outcome.

## Shared failure types

- [`DataPointValueValidationFailure`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.DataPointValueValidationFailure)
- [`ValidationFailureReason`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.ValidationFailureReason)

## Logging

The write path emits these events (defined on the listed source class):

| Event ID | Source | Message |
|----------|--------|---------|
| 205 | `OutgoingDataPortBase` | `Conversion failed for channel '{Channel}': {Details}` (per failed value) |
| 209 | `OutgoingDataPortBase` | `{FailureCount}/{TotalCount} values are incompatible with the configured data points, nothing will be written` (whole batch dropped) |
| 210 | `DataPointValueValidator` | `Value with dataType '{DataType}' for channel '{Channel}' at {Address} is outside allowed range` (per out-of-range value) |

## Related

- [Outgoing data port](../explanation/outgoing/outgoing-data-port.md) — the write path that runs these stages
- [Model](model.md#data-point-abstractions) — `IDataPointValue`, `ITypedDataPoint<TValue>`, `ITypedDataPointValue<TValue>`
- [Why range validation is outgoing-only](../explanation/outgoing/outgoing-data-port.md#range-validation-guards-the-device-so-it-runs-only-on-writes) — the trust boundary that keeps validation on the write path
