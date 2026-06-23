# Model the configuration as typed nodes

**Shared building block — step 2.** Used by both [incoming](../incoming-dataport/incoming-data-port.md)
and [outgoing](../outgoing-dataport/outgoing-data-port.md) ports.

The configuration reaches your port as a `DataPortCommunication` with a flat, weakly-typed list of
nodes. Before anything else runs, the base class turns that into a validated, strongly-typed tree
using a `TypedNodeMapper` you assemble from per-role mappers. This is where your protocol's structure
becomes real C# types — and where a malformed configuration fails, at construction, before the
network is touched.

This guide builds the tree from the bottom up: the communication object, then the three node roles
and their mappers, then the `TypedNodeMapper` that ties them together. The worked example is under
`tests/DataPort.Extensions.Tests/Stubs/Model/Nodes/`. For *why* the model is a typed tree with three
roles and dispatch-based mapping, see the
[design rationale](../../explanation/configuration/typed-node-model.md); this guide is the recipe.

## 1. Define the communication type

The communication type is the root configuration object, deserialized from the user's settings. It
extends `DataPortCommunication` and carries the connection-level configuration — and nothing
per-node, since the nodes arrive separately.

`Stubs/Model/Nodes/Device/TestCommunication.cs`:

```csharp
[Communication("TestDataPort")]
public record TestCommunication : DataPortCommunication
{
    public string IpAddress { get; init; } = "192.168.1.100";
    public int Port { get; init; } = 102;
    public QueueConfiguration QueueConfiguration { get; init; } = new(QueueStrategy.DropOldest, new QueueSize(10));
}
```

The `[Communication("…")]` id matches the schema's `Common` block, and the properties match the
root node's connection properties from [the schema](configuration-schema.md).

## 2. Define the root node and its mapper

Every tree has exactly one root, representing the source itself. It implements
`IRootConfigurationNode<TCommunication>` and holds the device identifier plus the list of child and
data-point nodes the mapper will attach.

`Stubs/Model/Nodes/Device/TestDeviceNode.cs`:

```csharp
public sealed class TestDeviceNode : IRootConfigurationNode<TestCommunication>
{
    public TestCommunication OriginalCommunication { get; init; }
    public DeviceIdentifier DeviceIdentifier { get; init; }
    public List<IConfigurationNode> ConfigurationNodes { get; } = [];
    public List<IDataPointNode> DataPointNodes { get; } = [];
    // CanBeAdded(...) gates which children may attach
}
```

The root mapper implements `IRootConfigurationNodeMapper<TRootNode, TCommunication>`. It validates the
whole communication once, up front, and builds the root node. If `Validate` returns failures, the
base class raises a single `InvalidConfigurationException` with per-field detail before any node is
mapped.

`Stubs/Model/Nodes/Device/Mapping/TestDeviceNodeMapper.cs`:

```csharp
internal sealed class TestDeviceNodeMapper : IRootConfigurationNodeMapper<TestDeviceNode, TestCommunication>
{
    public TestDeviceNode CreateRootNode(TestCommunication communication) =>
        new(communication.DesignId, communication);

    public ValidationResult Validate(TestCommunication communication) => new(); // no root-level rules
}
```

## 3. Define branch nodes and their mappers (optional)

Branch nodes are structural groupings — a folder, a program, a namespace — that hold no readable
value but can carry context for the nodes below them. Skip them if your protocol is flat. A branch
node implements `IBranchConfigurationNode`; its mapper implements
`IBranchConfigurationNodeMapper<TBranchNode>` and claims nodes by design id (and optionally finer
conditions).

`Stubs/Model/Nodes/Folders/Mapping/TestFolderNodeMapper.cs`:

```csharp
internal sealed class TestFolderNodeMapper : IBranchConfigurationNodeMapper<TestBranchNodeWithProperties>
{
    public string TargetLinkedNodeTypeId => "Folder";

    // Two mappers share the "Folder" id; this one claims the nodes WITHOUT a GroupName property
    public bool IsTargetMapperFor(LinkedNode linkedNode)
        => linkedNode.DesignId == TargetLinkedNodeTypeId && !linkedNode.HasProperty("GroupName");

    public TestBranchNodeWithProperties Map(LinkedNode node) => new()
    {
        OriginalNode = node,
        CustomProperty1 = node.GetRequiredPropertyValue<string>("CustomProperty1"),
        CustomProperty2 = node.GetRequiredPropertyValue<int>("CustomProperty2")
    };

    public ValidationResult Validate(LinkedNode linkedNode) => new();
}
```

