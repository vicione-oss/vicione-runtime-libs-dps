# DataPort.Extensions — Changelog

## Unreleased — type-safe values, conversion/validation split

### Added

- `ITypedDataPoint<TValue>` and `ITypedDataPointValue<TValue>` (in `Model.DataPoints`) — strongly-typed data point and value interfaces. The CLR type argument is now the source of truth for a data point's data type, replacing the former enum/string discriminator. `ITypedDataPoint<TValue>` adds `CreateTypedValue(TValue)`; `ITypedDataPointValue<TValue>` adds `TypedValue`.
- `DataPointValueConversion<TDataPointValue>` with cases `ConvertedDataPointValue<…>` and `NotConvertedDataPointValue<…>` (in `Model.DataPoints`) — a closed result type for value conversion, replacing the previous nullable-return pattern that silently lost the failure reason.
- `DataPointValuesConversionResult<TDataPointValue>` (in `Outgoing.ValueValidation`) — batch conversion outcome with a computed `AllConverted`.
- `Channels.AffectedChannels(params string[])` / `Channels.TransferredChannels(params string[])` — static factory helpers that build the channel lists from names.

### Changed

- **Breaking**: `IDataPoint` and `IDataPointValue` are no longer generic. `IDataPointValue` now exposes `IDataPoint DataPoint`, `object? Value`, and `bool IsInValueRange()`. The former `IDataPointValue<TDataPoint>` (which also carried `ValidateType()`) is replaced by `IDataPointValue` plus `ITypedDataPointValue<TValue>`.
- **Breaking**: `OutgoingDataPortBase.ConvertValue(ExternalValue, TDataPoint): TDataPointValue` → `ConvertToDataPointValue(ExternalValue, TDataPoint): DataPointValueConversion<TDataPointValue>`. Conversion failures are now returned as data (a `NotConvertedDataPointValue`) rather than thrown or signalled with `null`.
- **Breaking**: the outgoing write path is now two all-or-nothing stages — conversion, then range validation. A failure at either stage drops the whole batch. There is no longer a runtime *type* check (it is guaranteed at compile time by `ITypedDataPointValue<TValue>`).
- **Breaking**: `DataPointValueValidator<TDataPoint, TDataPointValue>` validates range only; the constraint is now `TDataPointValue : IDataPointValue` (was `IDataPointValue<TDataPoint>`), the constructor takes an `ILogger`, and `ValidateAll(IReadOnlyList<TDataPointValue>)` returns `DataPointValuesValidationResult<TDataPointValue>`.
- **Breaking**: `DataPointValuesValidationResult` now has a single type parameter `<TDataPointValue>` and carries `ValidValues` + `Failures`; `IsValid` is a computed property (was a constructor parameter).
- **Breaking**: `DataPointValueValidationFailure` moved to `Model.DataPoints` and is now a non-generic `readonly record struct (IDataPoint DataPoint, ValidationFailureReason Reason, string Details)` — the `ActualValue` member is removed.
- **Breaking**: `ValidationFailureReason.TypeMismatch` → `ValidationFailureReason.ConversionFailure`.
- **Breaking**: `Channels` record-struct properties renamed: `AffectedChannels` → `Affected`, `TransferredChannels` → `Transferred`. (The old names now exist as the static factory helpers above.)

### Removed

- **Breaking**: `IDataPointValue.ValidateType()` and the `TypeValidationResult` type — type correctness is now guaranteed at compile time by `ITypedDataPointValue<TValue>`.

### Migration

```csharp
// Before — single conversion method, type checked at runtime
protected override MyValue ConvertValue(ExternalValue externalValue, MyPoint dataPoint)
    => new MyValue(dataPoint, externalValue.Value);

// After — return a conversion result; report (don't throw) failures
protected override DataPointValueConversion<MyValue> ConvertToDataPointValue(
    ExternalValue externalValue, MyPoint dataPoint)
    => externalValue.Value is int raw
        ? new ConvertedDataPointValue<MyValue>(dataPoint.CreateTypedValue(raw))
        : new NotConvertedDataPointValue<MyValue>(new DataPointValueValidationFailure(
            dataPoint, ValidationFailureReason.ConversionFailure, "Expected int"));
```

