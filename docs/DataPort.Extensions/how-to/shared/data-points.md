# Define the runtime data points

**Shared building block — step 3.** Used by both [incoming](../incoming-dataport/incoming-data-port.md)
and [outgoing](../outgoing-dataport/outgoing-data-port.md) ports.

A data point is the runtime, flat representation of one addressable value. The
[node model](node-model.md) is the configuration-time tree; data points are what that tree is
flattened into for operation. They are the values a read returns and a write consumes, and they are
where the library's type safety lives.

The worked example is under `tests/DataPort.Extensions.Tests/Stubs/Model/DataPort/`.

## Pick the right interface for the direction

Two interfaces sit at the base of the data-point hierarchy, and which one you need depends on the
direction:

- **`IDataPoint`** — the minimum: `Channels`, `Identifier`, `DataTypeName`. Enough for an
  [outgoing](../outgoing-dataport/outgoing-data-port.md) port.
- **`IPollingDataPoint`** — adds `PollFrequency`, so the [incoming](../incoming-dataport/incoming-data-port.md)
  port can group points by how often they're polled.

A point that implements `IPollingDataPoint` is also an `IDataPoint`, so a single type can serve both
directions — which is what the example does, so its one `TestDataPoint` works for the incoming and
outgoing ports alike.

## Make the type the value's type

Declare a data point's value type with `ITypedDataPoint<TValue>` rather than an enum or string
discriminator — the type argument *is* the type, and a value in flight carries the same guarantee
through `ITypedDataPointValue<TValue>`. For *why* this is what lets the outgoing write path skip a
separate runtime type check, see the
[design rationale](../../explanation/configuration/internal-data-model.md).

The example factors the protocol-agnostic metadata into a base record, then a generic base that adds
the typed-conversion logic, leaving each concrete type tiny:

`Stubs/Model/DataPort/TestDataPoint.cs`:

```csharp
internal abstract record TestDataPoint : IPollingDataPoint
{
    public Channels Channels { get; init; } = new([], []);
    public DataPointIdentifier Identifier { get; init; } = new(string.Empty);
    public DataTypeName DataTypeName { get; init; } = new(string.Empty);
    public PollFrequency PollFrequency { get; init; } = PollFrequency.FromMilliseconds(100);

    // Outgoing only: turn an untyped engine value into a typed one, or explain why it can't be
    public abstract DataPointValueConversion<IDataPointValue> ConvertValue(object? value);
}

internal abstract record TestDataPoint<TValue> : TestDataPoint, ITypedDataPoint<TValue>
{
    public abstract ITypedDataPointValue<TValue> CreateTypedValue(TValue value);

    public override DataPointValueConversion<IDataPointValue> ConvertValue(object? value) => value switch
    {
        TValue typed => new ConvertedDataPointValue<IDataPointValue>(CreateTypedValue(typed)),
        _            => Failure($"value is not assignable to {typeof(TValue)}")
    };
}
```

`Stubs/Model/DataPort/DataPoints/Int/IntDataPoint.cs`:

```csharp
internal record IntDataPoint : TestDataPoint<int>
{
    public override ITypedDataPointValue<int> CreateTypedValue(int value) => new IntDataPointValue(this, value);

    private readonly record struct IntDataPointValue(IDataPoint DataPoint, int TypedValue) : ITypedDataPointValue<int>
    {
        public bool IsInValueRange() => true; // range check runs on the outgoing write path
    }
}
```

A second type — `StringDataPoint : TestDataPoint<string>` — follows the same shape with `string` as
its value type. Adding a data type to your protocol is one small record like this.

## The two methods that do the work

- **`CreateTypedValue(TValue)`** wraps a typed value in an `ITypedDataPointValue<TValue>`. An
  [incoming](../incoming-dataport/incoming-data-port.md) read client calls it to build the values it
  returns.
- **`ConvertValue(object?)`** (outgoing) turns the engine's untyped `object?` into either a
  `ConvertedDataPointValue<…>` or a `NotConvertedDataPointValue<…>` that carries the reason. Returning
  the failure rather than throwing lets the base class log it and drop the batch cleanly. The
  [outgoing port](../outgoing-dataport/outgoing-data-port.md) calls this from its
  `ConvertToDataPointValue` override.
- **`IsInValueRange()`** on the value is the second outgoing gate: range validation, run after
  conversion. Return `true` when the typed value is within the bounds the data point allows.

## Next

Continue with [Manage the connection](connection.md), then pick a direction.

## Related

- [Internal data model — design rationale](../../explanation/configuration/internal-data-model.md) —
  why the runtime model is flat and indexed rather than a tree
- [Value conversion and validation](../../reference/value-validation.md) — the two-gate conversion +
  range-validation system the outgoing path runs
- [Model reference](../../reference/model.md) — `IDataPoint`, `IPollingDataPoint`,
  `ITypedDataPoint<TValue>`, `ITypedDataPointValue<TValue>`, `Channels`, `PollFrequency`