`IsTargetMapperFor` is how two node types that share a design id are told apart — here, a plain folder
versus a named group. By default a mapper claims any node whose `DesignId` equals its
`TargetLinkedNodeTypeId`; override `IsTargetMapperFor` only when you need more than the id.

## 4. Define data-point (leaf) nodes and their mappers

Leaf nodes are the individual data points — the values that can be read or written. Each implements
`IDataPointNode`, exposing its `Channels` (built from the linked node) and, for pollable points, a
`PollFrequency`. The example shares a base record across data-point node types so each subtype only
declares its own property:

`Stubs/Model/Nodes/DataPoints/DataPointNodeBase.cs` and `…/Int/IntNode.cs`:

```csharp
internal abstract record DataPointNodeBase(LinkedNode OriginalNode, PollFrequency PollFrequency) : IDataPointNode
{
    public Channels Channels { get; } = new(
        Channels.AffectedChannels([.. OriginalNode.AffectedChannels]),
        Channels.TransferredChannels([.. OriginalNode.TransferredChannels]));
}

internal sealed record IntNode(LinkedNode OriginalNode, PollFrequency PollFrequency, IntNodeProperty IntNodeProperty)
    : DataPointNodeBase(OriginalNode, PollFrequency);
```

The data-point mapper implements `IDataPointNodeMapper<TNode>`. The example factors the common
poll-frequency parse into a base mapper, leaving each concrete mapper to declare its target id and
build its node:

`Stubs/Model/Nodes/DataPoints/DataPointNodeMapperBase.cs` and `…/Int/Mapping/IntNodeMapper.cs`:

```csharp
internal abstract class DataPointNodeMapperBase<TNode> : IDataPointNodeMapper<TNode> where TNode : IDataPointNode
{
    public abstract string TargetLinkedNodeTypeId { get; }
    public TNode Map(LinkedNode node) => CreateNode(node, ReadPollFrequency(node));
    public virtual ValidationResult Validate(LinkedNode linkedNode) => new();
    protected abstract TNode CreateNode(LinkedNode node, PollFrequency pollFrequency);

    private static PollFrequency ReadPollFrequency(LinkedNode node)
        => PollFrequency.FromMilliseconds(node.GetRequiredPropertyValue<int>("PollFrequency"));
}

internal sealed class IntNodeMapper : DataPointNodeMapperBase<IntNode>
{
    public override string TargetLinkedNodeTypeId => "Int"; // matches the schema's MappingId

    protected override IntNode CreateNode(LinkedNode node, PollFrequency pollFrequency)
        => new(node, pollFrequency, new IntNodeProperty(node.GetRequiredPropertyValue<string>("IntNodeProperty")));
}
```

Read every property you declared in [the schema](configuration-schema.md) here, with
`GetRequiredPropertyValue<T>("Id")` — the id and `T` must match the schema. `Validate` runs
immediately before `Map` for each node and gates it: a node that fails validation is never mapped,
and the first failure aborts construction.

## 5. Assemble the `TypedNodeMapper`

Finally, wire the root mapper plus the lists of branch and data-point mappers into a
`TypedNodeMapper<TCommunication, TRootNode>`. The base port takes this in its constructor.

`Stubs/Model/Nodes/Mapper/TestNodeMapper.cs`:

```csharp
internal static class TestNodeMapper
{
    public static TypedNodeMapper<TestCommunication, TestDeviceNode> Instance() =>
        new(
            new TestDeviceNodeMapper(),                              // the one root mapper
            [new TestGroupNodeMapper(), new TestFolderNodeMapper()], // branch mappers, tried in order
            [new IntNodeMapper(), new StringNodeMapper()]);          // data-point mappers, tried in order
}
```

When the engine walks the node tree, it hands each node to the first registered mapper that claims it.
A node no mapper claims is a hard configuration error (`NotSupportedException`) — surfaced at
construction, never skipped silently.

## Next

→ [Define the runtime data points](data-points.md) — the flat, typed values the port actually reads
and writes, produced from this tree.

## Related

- [Typed node model — design rationale](../../explanation/configuration/typed-node-model.md) — why a
  tree, three roles, dispatch, and validate-before-map
- [Model reference (TypedNodes)](../../reference/model.md#typednodes) — `TypedNodeMapper` constructor,
  `LinkedNode`, and the mapper contracts
- [Keep the schema and node model in sync](yaml-consistency-test.md)
