# Build order — the code shape of each step

Snippets are abridged from dp-modbus `src/Modbus.Client/` and cip `src/AllenBradley.Logix/`. The
authoritative prose lives in dps `docs/DataPort.Extensions/how-to/`; this file is the fast path.

Naming: `X` = the provider prefix (`Modbus`, `ModbusServer`, `Logix`, …). s7 and cip drop the
prefix where the namespace already carries it (`DeviceNode`, `IncomingDataPort`); dp-modbus keeps
it because one repo hosts two providers. Follow whichever the repo you're in already does.

---

## 1. YAML ruleset

`src/<Provider>/<Provider>.yaml`, copied to output:

```xml
<None Update="ModbusClient.yaml"><CopyToOutputDirectory>Always</CopyToOutputDirectory></None>
```

See [yaml-schema.md](yaml-schema.md).

## 2. Communication record

The root configuration object the engine deserializes and hands to both ports. One property per
YAML property on the root node, same id, same type.

```csharp
[Communication("ModbusClientDataPort")]          // == Root.Id in the YAML (compared ignoring case)
public sealed record ModbusClientDataPortCommunication : DataPortCommunication
{
    public byte TransportType { get; init; }      // UInt8 in YAML -> byte here
    public string IpAddress { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 502;
    public int PollInterval { get; init; } = 1000;
    public int MaxPendingMessages { get; init; } = 100_000;
    public byte Strategy { get; init; }

    /// <summary>Max write retry attempts (-1 = infinite). No YAML counterpart (engine default).</summary>
    public long MaxRetryAttempts { get; init; } = -1;
}
```

Defaults must match the YAML `DefaultValue`s. A property with no YAML counterpart (like
`MaxRetryAttempts`) needs the XML comment *and* an entry in the consistency test's
`ExcludedCommunicationProperties`.

## 3. Root node + mapper + validator

```csharp
public record ModbusRootNode(ModbusClientDataPortCommunication OriginalCommunication)
    : IRootConfigurationNode<ModbusClientDataPortCommunication>
{
    public IConfigurationNode? ParentConfigurationNode
    {
        get => null;
        set => throw new InvalidOperationException("The Modbus root node cannot have a parent.");
    }
    public List<IConfigurationNode> ConfigurationNodes { get; } = [];
    public List<IDataPointNode> DataPointNodes { get; } = [];

    // Gate what may attach directly under the root
    public bool CanBeAdded(IConfigurationNode node) => node is IBranchConfigurationNode;
    public bool CanBeAdded(IDataPointNode node) => false;

    public DeviceIdentifier DeviceIdentifier => new(ToClientInformation().Endpoint);

    internal ModbusClientInformation ToClientInformation() => /* pool key, see step 8 */;
}
```

The root node is also where the **pool key** is built (`ToClientInformation()`), so both ports and
the connection tester derive the same identity from the same code.

The root mapper implements `IRootConfigurationNodeMapper<TRoot, TComm>`: `CreateRootNode` plus a
`Validate` that runs once, up front, over the whole communication (a FluentValidation
`AbstractValidator<TCommunication>` — e.g. `ModbusClientDataPortCommunicationValidator`). Failures
become one `InvalidConfigurationException` with per-field detail *before* any node is mapped and
before the network is touched.

One root type may serve several root design ids when the device kinds share one shape — cip maps
all of its controller kinds onto one `DeviceNode` and tells them apart with
`DeviceNode.KindOf(designId)`.

## 4. Branch nodes (optional)

Structural groupings that carry context downward — a folder, a register area, a server unit. Skip
if the protocol is flat.

```csharp
internal sealed record ModbusAreaNode : IBranchConfigurationNode
{
    public const string CoilTypeId = "Coil";
    public const string HoldingRegisterTypeId = "HoldingRegister";

    public ModbusAreaNode(LinkedNode originalNode, RegisterArea registerArea) { … }

    public LinkedNode OriginalNode { get; init; }
    internal RegisterArea RegisterArea { get; }      // structural, not a YAML property
}
```

A mapper claims nodes by `TargetLinkedNodeTypeId` (== the YAML `MappingId`). One mapper class can
be registered several times with different ids when the shape is identical — dp-modbus registers
`ModbusAreaNodeMapper` four times, once per register area:

```csharp
new ModbusAreaNodeMapper(ModbusAreaNode.CoilTypeId, RegisterArea.Coil),
new ModbusAreaNodeMapper(ModbusAreaNode.DiscreteInputTypeId, RegisterArea.DiscreteInput),
```