- Make your value type implement `ITypedDataPointValue<TValue>` (drop any `ValidateType()` implementation); keep `IsInValueRange()` for range checks.
- Rename `Channels.AffectedChannels` / `.TransferredChannels` property access to `.Affected` / `.Transferred`.
- Replace `ValidationFailureReason.TypeMismatch` with `ValidationFailureReason.ConversionFailure`.

---

## 0.30.0 — 2026-03-20

### Added

- Mapper abstractions: `IDataPointsMapper` (outgoing) and `IDataPointGroupsMapper` (incoming) for decoupled configuration-to-data-point mapping
- Configuration verification: `IDataPointConfigurationVerifier` for async pre-connect data point validation, now available on both `IncomingDataPortBase` and `OutgoingDataPortBase`
- Strongly-typed value objects: `DataPointIdentifier`, `DataTypeName`, `QueueSize`
- `IncomingDataPortBase.ToExternalValues` — `protected virtual` hook to customise incoming value conversion without a separate interface
- `OutgoingDataPortBase.ConvertValue` — `protected abstract` method for converting `ExternalValue` + `TDataPoint` into `TDataPointValue`

### Changed

- **Breaking**: `IDataPoint.AddressString` → `IDataPoint.Identifier: DataPointIdentifier`
- **Breaking**: `IDataPoint.DataTypeString` → `IDataPoint.DataTypeName: DataTypeName`
- **Breaking**: `IConnectionManager<TClient>` renamed to `IClientLifecycleManager<TClient, TClientInformation>`, moved from `...Incoming.Client` to `...Client` (shared namespace, used by both base classes); no longer extends `IAsyncDisposable`; both methods now take a `TClientInformation` first parameter where `TClientInformation : IClientInformation` (see §5)
- **Breaking**: `IDataPointsMapper` type parameters reordered: `<TDataPoint, TRootNode, TCom>` → `<TRootNode, TCom, TDataPoint>` (see §2)
- **Breaking**: `IncomingDataPortBase` — new `TDataPoint` and `TClientInformation` type parameters (7 total), constructor replaced: `connectionManager` and `configurationVerifier` removed from constructor; `ClientLifecycleManager` and `ClientInformation` now abstract properties; configuration verification is an optional virtual factory method `CreateConfigurationVerifier(TReadClient client)`; `dataPointValueConverter` parameter removed; override `ToExternalValues` for custom conversion (see §3)
- **Breaking**: `OutgoingDataPortBase` — three new type parameters `TRootNode`, `TCommunication`, `TClientInformation` (6 total); `TClient` renamed to `TWriteClient`; constructor replaced: `connectionManager` and `configurationVerifier` removed from constructor; `ClientLifecycleManager` and `ClientInformation` now abstract properties; configuration verification is an optional virtual factory method `CreateConfigurationVerifier(TWriteClient client)`; `valueConverter` removed; `loggerFactory` moved to second position (see §4)
- **Breaking**: `ConnectAsync` on both base classes now throws `InvalidConfigurationException` (was `ConnectionFailureException`) when data points fail configuration verification
- **Breaking**: Namespace reorganisation — all types moved to more specific sub-namespaces (see §2)

### Removed

- **Breaking**: `IDataPointValidator<TDataPoint>` / `NoOpDataPointValidator<TDataPoint>` — replaced by `IDataPointConfigurationVerifier<TDataPoint>`
- **Breaking**: `IDataPointValueFactory<TDataPoint, TDataPointValue>` — replaced by `OutgoingDataPortBase.ConvertValue` abstract method
- **Breaking**: `IDataPointValueConverter<TDataPoint, TDataPointValue>` (incoming) — replaced by `IncomingDataPortBase.ToExternalValues` virtual method
- **Breaking**: `IExternalValueConverter<TDataPoint, TDataPointValue>` (outgoing) — replaced by `OutgoingDataPortBase.ConvertValue` abstract method
- **Breaking**: `QueueConfiguration` in `...Model` — replaced by `QueueConfiguration(QueueStrategy, QueueSize)` in `...Outgoing.QueueProcessing`
- **Breaking**: `OutgoingDataPortBase` abstract/virtual members removed: `WriteClient`, `AcquireConnectionAsync`, `ValidateConnectionAsync`, `ReleaseConnectionAsync`, `DisposeClient` — replaced by `ClientLifecycleManager` and `ClientInformation` abstract properties, the `CreateConfigurationVerifier` virtual factory, and the `ConvertValue` abstract method

