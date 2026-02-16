# DataPort Extensions — Core Concepts

This document explains the design patterns the Extensions package is built around. Reading it first makes the more detailed reference docs easier to follow.

---

## From YAML to Typed Objects

DataPort configuration starts life as a YAML file and is deserialized into a `DataPortCommunication` subclass — at that point it is a weakly-typed bag of strings and nested maps. The library's first job is to transform this into a validated, strongly-typed C# object hierarchy before any protocol logic runs.

```
YAML file
    └─▶ DataPortCommunication          (deserialized, weakly typed)
            └─▶ TypedNodeMapper        (validates + maps)
                    └─▶ IRootConfigurationNode   (strongly typed tree)
                            └─▶ IDataPointsMapper / IDataPointGroupsMapper
                                    └─▶ List<TDataPoint>   (runtime data points)
```

Shape mismatches and missing fields fail at startup during the mapper step, not during polling at runtime.

---

## Two Mapper Layers

**`TypedNodeMapper<TCommunication, TRootNode>`** (in `Model.TypedNodes.Mapping`)

Converts `DataPortCommunication` → `IRootConfigurationNode`. This is where FluentValidation runs and the node hierarchy is assembled with parent-child links. The mapper is provided once at construction time and is shared across the lifetime of the data port.

**`IDataPointsMapper` / `IDataPointGroupsMapper`** (in `Model.Mapping`)

Traverse the typed node tree to produce a flat `List<TDataPoint>`. The grouping variant (`IDataPointGroupsMapper`) additionally groups data points by `PollFrequency` for incoming data ports — one polling job runs per group.

Each layer has a single responsibility and can be tested in isolation.

---

## Typed Node Interfaces

The typed node tree is built from three interface layers:

| Interface | Role |
|-----------|------|
| `IRootConfigurationNode<TCommunication>` | Root of the tree; holds the device identity |
| `IBranchConfigurationNode` | Interior node; groups child nodes and data points |
| `IDataPointNode` | Leaf node; maps directly to a `TDataPoint` |

Protocol implementations define concrete types for each layer and register node mappers (`IBranchConfigurationNodeMapper`, `IDataPointNodeMapper`) that know how to translate a raw YAML node into the typed equivalent.

---

## YAML Consistency Tests

The `DataPort.Extensions.Testing` package provides `YamlConsistencyBaseTest<TCommunication, TYamlConfig, TRootNode>`, a base test class that automatically verifies the YAML schema and the C# mapper code stay in sync.

Implement `IYamlTestingConfig` to wire up the test:

```csharp
public class MyYamlConfig : IYamlTestingConfig<MyCommunication, MyRootNode>
{
    public static string YamlFileName => "my-protocol.yaml";
    public static TypedNodeMapper<MyCommunication, MyRootNode> Mapper => MyTypedNodeMappers.Create();
    public static HashSet<string> ExcludedCommunicationProperties => [];
}

public class MyYamlConsistencyTests : YamlConsistencyBaseTest<MyCommunication, MyYamlConfig, MyRootNode> { }
```

The base class generates individual `[Theory]` test cases that verify:

- The YAML file itself is structurally valid
- The `[Communication]` attribute ID matches the YAML root ID
- Every YAML property has a corresponding C# property on the communication class (and vice versa)
- Every registered mapper targets a node that exists in the YAML
- Every YAML node has a registered mapper
- Every property referenced in a mapper exists in the corresponding YAML node (and vice versa)

Each check is a separate test, so failures pinpoint exactly which property or node is out of sync.

---

## Further Reading

- [IncomingDataPort.md](Incoming/IncomingDataPort.md) — base class for read data ports; includes polling infrastructure details
- [OutgoingDataPort.md](Outgoing/OutgoingDataPort.md) — base class for write data ports
- [Interfaces.md](Interfaces.md) — all interface contracts
- [Model.md](Model.md) — typed node interfaces and model types