Rules that span sibling leaves (overlapping addresses) belong in the **branch** mapper's `Validate`,
which sees the unmapped children — `ModbusAreaNodeMapper` checks register overlaps there.

Override `IsTargetMapperFor(LinkedNode)` only when two node types share one design id and must be
told apart by more than the id (e.g. presence of a property).

## 5. Leaf nodes + mappers + validators

The leaf carries the address and the `Channels`:

```csharp
internal sealed record ModbusNumericNode(LinkedNode OriginalNode, int Address, WordOrder WordOrder)
    : IDataPointNode
{
    public const string AddressPropertyName = "Address";
    public const string IntegerTypeId = "DataPointInteger";

    internal ModbusValueType ValueType { get; init; }   // from LinkedNode.ValueType, not a YAML property
    public IConfigurationNode? Parent { get; set; }

    public Channels Channels { get; } = new(
        Channels.AffectedChannels([.. OriginalNode.AffectedChannels]),
        Channels.TransferredChannels([.. OriginalNode.TransferredChannels]));
}
```

Put property ids and design ids in `const string` fields on the node — the mappers, tests, and test
data builders all reference them, so a rename stays a single edit.

`Validate` runs immediately before `Map` and gates it; the first failure aborts construction.
**Validate the raw `LinkedNode`, not the mapped node**: a missing or mistyped property then becomes
a `ValidationFailure` the editor can show, instead of an exception thrown out of `Validate`. cip's
validators show the shape — `AbstractValidator<LinkedNode>` with `HasPropertyOfType<T>` (from
`ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping`) guarding every typed read:

```csharp
internal sealed class DataPointNodePropertyValidator : AbstractValidator<LinkedNode>
{
    public DataPointNodePropertyValidator()
    {
        RuleFor(static node => node)
            .Must(static node => node.HasPropertyOfType<int>(PollFrequencyPropertyName))
            .WithMessage(static node => $"Node '{node.Name}' ({node.DesignId}): property '{PollFrequencyPropertyName}' is required and must be an integer.");
        RuleFor(static node => node)
            .Must(static node => node.GetRequiredPropertyValue<int>(PollFrequencyPropertyName) > 0)
            .When(static node => node.HasPropertyOfType<int>(PollFrequencyPropertyName))
            .WithMessage(…);
    }
}
```

Range and cross-field rules (`Address + RegisterCount <= 65536`) go in the same validator.

`Map` then reads every property the schema declares:

```csharp
public ModbusNumericNode Map(LinkedNode node)
{
    var address = PropertyValueConverter.To<int>(
        node.GetRequiredProperty(ModbusNumericNode.AddressPropertyName).Value,
        ModbusNumericNode.AddressPropertyName);

    // UInt8 YAML properties arrive boxed as byte: To<int> rejects them, ToEnum handles
    // the byte/int/string representations.
    var wordOrder = PropertyValueConverter.ToEnum<WordOrder>(
        node.GetRequiredProperty(ModbusNumericNode.WordOrderPropertyName).Value,
        ModbusNumericNode.WordOrderPropertyName);

    // Grouped leaf: the concrete width comes from the engine-selected DataTypes entry.
    var valueType = node.ValueType is null
        ? throw new InvalidOperationException($"Numeric leaf '{node.Name}' has no selected data type.")
        : ModbusValueTypeExtensions.FromClrType(node.ValueType);

    return new ModbusNumericNode(node, address, wordOrder) { ValueType = valueType };
}
```

Simpler protocols use `node.GetRequiredPropertyValue<T>("Id")` directly; reach for
`PropertyValueConverter` when the boxed representation needs handling. Several leaf types with the
same property set share an abstract mapper base (cip `LogixTagNodeMapper<TNode>`).

## 6. Mapper composition root

One `internal static class` per provider, returning the assembled mapper. This is the single place
that knows the full node inventory:

```csharp
internal static class ModbusNodeMapper
{
    internal static TypedNodeMapper<ModbusClientDataPortCommunication, ModbusRootNode> Instance()
    {
        IReadOnlyList<IBranchConfigurationNodeMapper<IBranchConfigurationNode>> branchMappers =
        [ new ModbusFolderNodeMapper(), new ModbusAreaNodeMapper(…), … ];
        IReadOnlyList<IDataPointNodeMapper<IDataPointNode>> dataPointMappers =
        [ new ModbusBoolNodeMapper(), new ModbusNumericNodeMapper(ModbusNumericNode.IntegerTypeId), … ];
        return new TypedNodeMapper<…>(new ModbusRootConfigurationNodeMapper(), branchMappers, dataPointMappers);
    }
}
```

