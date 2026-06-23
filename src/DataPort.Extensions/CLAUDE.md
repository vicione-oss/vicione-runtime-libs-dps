# DataPort.Extensions

Reusable infrastructure library for building protocol-agnostic DataPort implementations in ViciOne Suite. Consumers implement protocol-specific read/write clients; this library handles lifecycle orchestration, polling, queuing, validation, and resilience.

**Target framework**: .NET 9.0
**Root namespace**: `ViciOne.Suite.DataPort.Extensions`

## Architecture

Canonical explanation for humans: [`../../docs/DataPort.Extensions/explanation/Overview.md`](../../docs/DataPort.Extensions/explanation/Overview.md). The summary below is the agent-facing cheat sheet — keep it terse and keep detail out of it.

The library is split into two symmetric halves:

### Incoming (read) data ports
`IncomingDataPortBase` orchestrates: connect → verify data points → start polling jobs → convert values → raise `Received` event → stop polling → disconnect.

Key components:
- `IClientLifecycleManager<TClient, TClientInformation>` — acquire/release protocol connections (shared with Outgoing; lives in top-level `Client/`)
- `IReadClient<TDataPointGroup, TDataPointValue>` — protocol-specific reads
- `PollingJobOrchestrator` / `DataPointGroupPollingJob` — periodic polling per group
- `PeriodicJob` — generic periodic task that runs each tick through a (mandatory) Polly `ResiliencePipeline`
- `IncomingDataPortBase.CreatePollingResiliencePipeline(TDataPointGroup)` — virtual hook returning each group's resilience pipeline; default returns `DefaultResiliencePipeline` (timeout + circuit breaker tuned to the group's poll frequency). The orchestrator calls it per group and threads the result down to `PeriodicJob`
- `IncomingDataPortBase.ToExternalValues` / `ToExternalValue` — virtual hooks for converting read values to `ExternalValue`
- `IDataPointConfigurationVerifier<TDataPoint>` — optional pre-poll validation (lives in top-level `Verification/`)

### Outgoing (write) data ports
`OutgoingDataPortBase` orchestrates: connect → start queue processor → receive `SendAsync` calls → resolve channels → convert → range-validate (both all-or-nothing) → enqueue → write with retry → disconnect.

Key components:
- `ChannelResolver<TDataPoint>` — O(1) channel-name → data point lookup (pre-built at connect time)
- `OutgoingDataPortBase.ConvertToDataPointValue` — abstract method converting an `ExternalValue` to a `DataPointValueConversion<TDataPointValue>` (`Converted…`/`NotConverted…`); failures reported as data, not thrown
- `DataPointValueValidator<TDataPoint, TDataPointValue>` — range validation only (type is compile-time via `ITypedDataPointValue<TValue>`); drops entire batch if any value fails
- `QueueProcessor<TValue>` — background write loop with peek/mark retry pattern
- `PrependableChannelQueue<T>` / `ChannelQueue<T>` — bounded `System.Threading.Channels` queue with backpressure
- `IRetryPolicy` — `BoundedRetryPolicy` or `InfiniteRetryPolicy`
- `RetryBackoffCalculator` — exponential backoff (`min(initial × 2^(n-1), max)`)
- `IWriteClient<TDataPointValue>` — protocol-specific writes

## Key models

| Type                           | Location            | Purpose                                                                |
|--------------------------------|---------------------|------------------------------------------------------------------------|
| `IDataPoint`                   | `Model/DataPoints/` | Addressable data element; has `Channels`, `Identifier`, `DataTypeName` |
| `IPollingDataPoint`            | `Model/DataPoints/` | Extends `IDataPoint` with `PollFrequency`                              |
| `ITypedDataPoint<TValue>`      | `Model/DataPoints/` | `IDataPoint` declaring its .NET value type; `CreateTypedValue(TValue)` |
| `IDataPointValue`              | `Model/DataPoints/` | Non-generic value view; `DataPoint`, `object? Value`, `IsInValueRange()` |
| `ITypedDataPointValue<TValue>` | `Model/DataPoints/` | Strongly-typed value payload (`TypedValue`); removes runtime type check |
| `Channels`                     | `Model/DataPoints/` | `record struct` of `Affected` + `Transferred` channel lists (`IReadOnlyList`) |
| `PollFrequency`                | `Model/DataPoints/` | `TimeSpan` wrapper with implicit conversions                           |
| `IRootConfigurationNode<TCom>` | `Model/TypedNodes/` | Root of typed configuration hierarchy; has `DeviceIdentifier`          |
| `IConfigurationNode`           | `Model/TypedNodes/` | Intermediate node; holds child nodes and data point nodes              |

