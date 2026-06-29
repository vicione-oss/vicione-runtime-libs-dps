# Define the configuration schema

**Shared building block — step 1.** Used by both [incoming](../incoming-dataport/incoming-data-port.md)
and [outgoing](../outgoing-dataport/outgoing-data-port.md) ports.

Before a port can run, a dataport author declares — in YAML — which parts of the protocol ViciOne
should expose: the node types, the properties each carries, and how they may be nested. The suite
editor reads that schema to offer the configuration UI, and at runtime the user's configuration
arrives at your port as a single `DataPortCommunication`. This guide covers the parts of the schema
your extension code depends on; the schema format itself is owned by the DataPort core (see
[Related](#related)).

The complete schema for the worked example is `tests/DataPort.Extensions.Tests/YamlConsistency/TestDataPort.yaml`.
The sections below point at it.

## What the schema declares

A schema file has a handful of top-level sections. The ones that matter when you write the typed node
model are `Root`, `Nodes`, and `Properties`:

```yaml
Root:
  Id: "TestDataPort"
  ChildNodes:
    - Id: Device              # which node types may sit directly under the root

Nodes:
  - Id: Device
    Type: DataPortNode        # the root/device node
    TransferDirections: [ Outbound, Inbound ]
    ChildNodes:               # which node types may nest inside a Device
      - Id: Folder
      - Id: Int
      - Id: String
    PropertyCategories:
      - Id: Connection
        Properties:
          - Id: IpAddress
          - Id: Port

  - Id: Int
    MappingId: Int            # the id your data-point mapper matches on
    Type: DataPortTreeNode
    DataTypes: [ Int32 ]      # the value type this data point carries
    TransferModes: [ OnChange, Periodic ]
    PropertyCategories:
      - Id: Configuration
        Properties:
          - Id: IntNodeProperty
          - Id: PollFrequency
```

Each entry under `Nodes` becomes one node type in the editor. A node declares the child node types it
accepts (`ChildNodes`), the properties it carries (grouped into `PropertyCategories`), and — for a
data point — the value types it can hold (`DataTypes`).

## How the schema connects to your code

Three things in the YAML are the contract your typed node model is written against:

- **`Id` / `MappingId`** — the design id the editor stamps onto each configured node. Your node
  mappers claim a node by matching it, through `TargetLinkedNodeTypeId` (see
  [Model the configuration as typed nodes](node-model.md)). In the example, the `Int` node's
  `MappingId: Int` is what `IntNodeMapper` returns from `TargetLinkedNodeTypeId`.
- **Property `Id`s** — each property your code reads must be declared here. Your mappers pull them out
  by id with `LinkedNode.GetRequiredPropertyValue<T>("PollFrequency")`, so the id and the .NET type
  (`DataType`) have to line up with the read.
- **`DataTypes`** — the value type a data point declares is what your runtime data point models with
  `ITypedDataPoint<TValue>` (see [Define the runtime data points](data-points.md)).

Because these are a contract and not a coincidence, drift between the YAML and the C# model is a real
risk. The [YAML-consistency test](yaml-consistency-test.md) exists to catch it.

## Next

→ [Model the configuration as typed nodes](node-model.md) — turn the schema's nodes into a validated,
strongly-typed tree.

## Related

- [DataPortCommunication](../../../DataPort/explanation/data-port-communication.md) — what the runtime
  configuration object carries and where it comes from
- [LinkedNode and channels](../../../DataPort/explanation/linked-node.md) — how the flat node list is
  rebuilt into a tree, and the channel system
- [Typed node model — design rationale](../../explanation/configuration/typed-node-model.md) — why the
  schema is a tree of typed nodes