A node no mapper claims is a hard `NotSupportedException` at construction — never silently skipped.

## 7. Runtime data points

The flat runtime form. `IPollingDataPoint` (adds `PollFrequency`) if the port polls; plain
`IDataPoint` is enough for write-only and for push. Declare the value type with
`ITypedDataPoint<TValue>` — the type argument *is* the type, which is why the write path needs no
runtime type check.

- `CreateTypedValue(TValue)` — the framework member; the read client builds returned values with it.
- `ConvertValue(object?)` — a provider convention, not a framework member (dps stubs, dp-modbus,
  cip): the outgoing port's `ConvertToDataPointValue` delegates to it. It returns
  `ConvertedDataPointValue<…>` or `NotConvertedDataPointValue<…>` carrying the reason. **Return the
  failure, never throw**: the base class logs it and drops the batch cleanly.
- `IsInValueRange()` on the value — the second outgoing gate.

Adding a data type to an existing provider is normally: one node record + mapper (+ validator), one
data-point record, one entry in the composition root, one YAML leaf, plus tests. dp-s7-absolute's
`docs/Siemens.S7.Absolute.Documentation/how-to/add-a-data-type.md` walks the richest version of this.

## 8. Connection: information, client, pool

**`IClientInformation`** — a value type with value equality (dp-modbus and cip use a
`readonly record struct`), because equality *is* the "same connection" test. Deliberately exclude
per-data-point values: dp-modbus keeps `UnitId` out of `ModbusClientInformation` because it varies
per register. When it also carries connect parameters that must not split the pool (dp-modbus's
TCP connect timeout and retries — "first connection wins"), override `Equals`/`GetHashCode` to
compare only the identity. Factories per transport (`ModbusClientInformation.Tcp(...)` /
`.Rtu(...)`) keep the branching in one place; normalize inputs there (dp-modbus **trims** serial
port names — a stray space misroutes pseudo-terminal detection and splits pool identity).

**The client** implements `IReadClient<TGroup, TValue>` and/or `IWriteClient<TValue>`. Serialize
access to a non-thread-safe protocol handle with a `SemaphoreSlim` gate. Wrap protocol failures in
`ConnectionFailureException`. Reset the link on a failed read/write (`Disconnect()`) so a
connected-but-dead socket or serial line recovers on the next tick instead of staying stuck.

**The pool** implements `IClientLifecycleManager<TClient, TClientInformation>` — one connection per
distinct client information, reference counted, disposed at zero. Both directions on the same
endpoint share it. Start from cip's generic `ClientPool<TClient, TKey>`
(`src/AllenBradley.Client.Shared/Pool/ClientPool.cs`): one `Lock` over the dictionary, a per-entry
gate so concurrent acquirers await the first caller's connect instead of opening duplicates, and
each caller waiting with its own `CancellationToken`, so one caller cancelling does not cancel the
shared connect. Its client factory is a constructor argument, so tests build their own pool.

The process-wide instance is `internal static GetInstance(ILoggerFactory)` — production only.
Older pools (`S7ClientPool`, `ModbusClientPool`) hide their factory and add an
`internal static CreateTestInstance(ILoggerFactory, IXClientFactory)` for tests instead.

## 9. Data-point mappers

`IDataPointsMapper<TRoot, TComm, TDataPoint>.ToDataPoints` walks the typed tree and emits one point
per address. `IDataPointGroupsMapper` extends it with `CreateGroup`, so one class can serve both
directions; the interface's default `Group`/`ToDataPointGroups` partition by `PollFrequency`.

Make the mapper **direction-aware** rather than writing two:

```csharp
public ModbusDataPointsMapper() : this(static _ => true) { }                       // incoming: all areas
public static ModbusDataPointsMapper ForWritableAreas() => new(static a => a.IsMasterWritable());
```

Precompute batching in `CreateGroup` where the protocol supports block reads — dp-modbus plans
contiguous register spans per (unit, area) there, so the client just issues them.

## 10. The ports

Both follow the same two-constructor shape: a **public** one the engine calls, and an **internal**
one for tests that injects the lifecycle manager, `TimeProvider`, and/or `IDelayProvider`.

