# DataPort.Extensions

Reusable infrastructure library for building protocol-agnostic DataPort providers in ViciOne Suite. Consumers implement protocol-specific read/write clients; this library handles lifecycle orchestration, polling, queuing, validation, and resilience.

**Target framework**: .NET 9.0
**Root namespace**: `ViciOne.Suite.DataPort.Extensions`

## Architecture

The library is split into two symmetric halves:

### Incoming (read) data ports
`IncomingDataPortBase` orchestrates: connect → verify data points → start polling jobs → convert values → raise `Received` event → stop polling → disconnect.

Key components:
- `IClientLifecycleManager<TClient>` — acquire/release protocol connections
- `IReadClient<TDataPointGroup, TDataPointValue>` — protocol-specific reads
- `PollingJobOrchestrator` / `DataPointGroupPollingJob` — periodic polling per group
- `PeriodicJob` — generic periodic task with Polly circuit breaker and timeout
- `IDataPointValueConverter<TDataPoint, TDataPointValue>` — convert read values to `ExternalValue`
- `IDataPointConfigurationVerifier<TDataPoint>` — optional pre-poll validation

### Outgoing (write) data ports
`OutgoingDataPortBase` orchestrates: connect → start queue processor → receive `SendAsync` calls → resolve channels → convert → validate (all-or-nothing) → enqueue → write with retry → disconnect.

Key components:
- `ChannelResolver<TDataPoint>` — O(1) channel-name → data point lookup (pre-built at connect time)
- `IExternalValueConverter<TDataPoint, TDataPointValue>` — convert `ExternalValue` to protocol value
- `DataPointValueValidator<TDataPoint, TDataPointValue>` — type + range validation; drops entire batch if any value fails
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
| `IDataPointValue<T>`           | `Model/DataPoints/` | Value + data point; `ValidateType()`, `IsInValueRange()`               |
| `Channels`                     | `Model/DataPoints/` | Immutable record of `AffectedChannel` and `TransferredChannel` lists   |
| `PollFrequency`                | `Model/DataPoints/` | `TimeSpan` wrapper with implicit conversions                           |
| `IRootConfigurationNode<TCom>` | `Model/TypedNodes/` | Root of typed configuration hierarchy; has `DeviceIdentifier`          |
| `IConfigurationNode`           | `Model/TypedNodes/` | Intermediate node; holds child nodes and data point nodes              |

## Typed node mapping

`TypedNodeMapper<TCommunication, TRootNode>` converts raw configuration into a strongly-typed tree:
1. Validate via FluentValidation
2. Create root node via `IRootConfigurationNodeMapper`
3. Walk nodes as `LinkedNode` (establishes parent links)
4. Map to typed nodes via `IBranchConfigurationNodeMapper` / `IDataPointNodeMapper`

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

**Validation semantics**: Outgoing validation is all-or-nothing. If any value in a batch fails type or range validation, the entire batch is discarded. This is intentional — do not change to partial validation without discussion.

**Queue peek/mark pattern**: `QueueProcessor` peeks the head item before writing, then marks it as read only on success. This allows retry without re-enqueue. Do not refactor to dequeue-first.

**Thread safety**: Base class lifecycle methods (`ConnectAsync`, `DisconnectAsync`, `SendAsync`) are NOT thread-safe and must be called from a single thread.

**Logging**: Each major component has a companion `*Logs` class with structured log definitions and event IDs. Add new log messages there rather than inline. Event IDs 1000–1299 are reserved for `PeriodicJob`.

**Performance hot paths**: `OutgoingDataPortBase.SendAsync` and `QueueProcessor` use pre-allocated `List` capacities and indexed `for` loops — avoid introducing LINQ in those paths.

**Logger access**: `OutgoingDataPortBase` exposes a `protected ILogger Logger` property. Subclasses use `Logger`; the base class uses it directly. The logger is initialized in the constructor from `ILoggerFactory`.

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
├── Exceptions/
├── Incoming/
│   ├── Client/                   # IClientLifecycleManager, IReadClient
│   ├── Polling/
│   │   └── Job/                  # PeriodicJob, DefaultResiliencePipeline
│   ├── ValueConversion/          # IDataPointValueConverter
│   └── Verification/             # IDataPointConfigurationVerifier
├── Model/
│   ├── DataPoints/               # IDataPoint, IDataPointValue, Channels, PollFrequency
│   ├── Mapping/                  # IDataPointsMapper, IDataPointGroupsMapper
│   └── TypedNodes/
│       └── Mapping/
│           └── Properties/       # InvalidPropertyException, PropertyValidationErrorType, PropertyValueConverter
└── Outgoing/
    ├── ChannelResolution/        # ChannelResolver, ChannelResolutions
    ├── Client/                   # IWriteClient
    ├── QueueProcessing/
    │   ├── Bounded/              # ChannelQueue
    │   ├── Prependable/          # PrependableChannelQueue
    │   └── Retry/                # IRetryPolicy, BoundedRetryPolicy, InfiniteRetryPolicy, RetryBackoffCalculator
    └── Value/
        ├── Conversion/           # IExternalValueConverter
        └── Validation/           # DataPointValueValidator and result types
```