---

## Migration from previous version

### 1. `IDataPoint` — renamed properties with value-object types

| Before | After |
|---|---|
| `string AddressString { get; }` | `DataPointIdentifier Identifier { get; }` |
| `string DataTypeString { get; }` | `DataTypeName DataTypeName { get; }` |

`DataPointIdentifier` and `DataTypeName` are simple records wrapping a `string Value`. Access the raw string via `.Value`.

```csharp
// Before
public class MyDataPoint : IDataPoint
{
    public string AddressString { get; init; } = "";
    public string DataTypeString { get; init; } = "";
}
logger.Log(dataPoint.AddressString);

// After
public class MyDataPoint : IDataPoint
{
    public DataPointIdentifier Identifier { get; init; } = new("");
    public DataTypeName DataTypeName { get; init; } = new("");
}
logger.Log(dataPoint.Identifier.Value);
```

---

### 2. Namespace reorganisation

Update your `using` directives:

| Old namespace | New namespace |
|---|---|
| `...Incoming` (`IConnectionManager`) | `...Client` (renamed to `IClientLifecycleManager<TClient, TClientInformation>`; implement `IClientInformation` on your connection-info type) |
| `...Incoming` (`IReadClient`) | `...Incoming.Client` |
| `...Outgoing` (`IWriteClient`) | `...Outgoing.Client` |
| `...TypedNodes` | `...Model.TypedNodes` |
| `...TypedNodes.Mapping` | `...Model.TypedNodes.Mapping` |
| `...TypedNodes.Mapping` (`InvalidPropertyException`, `PropertyValidationErrorType`, `PropertyValueConverter`) | `...Model.TypedNodes.Mapping.Properties` |
| `...Queue` (`ChannelQueue`, `ChannelQueueBase`, `ChannelQueueEnumerator`) | `...Outgoing.QueueProcessing.Bounded` |
| `...Queue` (`PrependableChannelQueue`, `PrependableChannelQueueEnumerator`) | `...Outgoing.QueueProcessing.Prependable` |
| `...Queue` (`QueueStrategy`) | `...Outgoing.QueueProcessing` |
| `...Outgoing` (`BoundedRetryPolicy`, `IRetryPolicy`, `InfiniteRetryPolicy`) | `...Outgoing.QueueProcessing.Retry` |
| `...Resilience` (`IDelayProvider`) | `...Outgoing.QueueProcessing` |
| `...Resilience` (`RetryBackoffCalculator`) | `...Outgoing.QueueProcessing.Retry` |
| `...ValueValidation` | `...Outgoing.ValueValidation` |
| `...Incoming` (`DataPointGroupPollingJob`) | `...Incoming.Polling` |
| `...PeriodicJob` (`PeriodicJob`, `DefaultResiliencePipeline`, ...) | `...Incoming.Polling.Job` |
| `...Model` (`QueueConfiguration`) | `...Outgoing.QueueProcessing` |

---

### 3. `IncomingDataPortBase` — new type parameters and constructor

The class gains `TDataPoint` (position 3) and `TClientInformation` (position 7) type parameters and a completely new constructor. The base class now owns configuration mapping internally — you no longer pre-build `TDeviceNode` or `IReadOnlyList<TDataPointGroup>` before constructing.

```
// Before (5 type params)
IncomingDataPortBase<TCommunication, TDeviceNode, TDataPointGroup, TDataPointValue, TClient>

// After (7 type params)
IncomingDataPortBase<TCommunication, TDeviceNode, TDataPoint, TDataPointGroup, TDataPointValue, TReadClient, TClientInformation>
```