## Typed node mapping

`TypedNodeMapper<TCommunication, TRootNode>` converts raw configuration into a strongly-typed tree:
1. Validate root communication via `IRootConfigurationNodeMapper.Validate`; fail fast if invalid
2. Create root node via `IRootConfigurationNodeMapper.CreateRootNode`
3. Build `LinkedNode` tree from flat nodes (establishes parent links)
4. Walk the tree recursively — for each node: validate via its mapper, then map to a typed node. Validation and mapping are interleaved per node, not separate passes

Related files live in `Model/TypedNodes/Mapping/` and its `Properties/` subfolder.

## Exception types (`Exceptions/`)

| Exception                       | Meaning                                               |
|---------------------------------|-------------------------------------------------------|
| `ConnectionFailureException`    | Device connection lost or data point misconfiguration |
| `DataConversionException`       | Value conversion failed                               |
| `DataRetrievalException`        | Read operation failed                                 |
| `InvalidConfigurationException` | Configuration validation failed                       |
| `MaxRetriesExceededException`   | Write exceeded max retry count                        |
| `QueueBackpressureException`    | Item dropped under backpressure during retry          |

## Important conventions

**Validation semantics**: The outgoing path has two all-or-nothing gates — conversion then range validation. If any value in a batch fails to convert, or any converted value is out of range, the entire batch is discarded. (Unmatched channels are the exception: they are logged and skipped, not batch-dropping.) This is intentional — do not change to partial validation without discussion. There is no runtime type check; type correctness is guaranteed at compile time by `ITypedDataPointValue<TValue>`.

**Queue peek/mark pattern**: `QueueProcessor` peeks the head item before writing, then marks it as read only on success. This allows retry without re-enqueue. Do not refactor to dequeue-first.

**Thread safety**: Base class lifecycle methods (`ConnectAsync`, `DisconnectAsync`, `SendAsync`) are NOT thread-safe and must be called from a single thread.

**Logging**: Each major component has a companion `*Logs` class with structured log definitions and event IDs. Add new log messages there rather than inline.

**Performance hot paths**: `OutgoingDataPortBase.SendAsync` and `QueueProcessor` use pre-allocated `List` capacities and indexed `for` loops — avoid introducing LINQ in those paths.

**Logger access**: `OutgoingDataPortBase` holds a private `ILogger` for its own use and stores the injected `ILoggerFactory`. Subclasses that need to log should create their own typed logger from the factory rather than expect an inherited `Logger` property.

## Testing conventions

**Stubs over mocks**: Prefer custom stub classes over NSubstitute for `IWriteClient`, `IReadClient`,
and similar interfaces. Use a primary-constructor stub with a lambda parameter to inject behavior.
NSubstitute is acceptable for simple argument capture but generates quality warnings on complex setups.

**Test helpers** (`DataPort.Extensions.Testing`): provides `EventualAssertions` (async polling
assertions), `TestLogging` (capture log output), and `YamlConsistencyBaseTest` (YAML-driven
consistency tests). Reference this package in new test projects rather than reimplementing these.

## Folder structure

```
src/DataPort.Extensions/
├── Client/                       # IClientLifecycleManager, IClientInformation (shared)
├── Exceptions/
├── Verification/                 # IDataPointConfigurationVerifier, MisConfiguredDataPoint (shared)
├── Incoming/
│   ├── Client/                   # IReadClient
│   └── Polling/
│       └── Job/                  # PeriodicJob, DefaultResiliencePipeline
├── Model/
│   ├── DataPoints/               # IDataPoint, IDataPointValue, Channels, PollFrequency, IPollingDataPoint, IDataPointPollingGroup
│   ├── Mapping/                  # IDataPointsMapper, IDataPointGroupsMapper
│   └── TypedNodes/
│       └── Mapping/
│           └── Properties/       # InvalidPropertyException, PropertyValidationErrorType, PropertyValueConverter
└── Outgoing/
    ├── ChannelResolution/        # ChannelResolver, ChannelResolutions
    ├── Client/                   # IWriteClient
    ├── QueueProcessing/
    │   ├── Bounded/              # ChannelQueue, ChannelQueueBase, ChannelQueueEnumerator
    │   ├── Prependable/          # PrependableChannelQueue, PrependableChannelQueueEnumerator, HeadSource
    │   └── Retry/                # IRetryPolicy, BoundedRetryPolicy, InfiniteRetryPolicy, RetryBackoffCalculator
    └── ValueValidation/          # DataPointValueValidator and result types
```
