# The YAML ruleset — the DataPort side

One file per provider: `src/<Provider>/<Provider>.yaml`, `CopyToOutputDirectory: Always` (the
consistency test loads it from the test output). Why it is the source of truth is in SKILL.md.

The format itself — sections, node and property fields, data types, UI controls, anchors, the rules
a file must satisfy to load — belongs to `ViciOne.Tree.Builder` and its `writing-ruleset-yaml` skill
(tree-builder `.claude/skills/writing-ruleset-yaml/`, with `references/schema-reference.md` and a
DataPort template). Read it at the tag the provider pins (`v3.0.0` today), not at tree-builder's
main. This file keeps only what a DataPort adds on top.

Worked examples: dp-modbus `src/Modbus.Client/ModbusClient.yaml` (grouped numeric leaves, transport
switching, per-area directions) and `src/Modbus.Server/ModbusServer.yaml` (extra unit level), cip
`src/AllenBradley.Logix/allen-bradley-logix.yaml` and dp-s7-absolute (large data-type inventories,
poll frequency per leaf), dp-mqtt `src/Mqtt/Mqtt.yaml` (envelope children).

## Header and root

```yaml
Common:
  Target: DataPort
  ContentType: Rules
  Version: 1.0.0                 # x.y[.z], no pre-release suffix — when it moves: SKILL.md
  Date: 2026-10-01               # a release sets it
  Description: "Modbus Client DataPort"
  Author: "ViciOne open automation gmbh"
  MetadataType: YAML
  Namespace: "ViciOne.DataPort.Modbus"

Root:
  Id: "ModbusClientDataPort"     # == the [Communication("…")] id on the record (compared ignoring case)
  Name: "Modbus Client"
  Icon: modbus
  ChildNodes:
    - Id: ModbusClientInstance   # the connection node
      MaxInstances: 16
```

## What the C# side pairs with

| YAML | C# |
|---|---|
| `Root.Id` | `[Communication("…")]` on the communication record |
| a property on the connection node (`Type: DataPortNode`) | a property of the communication record, same id, same type, same default |
| `MappingId` of a `DataPortTreeNode` | `TargetLinkedNodeTypeId` of exactly the mapper that claims it |
| a property under a tree node's `PropertyCategories` | a public property on that node record |
| `DataTypes` of a leaf | the CLR type of the runtime data point (grouped leaves: see below) |

**`MappingId` has no default** — it is an empty string unless declared, and the engine sends it
unchanged as the node's `DesignId`. Declare it on every `DataPortTreeNode`. It need not be unique:
dp-mqtt maps seven node types through `Topic`.

## Gotchas

**`UInt8` boxes as `byte`.** The engine boxes a `UInt8` property as `byte`, so
`PropertyValueConverter.To<int>` rejects it with an `InvalidType` error. Use
`PropertyValueConverter.ToEnum<TEnum>` for enum-shaped ones, and type the communication property
`byte`.

**`DependentProperties` keys are property Ids.** The loader rejects a key that matches no property
Id, and every value must be a key of this property's `Elements`:

```yaml
- Id: TransportType
  Elements: { 0: 'TCP', 1: 'RTU' }
  DependentProperties: { 'IpAddress': [0],
                         'Port':      [0],
                         'PortName':  [1],
                         'BaudRate':  [1] }
```

Renaming a property's Id means updating every `DependentProperties` entry that names it.
`ModbusDependentPropertiesTests` pins the map.

**Grouped `DataTypes` leaves.** One leaf id can carry several widths (`DataPointInteger` covering
`Int16 … UInt64`). The engine then populates `LinkedNode.ValueType` with the selected CLR type, and
the mapper derives the concrete width from it. `ValueType` is **not** a YAML property, so it must
not be a public settable property on the node — keep it `internal` and assign it via `init` from
the mapper.

**Anchors for repeated blocks.** Standard YAML anchors keep parallel node types honest:

```yaml
  - Id: InputRegister
    ChildNodes: &numericLeafNodes
      - Id: DataPointInteger
      - Id: DataPointFloat

  - Id: HoldingRegister
    ChildNodes: *numericLeafNodes
```

**`TransferModes`.** The first entry is the editor's default; an empty list does not load. Leaves
list what they support (`[ OnChange, Periodic ]`), containers `[ None ]`.

**Directions on area nodes.** `TransferDirections` may sit on a `DataPortTreeNode`, not only on the
connection node — dp-modbus declares read-only areas as `[ Inbound ]`. The editor narrows each node
to the directions it shares with its parent and offers connectors only in those, so a leaf under an
`[ Inbound ]` area gets no outbound connector. Keep the runtime guard anyway: the direction-aware C#
mapper (`ForWritableAreas()`) plus a defensive check in the client. Pin the declarations with an
`XNodeDirectionTests`-style test.

**Envelope children** (Tree.Builder 3). A leaf with `DataTypes` may have fixed `ChildNodes` that
travel with its value — dp-mqtt's `Timestamp`, `Validity` and `Type` on every data point. Their
`TransferDirections` must be a subset of the parent's, and `LinkDirections` says in which direction
the user may link them (`[ None ]` = filled by the port, allowed on envelope children only). The
tree-builder skill lists the load-time rules. `ruleset.IsValueNode(id)`
(`ViciOne.Tree.Builder.Extensions.EnvelopeExtensions`) tells a value node from a container; dps's
`YamlConsistencyBaseTest` still decides by `ChildNodes.Length`, so an envelope parent is compared
as a configuration node.

**Declarations nothing enforces.** These load, but nothing acts on them, so the mapper or validator
must:

- `Required: true` on a property reference — Tree.Builder only parses it; validate presence in the
  node's validator.
- `MaxDepth` on a `Root.ChildNodes` entry — the depth check only runs below a node that has a
  reference, so it never applies to the root's children.

**Poll interval placement.** dp-modbus puts `PollInterval` on the connection and every data point
inherits it; the dps stub, s7, dp-s7-absolute and cip put a `PollFrequency` property on each leaf.
Both are valid — the groups mapper just has to produce a `PollFrequency` per data point. Pick one
per provider and stay with it.