New constraints: `TDataPoint : IPollingDataPoint`, `TDataPointGroup : IDataPointPollingGroup<TDataPoint>`, `TClientInformation : IClientInformation`

```csharp
// Before
protected MyIncomingDataPort(
    TDeviceNode deviceNode,
    IReadOnlyList<TDataPointGroup> dataPointGroups,
    ILogger logger,
    ILoggerFactory loggerFactory,
    IConnectionManager<TClient> connectionManager,
    IDataPointValidator<IDataPoint>? validator = null,
    TimeProvider? timeProvider = null)
    : base(deviceNode, dataPointGroups, logger, loggerFactory, connectionManager, validator, timeProvider)

// After
protected MyIncomingDataPort(
    TCommunication communication,
    TypedNodeMapper<TCommunication, TDeviceNode> typedNodeMapper,
    IDataPointGroupsMapper<TDataPoint, TDataPointGroup, TDeviceNode, TCommunication> dataPointGroupsMapper,
    ILoggerFactory loggerFactory,
    TimeProvider? timeProvider = null)
    : base(communication, typedNodeMapper, dataPointGroupsMapper, loggerFactory, timeProvider)

protected override IClientLifecycleManager<MyReadClient, MyClientInformation> ClientLifecycleManager { get; } = new MyClientLifecycleManager();
public override MyClientInformation ClientInformation { get; } = new MyClientInformation(...);

// Optional — override only if you need pre-poll configuration verification.
// The base provides a virtual factory that returns null by default; the client is supplied at call time.
protected override IDataPointConfigurationVerifier<MyDataPoint>? CreateConfigurationVerifier(MyReadClient client)
    => new MyVerifier(client);
```

**Abstract methods removed** — move this logic to the injected abstractions:

| Removed abstract method | Where the logic now lives |
|---|---|
| `GetDeviceAddress(TDeviceNode)` | `TDeviceNode` exposes `DeviceIdentifier`; base class reads it directly |
| `GetAllDataPoints(IReadOnlyList<TDataPointGroup>)` | `IDataPointGroupsMapper.ToDataPoints` — called by the base class constructor |
| `GetPollFrequency(TDataPointGroup)` | `IDataPointPollingGroup.PollFrequency` property on the group |
| `GetTransferredChannelsCount(TDataPointValue)` | Handled by the default `ToExternalValues` implementation |
| `AddExternalValuesForDataPoint(List<ExternalValue>, TDataPointValue, DateTime)` | Override `ToExternalValues` on the base class |

**New protected properties** available after construction: `DeviceNode`, `DataPoints`, `DataPointGroups`.

**Implement `IDataPointGroupsMapper`:**

```csharp
public class MyDataPointGroupsMapper
    : IDataPointGroupsMapper<MyDataPoint, MyDataPointGroup, MyDeviceNode, MyCommunication>
{
    public List<MyDataPoint> ToDataPoints(MyDeviceNode deviceNode)
    {
        // extract flat list of data points from the typed node tree
    }

    public MyDataPointGroup CreateGroup(PollFrequency pollFrequency, ImmutableList<MyDataPoint> dataPoints)
        => new MyDataPointGroup(pollFrequency, dataPoints);

    // Group() has a default implementation — override only if custom grouping is needed
}
```

**Override `ToExternalValues` for custom conversion** (optional — the default implementation maps each `TransferredChannel` to an `ExternalValue` using `dataPointValue.Value`):

```csharp
protected override IReadOnlyList<ExternalValue> ToExternalValues(
    IReadOnlyList<MyDataPointValue> values, DateTime timestamp)
{
    var result = new List<ExternalValue>();
    foreach (var dpv in values)
        foreach (var channel in dpv.DataPoint.Channels.TransferredChannels)
            result.Add(new ExternalValue
            {
                Channel = channel.Value,
                Value = dpv.Value,
                Timestamp = timestamp,
                Validity = 1
            });
    return result;
}
```

---

### 4. `OutgoingDataPortBase` — three new type parameters and constructor

The class gains `TRootNode`, `TCommunication`, and `TClientInformation` type parameters and a completely new constructor. The base class now owns configuration mapping, channel resolution, connection management, and value conversion internally.

