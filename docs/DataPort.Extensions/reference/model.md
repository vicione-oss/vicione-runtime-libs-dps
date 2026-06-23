# Model Components

**Namespace**: `ViciOne.Suite.DataPort.Extensions.Model`

> **Beyond the API summaries.** The generated [API reference](api/index.md) carries each type's
> signatures and members. This page adds the *map*: it groups the runtime abstractions, value types, and
> configuration-mapping types by the role they play, and points to the design rationale —
> [Typed node model](../explanation/configuration/typed-node-model.md) and
> [Internal data model](../explanation/configuration/internal-data-model.md).

## Data point abstractions

**Namespace**: `ViciOne.Suite.DataPort.Extensions.Model.DataPoints`

- [`IDataPoint`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.IDataPoint)
- [`ITypedDataPoint<TValue>`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.ITypedDataPoint%601)
- [`IPollingDataPoint`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.IPollingDataPoint)
- [`IDataPointValue`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.IDataPointValue)
- [`ITypedDataPointValue<TValue>`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.ITypedDataPointValue%601)

→ Why the model is split across these interfaces, and why type is carried by the CLR rather than a
discriminator: [Internal data model](../explanation/configuration/internal-data-model.md#why-the-data-point-model-is-split-across-several-interfaces)

### IDataPointPollingGroup

[`IDataPointPollingGroup<TDataPoint>`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.IDataPointPollingGroup%601)
is the unit an incoming port polls: one polling job runs per group.

## Value types

- [`Channels`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.Channels) — a `record struct`
  pairing the `Transferred` and `Affected` channel lists, with
  [`AffectedChannel`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.AffectedChannel) and
  [`TransferredChannel`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.TransferredChannel)
  wrappers.
- [`PollFrequency`](xref:ViciOne.Suite.DataPort.Extensions.Model.DataPoints.PollFrequency) — a polling
  interval that converts implicitly to and from `TimeSpan`.

## Configuration records

[`QueueConfiguration`](xref:ViciOne.Suite.DataPort.Extensions.Outgoing.QueueProcessing.QueueConfiguration)
is the required constructor parameter of `OutgoingDataPortBase`. See [Queue](queue.md) for the queue it
configures.

## Mapping

The mapper contracts that turn a typed root node into runtime data points are listed under
[Interfaces — Mapping](interfaces.md#mapping-interfaces).

## TypedNodes

**Namespace**: `ViciOne.Suite.DataPort.Extensions.Model.TypedNodes`

The configuration-time node model and the mappers that build it:

- [`TypedNodeMapper<TCommunication, TRootNode>`](xref:ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping.TypedNodeMapper%602) — builds the typed tree from a `DataPortCommunication`
- Node roles: [`IRootConfigurationNode<TCommunication>`](xref:ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.IRootConfigurationNode%601),
  [`IConfigurationNode`](xref:ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.IConfigurationNode),
  [`IBranchConfigurationNode`](xref:ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.IBranchConfigurationNode),
  [`IDataPointNode`](xref:ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.IDataPointNode)
- Per-node mappers: [`IBranchConfigurationNodeMapper<T>`](xref:ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping.IBranchConfigurationNodeMapper%601),
  [`IDataPointNodeMapper<T>`](xref:ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping.IDataPointNodeMapper%601)
- [`LinkedNodeExtensions`](xref:ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping.LinkedNodeExtensions) — typed property accessors over a `LinkedNode` (the latter is defined upstream in `DataPort`)

→ The tree, the three node roles, mapper dispatch (`IsTargetMapperFor`), `LinkedNode`, and why validation
runs before mapping: [Typed node model](../explanation/configuration/typed-node-model.md)