```csharp
public sealed class ModbusClientDataPortIncoming
    : IncomingDataPortBase<TComm, TRoot, TDataPoint, TGroup, TValue, TClient, TClientInformation>
{
    public ModbusClientDataPortIncoming(ModbusClientDataPortCommunication communication, ILoggerFactory loggerFactory)
        : this(communication, loggerFactory, TimeProvider.System, null) { }

    internal ModbusClientDataPortIncoming(…, TimeProvider timeProvider,
        IClientLifecycleManager<IModbusClient, ModbusClientInformation>? clientLifecycleManager)
        : base(communication, loggerFactory, ModbusNodeMapper.Instance(), new ModbusDataPointGroupsMapper(), timeProvider)
    {
        ClientInformation = DeviceNode.ToClientInformation();
        ClientLifecycleManager = clientLifecycleManager ?? ModbusClientPool.GetInstance(loggerFactory);
    }

    public override ModbusClientInformation ClientInformation { get; }
    protected override IClientLifecycleManager<…> ClientLifecycleManager { get; }
}
```

A push protocol's incoming port is hand-written (Step 0 in SKILL.md); its receive callback takes
the place of the polling job.

The outgoing base constructor takes a `QueueConfiguration` (from the communication's
`MaxPendingMessages` / `Strategy`) and an `IDelayProvider` — neither is optional; production passes
`DefaultDelayProvider`, tests `InstantDelayProvider`. Outgoing also has a `RetryPolicy` and the
required `ConvertToDataPointValue` override, which normally just delegates to the data point:

```csharp
protected override IRetryPolicy RetryPolicy =>
    new BoundedRetryPolicy(_maxRetryAttempts, _loggerFactory.CreateLogger<BoundedRetryPolicy>());

protected override DataPointValueConversion<IModbusDataPointValue> ConvertToDataPointValue(
    ExternalValue externalValue, IModbusDataPoint dataPoint) => dataPoint.ConvertValue(externalValue.Value);
```

**Optional overrides**, all with working defaults — override only with a reason:

| Hook | Default | Override when |
|------|---------|---------------|
| `ToExternalValues` / `ToExternalValue` | one value per transferred channel, `Validity = 1` | different channel mapping or value transform (keep it allocation-light); a value that fans out of one received message carries that message's validity (see SKILL.md) |
| `CreatePollingResiliencePipeline(group)` | timeout + circuit breaker scaled to the group's poll frequency | per-group tuning or a shared breaker |
| `CreateConfigurationVerifier(client)` | none | addresses can be checked against the live device on connect — both directions; cip verifies its tags in both ports |
| `RetryPolicy` (outgoing) | `InfiniteRetryPolicy` | informational writes whose receiver keeps no state → `BoundedRetryPolicy` |
| `DisposeAdditionalResourcesAsync()` | nothing | the port owns resources of its own; runs before the base releases its own, no `base` call needed |
| `SendAsync` (outgoing, virtual) | queue and return | almost never — the queue semantics in [conventions.md](conventions.md) depend on it |

Neither base exposes a `Logger` — both keep a private one. Subclasses create their own typed logger
from the injected `ILoggerFactory`.

Base lifecycle methods (`ConnectAsync`, `DisconnectAsync`, `SendAsync`) are **not thread-safe** and
must be called from one thread. `DisconnectAsync` is reversible; `DisposeAsync` is terminal. The
library never auto-reconnects — reconnection is the client's job (see step 8).

## 11. Connection tester

Reachability probe for the editor. It must **bypass the pool** so it never disturbs live refcounts,
and build its client information from the same root-node code path as the ports:

```csharp
public sealed class ModbusClientConnectionTester : IDataPortConnectionTester<ModbusClientDataPortCommunication>
{
    public static async Task TestConnectionAsync(ModbusClientDataPortCommunication communication)
    {
        var clientInformation = new ModbusRootNode(communication).ToClientInformation();
        using var client = new ModbusClientFactory(NullLoggerFactory.Instance).Create(clientInformation);
        await client.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
        await client.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
```

## 12. Packaging

`src/<Provider>/metadata.json` (CI substitutes the placeholders):

```json
{ "Name": "modbusdataport", "Version": "__VERSION__", "MinCoreVersion": "__MIN_CORE_VERSION__" }
```

Plus, per provider, a block of GitLab CI component jobs in `.gitlab-ci.yml` — metadata, publish,
package, upload to dev/staging, promote. Copy dp-modbus's block for one module and rename; see
[repo-layout.md](repo-layout.md).