```
// Before (3 type params)
OutgoingDataPortBase<TDataPoint, TDataPointValue, TClient>

// After (6 type params)
OutgoingDataPortBase<TDataPoint, TDataPointValue, TWriteClient, TRootNode, TCommunication, TClientInformation>
```

New constraints: `TRootNode : IRootConfigurationNode<TCommunication>`, `TCommunication : DataPortCommunication`, `TWriteClient : IWriteClient<TDataPointValue>`, `TClientInformation : IClientInformation`.
The class additionally implements `IExternalOutgoingCommunication<TCommunication>`.

```csharp
// Before
protected MyOutgoingDataPort(
    IReadOnlyList<MyDataPoint> dataPoints,
    Func<MyDataPoint, IReadOnlyList<string>> channelSelector,
    int queueCapacity,
    QueueStrategy overflowStrategy,
    ILogger logger,
    IDelayProvider? delayProvider = null)
    : base(dataPoints, channelSelector, queueCapacity, overflowStrategy, logger, delayProvider)

// After
protected MyOutgoingDataPort(
    MyCommunication communication,
    ILoggerFactory loggerFactory,
    TypedNodeMapper<MyCommunication, MyRootNode> typedNodeMapper,
    IDataPointsMapper<MyRootNode, MyCommunication, MyDataPoint> mapper,
    QueueConfiguration queueConfiguration,
    IDelayProvider delayProvider)
    : base(communication, loggerFactory, typedNodeMapper, mapper, queueConfiguration, delayProvider)

protected override IClientLifecycleManager<MyWriteClient, MyClientInformation> ClientLifecycleManager { get; } = new MyClientLifecycleManager();
public override MyClientInformation ClientInformation { get; } = new MyClientInformation(...);

// Optional — override only if you need pre-connect configuration verification.
// The base provides a virtual factory that returns null by default; the client is supplied at call time.
protected override IDataPointConfigurationVerifier<MyDataPoint>? CreateConfigurationVerifier(MyWriteClient client)
    => new MyVerifier(client);
```

**Abstract members removed:**

| Removed | Replacement |
|---|---|
| `abstract IWriteClient<TDataPointValue> WriteClient { get; }` | `TWriteClient` (returned by `IClientLifecycleManager`) now implements `IWriteClient<TDataPointValue>` directly |
| `abstract Task<TClient> AcquireConnectionAsync(CancellationToken)` | Implement `abstract IClientLifecycleManager<TWriteClient, TClientInformation> ClientLifecycleManager { get; }` |
| `abstract Task ValidateConnectionAsync(CancellationToken)` | Override `protected virtual IDataPointConfigurationVerifier<TDataPoint>? CreateConfigurationVerifier(TWriteClient client)` (default returns `null` — no verification) |
| `abstract Task ReleaseConnectionAsync(CancellationToken)` | `IClientLifecycleManager.ReleaseAsync` is called internally by the base class |
| `virtual void DisposeClient()` | Remove; dispose logic belongs in `IClientLifecycleManager` |
| `abstract IDataPointValueFactory<TDataPoint, TDataPointValue> ValueFactory { get; }` | Implement `ConvertValue` abstract method |

**Abstract properties added** — implement on your subclass:

```csharp
// Client lifecycle manager — required
protected override IClientLifecycleManager<MyWriteClient, MyClientInformation> ClientLifecycleManager { get; } = new MyClientLifecycleManager();

// Client information — uniquely identifies the connection
public override MyClientInformation ClientInformation { get; } = new MyClientInformation(...);
```

**Optional virtual factory method** — override only if you need pre-connect configuration verification. The client is passed in so the verifier can query the live device:

```csharp
protected override IDataPointConfigurationVerifier<MyDataPoint>? CreateConfigurationVerifier(MyWriteClient client)
    => new MyVerifier(client);
```

**Abstract method added** — implement directly on your subclass:

```csharp
protected override MyDataPointValue ConvertValue(ExternalValue externalValue, MyDataPoint dataPoint)
    => new MyDataPointValue(dataPoint, externalValue.Value);
```

