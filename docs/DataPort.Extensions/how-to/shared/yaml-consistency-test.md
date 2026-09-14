# Keep the schema and node model in sync

**Shared building block — recommended.** Applies to any port built with the
[typed node model](node-model.md).

The [YAML schema](configuration-schema.md) and the [C# node model](node-model.md) are two
descriptions of the same structure, written in two places. When they drift — a property renamed in
YAML but not in a mapper, a node type added to one but not the other — the failure shows up at runtime
as a missing property or an unmapped node. The `YamlConsistencyBaseTest` from the
**DataPort.Extensions.Testing** package turns that drift into a failing unit test instead.

The worked example is `tests/DataPort.Extensions.Tests/YamlConsistency/YamlConsistencyTest.cs`.

## Write the test

Subclass `YamlConsistencyBaseTest<TCommunication, TYamlConfig, TRootNode>` and implement
`IYamlTestingConfig`. You point it at your YAML file and your assembled
[`TypedNodeMapper`](node-model.md#5-assemble-the-typednodemapper); the base test walks both and
asserts they agree.

```csharp
public class YamlConsistencyTest :
    YamlConsistencyBaseTest<TestCommunication, YamlConsistencyTest, TestDeviceNode>,
    IYamlTestingConfig<TestCommunication, TestDeviceNode>
{
    public static string YamlFileName => "TestDataPort.yaml";

    public static TypedNodeMapper<TestCommunication, TestDeviceNode> Mapper => TestNodeMapper.Instance();

    // Properties that exist in code but intentionally aren't surfaced in the schema:
    public static HashSet<string> ExcludedCommunicationProperties => [nameof(TestCommunication.ConnectionString)];

    public static Dictionary<Type, HashSet<string>> ExcludedConfigurationNodeProperties => new()
    {
        [typeof(TestBranchNodeWithProperties)] = [nameof(TestBranchNodeWithProperties.FullPath)],
    };

    public static Dictionary<Type, HashSet<string>> ExcludedDataPointNodeProperties => new();
}
```

The `Excluded…` sets are the escape hatch for the handful of members that legitimately exist on only
one side — a computed property like `ConnectionString`, or a derived `FullPath` — so the test stays
green for genuine, deliberate differences while still catching accidental drift. The dictionary key
may be the concrete node type or any base type or interface that declares the property — an entry
keyed by a base class applies to all node types deriving from it.

Make sure the YAML file is copied to the test output so the test can load it. In the example's
`.csproj`:

```xml
<Content Include="YamlConsistency/TestDataPort.yaml" Link="TestDataPort.yaml">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
```

## Related

- [DataPort.Extensions.Testing](../../../DataPort.Extensions.Testing/index.md) —
  `YamlConsistencyBaseTest`, the eventual-assertion and logging helpers
- [Define the configuration schema](configuration-schema.md) ·
  [Model the configuration as typed nodes](node-model.md) — the two sides this test compares
- [Typed node model — design rationale](../../explanation/configuration/typed-node-model.md) — why the
  model being explicit in code lets it be checked against the schema at all
