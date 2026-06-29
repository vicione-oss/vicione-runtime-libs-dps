# DataPort.Extensions.Testing

Test helpers for DataPort implementations built with `DataPort.Extensions`. Two distinct capabilities: YAML schema consistency verification, and eventual-value assertions for async test code.

---

## YAML Consistency Tests

### Why they exist

A DataPort implementation built with `DataPort.Extensions` has two artefacts that must stay in sync:

- The **YAML schema file** — documents the node types, property names, and structure that the protocol's configuration can carry. This is what the ViciOne platform uses to understand the protocol's configuration shape.
- The **C# mapper code** — `IDataPointNodeMapper` and `IBranchConfigurationNodeMapper` implementations that read those property names from `LinkedNode` instances at runtime.

During development, it is easy for these to drift: a property gets renamed in the schema but the mapper still reads the old name, or a new node type gets added to the C# code but not documented in the YAML. Neither of these mismatches raises a compile error. They surface as runtime failures or silent misbehaviour.

`YamlConsistencyBaseTest` automates the cross-check. Each consistency rule — property names declared in YAML match property names read by mappers, every mapper has a corresponding YAML node, every YAML node has a corresponding mapper — becomes a separate `[Theory]` test case. A failure names exactly what is out of sync, not just that something is wrong.

### What gets verified

1. YAML file parses without errors
2. `[Communication]` attribute ID on the `DataPortCommunication` subclass matches the YAML root ID
3. Properties declared in YAML DataPort nodes exist as C# properties on the `DataPortCommunication` subclass
4. C# properties on the `DataPortCommunication` subclass exist in YAML device nodes
5. Each `IBranchConfigurationNodeMapper` has a corresponding node in the YAML
6. Each configuration node in the YAML has a corresponding `IBranchConfigurationNodeMapper`
7. Properties read by each branch mapper exist in the corresponding YAML node
8. Properties declared in each YAML configuration node exist in the corresponding branch mapper
9. Each `IDataPointNodeMapper` has a corresponding node in the YAML
10. Each data point node in the YAML has a corresponding `IDataPointNodeMapper`
11. Properties read by each data point mapper exist in the corresponding YAML node
12. Properties declared in each YAML data point node exist in the corresponding data point mapper

### Setup

```csharp
// 1. Define a testing configuration
public class MyYamlTestConfig : IYamlTestingConfig<MyCommunication, MyRootConfigurationNode>
{
    public static string YamlFileName => "my-device.yaml";

    public static TypedNodeMapper<MyCommunication, MyRootConfigurationNode> Mapper =>
        MyTypedNodeMapper.Instance;

    // Optional: exclude computed or internal properties from the C# ↔ YAML check
    public static HashSet<string> ExcludedCommunicationProperties => ["InternalComputedProperty"];
}

// 2. Inherit the base test class — all test methods are provided
public class MyYamlConsistencyTests
    : YamlConsistencyBaseTest<MyCommunication, MyYamlTestConfig, MyRootConfigurationNode>
{
}
```

No additional test methods are needed. The base class provides all twelve checks as xUnit `[Theory]` test cases with dynamically generated test case data.

### Test methods

| Method | What it checks |
|---|---|
| `IsYamlFileValid()` | YAML syntax and structure |
| `CommunicationAttributeMatchesYamlRootId()` | `[Communication]` attribute vs YAML root ID |
| `YamlDataPortNodePropertiesExistInCommunicationClass` | YAML properties → C# class |
| `CommunicationPropertyExistsInYamlDeviceNodes` | C# class properties → YAML |
| `ConfigurationNodeMapperHasCorrespondingYamlConfigurationNode` | Branch mapper → YAML node |
| `YamlConfigurationNodeHasCorrespondingConfigurationNodeMapper` | YAML node → branch mapper |
| `ConfigurationMapperPropertyExistsInYamlNode` | Branch mapper property names → YAML |
| `YamlConfigurationNodePropertyExistsInConfigurationMapper` | YAML property names → branch mapper |
| `DataPointNodeMapperHasCorrespondingYamlDataPointNode` | Data point mapper → YAML node |
| `YamlDataPointNodeHasCorrespondingDataPointNodeMapper` | YAML node → data point mapper |
| `DataPointMapperPropertyExistsInYamlNode` | Data point mapper property names → YAML |
| `YamlDataPointNodePropertyExistsInDataPointMapper` | YAML property names → data point mapper |

---

## Eventual Assertions

### Why they exist

Async DataPort behaviour — connection established, data published, queue drained — cannot be asserted at a single instant. Tests that use `Thread.Sleep` or fixed delays are slow and fragile; tests that poll in a loop until a condition is true are verbose.

`EventualAssertions` provides a thin wrapper that retries an assertion until it passes or a timeout elapses, producing a clean failure message when the condition is never met.

### Usage

```csharp
using ViciOne.Suite.DataPort.Extensions.Testing.Assertions;

// Assert a value eventually reaches the expected value (default 5s timeout)
Eventually(() => callCount).Should().Be(21, "retries should complete");

// Assert an eventual boolean condition
Eventually(() => isComplete).Should().BeTrue("operation should finish");

// Use a custom timeout
Eventually(() => items.Count, timeoutMs: 10000).Should().Be(5, "items should be added");

// Assert a custom condition
Eventually(() => status)
    .Should().Match(s => s.StartsWith("success"), "status should indicate success");
```

The default timeout is 5000 ms.

---

## Related

- [Typed node model](../DataPort.Extensions/explanation/configuration/typed-node-model.md) — why the typed node model exists and why validation runs before mapping
- [DataPort.Extensions overview](../DataPort.Extensions/explanation/Overview.md) — the pipeline that YAML consistency tests guard