**New protected properties** available after construction: `RootNode`, `DataPoints`.

**Implement `IDataPointsMapper`:**

```csharp
public class MyDataPointsMapper
    : IDataPointsMapper<MyRootNode, MyCommunication, MyDataPoint>
{
    public List<MyDataPoint> ToDataPoints(MyRootNode rootNode)
    {
        // traverse the typed node tree and return a flat list of data points
    }
}
```

---

### 5. `IConnectionManager` → `IClientLifecycleManager` — renamed, new namespace, second type parameter, and updated method signatures

The interface was renamed from `IConnectionManager` to `IClientLifecycleManager`, moved from `...Incoming.Client` to `...Client` (shared by both base classes), gained a `TClientInformation` type parameter, and no longer extends `IAsyncDisposable`. Both methods now take a `TClientInformation` (instead of `CancellationToken`-only or `TClient`) as their first argument.

```csharp
// Before (in ViciOne.Suite.DataPort.Extensions.Incoming.Client)
public interface IConnectionManager<TClient> : IAsyncDisposable
{
    Task<TClient> AcquireConnectedAsync(CancellationToken cancellationToken);
    Task ReleaseAsync(TClient client, CancellationToken cancellationToken);
}

// After (in ViciOne.Suite.DataPort.Extensions.Client)
public interface IClientLifecycleManager<TClient, in TClientInformation>
    where TClientInformation : IClientInformation
{
    Task<TClient> AcquireConnectedAsync(TClientInformation clientInformation, CancellationToken cancellationToken);
    Task ReleaseAsync(TClientInformation clientInformation, CancellationToken cancellationToken);
}
```

`IClientInformation` is a marker interface in `ViciOne.Suite.DataPort.Extensions.Client`. Implement it on the type that carries the connection parameters for your protocol (e.g. hostname, port, credentials).

The base classes no longer call `DisposeAsync` on the lifecycle manager — manage its lifetime externally if needed.

---

### 6. `QueueConfiguration` — new type in new namespace

```csharp
// Before
using ViciOne.Suite.DataPort.Extensions.Model;
using ViciOne.Suite.DataPort.Extensions.Queue;

var config = new QueueConfiguration(MaxPendingMessages: 100, Strategy: QueueStrategy.DropOldest);

// After
using ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing;

var config = new QueueConfiguration(QueueStrategy.DropOldest, new QueueSize(100));
```

---

### 7. `IDataPointValidator` → `IDataPointConfigurationVerifier`

```csharp
// Before
public class MyValidator : IDataPointValidator<MyDataPoint>
{
    public void Validate<TClient>(TClient client, IReadOnlyList<MyDataPoint> dataPoints, ILogger logger)
        where TClient : class
    {
        foreach (var dp in dataPoints)
            if (!IsValid(dp))
                throw new ConnectionFailureException($"Data point {dp.AddressString} is misconfigured.");
    }
}

// After
public class MyVerifier : IDataPointConfigurationVerifier<MyDataPoint>
{
    public async ValueTask<List<MisConfiguredDataPoint<MyDataPoint>>> Verify(
        List<MyDataPoint> dataPoints, CancellationToken cancellationToken)
    {
        var failures = new List<MisConfiguredDataPoint<MyDataPoint>>();
        foreach (var dp in dataPoints)
            if (!IsValid(dp))
                failures.Add(new MisConfiguredDataPoint<MyDataPoint>(
                    dp, [new MisMatchingConfiguration("Reason for the mismatch")]));
        return failures;
    }
}
```

The base class throws `InvalidConfigurationException` automatically when failures are returned — do not throw from the verifier itself. Pass it as the optional `configurationVerifier` parameter to `IncomingDataPortBase` or `OutgoingDataPortBase`.

---

### 8. `DataPort.Extensions.Testing` — namespace update only

`IYamlTestingConfig` and `YamlConsistencyBaseTest` reference types from the new `Model.TypedNodes` and `Model.TypedNodes.Mapping` namespaces. Update `using` directives in any test helpers that import these types directly (see namespace table in §2). No API changes to the testing types themselves.
