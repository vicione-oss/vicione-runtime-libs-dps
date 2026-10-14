# Testing a DataPort

Stack: **xUnit v3** on the Microsoft Testing Platform, **AwesomeAssertions** (never
FluentAssertions — it conflicts and was dropped over a licence change), **NSubstitute**,
**FluentValidation** for mapper rules, `FakeTimeProvider` for polling, `FakeLogger` for log
assertions, and `ViciOne.Suite.DataPort.Extensions.Testing` for the DataPort-specific helpers.

## Running

Take the command from the repo's `AGENTS.md`; the repos select suites in two ways.

**Filter query** (dp-modbus):

```bash
dotnet test --project tests/<Project>/<Project>.csproj --filter-query "/[(Category!=System)&(Category!=Integration)]"
dotnet test --solution <repo>.slnx --filter-query "/[(Category!=System)&(Category!=Integration)]"
```

That is the filter dp-modbus CI passes as `test_filter`. dp-mqtt, dp-opcua and dp-sql exclude
`Interoperability` as well.

**`test-suite` property** (s7, cip): `tests/Directory.Build.props` injects the filter, and
`--filter-query` clashes with it. Unset runs the unit suite, so a bare `dotnet test` never touches
hardware; an unknown value fails the build.

```bash
dotnet test                                   # unit
dotnet test -p:test-suite=integration         # needs a device
dotnet test --project tests/<Project>/<Project>.csproj --filter-class "<fully.qualified.ClassName>"
```

MTP specifics that bite:

- No `--` separator; pass filter args straight to `dotnet test`.
- A project path needs `--project` (or `--solution` for the `.slnx`), and `global.json` must select
  the MTP runner.
- **No VSTest `--filter`/`--nologo`** — use `--filter-query`, `--filter-class`, `--filter-method`,
  `--filter-namespace`, `--filter-trait`, `--filter-not-class`. One style per command.
- All-tests-filtered-out exits 8; suppress with `--ignore-exit-code 8` when that's expected.

## The expected test set per provider

Mirroring dp-modbus `tests/Modbus.Client.Tests/`:

| Folder | Covers |
|---|---|
| `Yaml/XYamlConsistencyTests.cs` | `YamlConsistencyBaseTest` — YAML ↔ C# parity |
| `Yaml/XNodeDirectionTests.cs` | pins `TransferDirections` per node type |
| `Yaml/XDependentPropertiesTests.cs` | pins the `DependentProperties` visibility map |
| `Mapping/` | each node mapper + validator; the data-points and groups mappers; the communication validator |
| `Client/` | the client, its factory, the client information (pool identity), the pool |
| `Incoming/` | the incoming port: poll → publish, failure keeps polling |
| `Outgoing/` | the outgoing port: convert/validate gates, retry, queue behavior |
| `Integration/` | round-trips against a real device, a container or an in-process server |
| `Infrastructure/` | `XCommunicationTestData` — builds `Node` lists the way the engine does |

Mark a test `[Trait("Category", "Integration")]` only when it needs something outside the test
process — a device, a container, a serial line. dp-modbus's client round-trip runs against an
in-process loopback server, carries no trait and runs in CI; its server round-trip carries the
trait.

## YAML consistency test

```csharp
public class ModbusYamlConsistencyTests :
    YamlConsistencyBaseTest<ModbusClientDataPortCommunication, ModbusYamlConsistencyTests, ModbusRootNode>,
    IYamlTestingConfig<ModbusClientDataPortCommunication, ModbusRootNode>
{
    public static string YamlFileName => "ModbusClient.yaml";
    public static TypedNodeMapper<…> Mapper => ModbusNodeMapper.Instance();

    // MaxRetryAttempts has no YAML counterpart: engine-level write-retry default, not user-configured.
    public static HashSet<string> ExcludedCommunicationProperties => ["MaxRetryAttempts"];
}
```

The node-level sets are keyed by node type — or by an interface or base type, which covers every
node that implements it. From cip and the dps stub test:

```csharp
public static Dictionary<Type, HashSet<string>> ExcludedConfigurationNodeProperties => new()
{
    [typeof(ArrayContainerNode)] = [nameof(ArrayContainerNode.ArrayDataType)],
};

public static Dictionary<Type, HashSet<string>> ExcludedDataPointNodeProperties => new()
{
    [typeof(DataPointNodeBase)] = [nameof(DataPointNodeBase.Description)],
};
```

Only public members take part in the comparison — dp-modbus keeps `ValueType` `internal` on its
leaves and needs no exclusion for it.

Every exclusion carries a comment saying why. The YAML must reach the test output — it does via the
`src` project's `CopyToOutputDirectory: Always`, or add a `<Content Include=… Link=…>` in the test
project.

The test tells leaves from containers by whether the YAML node has `ChildNodes`. A data point with
envelope children therefore lands in the configuration-node half of the comparison; see
[yaml-schema.md](yaml-schema.md).

## Test-data builder

Build `Node` lists exactly as the engine supplies them — parents linked by `ParentId`, `UInt8`
properties boxed as `byte`, `ValueType` set for grouped leaves:

```csharp
public static Node NumericLeaf(ModbusValueType valueType, Guid areaId, string channel, int address, byte wordOrder = 0) =>
    new()
    {
        DesignId = valueType is Float32 or Float64 ? ModbusNumericNode.FloatTypeId : ModbusNumericNode.IntegerTypeId,
        Id = NewGuid(), ParentId = areaId, ValueType = ClrTypeOf(valueType),
        AffectedChannels = [channel], TransferredChannels = [channel],
        Properties = new Dictionary<string, Property>
        {
            [ModbusNumericNode.AddressPropertyName] = new() { Value = address },
            [ModbusNumericNode.WordOrderPropertyName] = new() { Value = wordOrder },  // boxed byte
        },
    };
```

Getting the boxing wrong here hides a real mapper bug, so keep this helper faithful to the engine.

## Determinism

- **Polling**: inject `FakeTimeProvider` through the internal constructor and `Advance(...)` past the
  interval. The job body still runs on a background thread, so assert *eventually*, not immediately
  — `EventualAssertions.Eventually(() => …).Should()…` from the Testing package exists for this.
- **Retry/backoff**: inject `InstantDelayProvider` (in the main Extensions package,
  `ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing`) so retries don't wait; assert on
  call counts with a stub client that fails N times then succeeds.
- **Pools**: never the process-wide `GetInstance()` in a test — it shares a singleton and breaks
  isolation. Build a pool with a test client factory (`CreateTestInstance(loggerFactory,
  clientFactory)` where the pool hides its factory).
- **Logging**: assert on logs with `FakeLogger` / `FakeLogCollector`
  (`Microsoft.Extensions.Diagnostics.Testing`). `TestLogging.CreateLoggerFactory()` from the Testing
  package only writes to the console and is silent by default (`LogLevel.None`) — it is for
  watching a test, not for asserting.

## Stubs over mocks

Prefer a small primary-constructor stub with a lambda parameter for `IReadClient` / `IWriteClient`
and similar seams. NSubstitute is fine for simple argument capture but generates quality warnings
on complex setups — and those warnings fail the build.
